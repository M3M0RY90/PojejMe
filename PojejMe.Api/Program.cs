using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PojejMe.Api.Data;
using PojejMe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// povezava na MySQL: lokalno iz appsettings.Development.json, na Azuru iz okoljske spremenljivke
// ConnectionStrings__PojejMe
var connectionString = builder.Configuration.GetConnectionString("PojejMe");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<PojejMeDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));
}

// ključ za podpis žetonov (UZ-1.1): lokalno iz appsettings.Development.json, na Azuru iz skrivnosti JWT_KLJUC
// (okoljska spremenljivka Jwt__Kljuc); če ga ni, uporabimo naključnega, žetoni pa veljajo do ponovnega zagona
var jwtKljuc = builder.Configuration["Jwt:Kljuc"];
if (string.IsNullOrEmpty(jwtKljuc))
{
    jwtKljuc = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    Console.WriteLine("OPOZORILO: Jwt:Kljuc ni nastavljen, uporabljam naključen ključ (žetoni ne preživijo ponovnega zagona).");
}
var podpisniKljuc = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKljuc));
builder.Services.AddSingleton(podpisniKljuc);
builder.Services.AddSingleton<ZetonStoritev>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // imen zahtevkov ne preslikamo v .NET imena: "sub" ostane "sub"
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = ZetonStoritev.Izdajatelj,
            ValidAudience = ZetonStoritev.Izdajatelj,
            IssuerSigningKey = podpisniKljuc,
            ClockSkew = TimeSpan.Zero
        };
        o.Events = new JwtBearerEvents
        {
            // podpis in rok sta že preverjena; še verzijo žetona primerjamo z bazo (odjava jo poveča)
            OnTokenValidated = async ctx =>
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<PojejMeDbContext>();
                var sub = ctx.Principal?.FindFirstValue("sub");
                var verzija = ctx.Principal?.FindFirstValue("verzija");

                if (!int.TryParse(sub, out var id) || !int.TryParse(verzija, out var v))
                {
                    ctx.Fail("Žeton ni veljaven.");
                    return;
                }

                var trenutna = await db.Uporabniki
                    .Where(u => u.Id == id)
                    .Select(u => (int?)u.ZetonVerzija)
                    .FirstOrDefaultAsync();

                if (trenutna != v) ctx.Fail("Žeton ni več veljaven.");
            }
        };
    });
builder.Services.AddAuthorization();

// enume v JSON pišemo kot besedilo ("Hladilnik"), ne kot številke (0)
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // gumb "Authorize" v Swaggerju: prilepiš žeton iz /api/auth/prijava (brez besede Bearer)
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Žeton iz /api/auth/prijava"
    });
    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// ob zagonu posodobimo bazo na zadnjo migracijo
if (!string.IsNullOrEmpty(connectionString))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<PojejMeDbContext>().Database.Migrate();
}

// Swagger je vklopljen tudi na Azuru, ker je to edini vmesnik za testiranje API-ja
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

// stanje strežnika, povezave z bazo in različica slike (commit), nastavljena ob gradnji v GitHub Actions
app.MapGet("/api/status", async (IServiceProvider services) =>
{
    var db = services.GetService<PojejMeDbContext>();
    var baza = db is null ? "ni nastavljena" : await db.Database.CanConnectAsync() ? "ok" : "napaka";

    return new
    {
        status = "ok",
        baza,
        verzija = Environment.GetEnvironmentVariable("APP_VERSION") ?? "lokalno"
    };
});

app.MapControllers();

app.Run();
