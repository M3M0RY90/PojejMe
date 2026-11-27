namespace PojejMe.Api.Services;

// UZ-2.6: oznaka svežosti glede na rok uporabnosti
public enum OznakaRoka { Sveze, KmaluPotece, Poteklo }

public static class Svezost
{
    // koliko dni pred rokom je živilo že »kmalu poteče« (danes in naslednji 3 dnevi)
    public const int MejaKmaluPotece = 3;

    private static readonly TimeZoneInfo Slovenija = NajdiCasovniPas();

    // današnji datum po slovenskem času; strežnik na Azuru teče v UTC, zato bi bil med polnočjo
    // in 1h (2h poleti) sicer še včerajšnji dan
    public static DateOnly Danes() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Slovenija));

    public static int? DniDoRoka(DateOnly? rok, DateOnly danes) =>
        rok is null ? null : rok.Value.DayNumber - danes.DayNumber;

    public static OznakaRoka? Oznaka(DateOnly? rok, DateOnly danes) => DniDoRoka(rok, danes) switch
    {
        null => null,                                 // brez roka: brez oznake
        < 0 => OznakaRoka.Poteklo,                    // rok je že mimo
        <= MejaKmaluPotece => OznakaRoka.KmaluPotece, // danes ali v naslednjih 3 dneh
        _ => OznakaRoka.Sveze
    };

    public static string? Barva(OznakaRoka? oznaka) => oznaka switch
    {
        OznakaRoka.Sveze => "zelena",
        OznakaRoka.KmaluPotece => "rumena",
        OznakaRoka.Poteklo => "rdeca",
        _ => null
    };

    private static TimeZoneInfo NajdiCasovniPas()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Ljubljana"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }
}
