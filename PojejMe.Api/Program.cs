var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Pojej.me API deluje! - Test CI!");

// stanje strežnika in različica slike (commit), nastavljena ob gradnji v GitHub Actions
app.MapGet("/api/status", () => new
{
    status = "ok",
    verzija = Environment.GetEnvironmentVariable("APP_VERSION") ?? "lokalno"
});

app.Run();
