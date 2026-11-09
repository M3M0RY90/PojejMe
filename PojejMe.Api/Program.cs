using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// povezava na MySQL: lokalno iz appsettings.Development.json, na Azuru iz okoljske spremenljivke
// ConnectionStrings__PojejMe
var connectionString = builder.Configuration.GetConnectionString("PojejMe");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<PojejMeDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
