using System.ComponentModel.DataAnnotations;
using PojejMe.Api.Services;

namespace PojejMe.Api.Models.Shramba;

// kar član gospodinjstva pošlje pri dodajanju v shrambo
public class ZalogaVnos
{
    [Range(1, int.MaxValue, ErrorMessage = "Izberi živilo iz baze.")]
    public int ZiviloId { get; set; }

    // 0 in negativne količine zavrnemo (pogoj 3 pri UZ-2.2)
    [Range(0.01, 100000, ErrorMessage = "Količina mora biti večja od 0.")]
    public decimal Kolicina { get; set; }

    [Required]
    public Enota? Enota { get; set; }

    [Required]
    public Lokacija? Lokacija { get; set; }

    public DateOnly? RokUporabnosti { get; set; }
}

public record ZalogaOdgovor(
    int Id,
    int ZiviloId,
    string Zivilo,
    string Skupina,
    decimal Kolicina,
    Enota Enota,
    Lokacija Lokacija,
    DateOnly? RokUporabnosti,
    int? DniDoRoka,
    OznakaRoka? Oznaka,
    string? Barva,
    string Dodal,
    DateTime DodanoOb,
    string? Opozorilo)
{
    public static ZalogaOdgovor Iz(ZalogaShrambe z, string? opozorilo = null)
    {
        // UZ-2.6: oznaka in barva glede na to, koliko dni je še do roka
        var danes = Svezost.Danes();
        var oznaka = Svezost.Oznaka(z.RokUporabnosti, danes);

        return new(
            z.Id, z.ZiviloId, z.Zivilo.Ime, z.Zivilo.Skupina.Ime, z.Kolicina, z.Enota, z.Lokacija,
            z.RokUporabnosti, Svezost.DniDoRoka(z.RokUporabnosti, danes), oznaka, Svezost.Barva(oznaka),
            z.Dodal.Ime, z.DodanoOb, opozorilo);
    }
}

// UZ-2.5 pregled po lokacijah: zadetki, število po lokacijah in sporočilo, ko ni ničesar
public record PregledShrambe(
    Dictionary<Lokacija, int> PoLokacijah,
    int Stevilo,
    string? Sporocilo,
    List<ZalogaOdgovor> Zaloge);

// UZ-2.7 poraba ali zavržek: brez količine se porabi oz. zavrže celotna zaloga
public class PorabaVnos
{
    [Range(0.01, 100000, ErrorMessage = "Količina mora biti večja od 0.")]
    public decimal? Kolicina { get; set; }
}

public class ZavrzekVnos : PorabaVnos
{
    [MaxLength(200)]
    public string? Razlog { get; set; }
}

public record DogodekOdgovor(
    int Id, int ZiviloId, string Zivilo, decimal Kolicina, Enota Enota, VrstaDogodka Vrsta,
    string? Razlog, string Uporabnik, DateTime Cas)
{
    public static DogodekOdgovor Iz(DogodekZaloge d) => new(
        d.Id, d.ZiviloId, d.Zivilo.Ime, d.Kolicina, d.Enota, d.Vrsta, d.Razlog, d.Uporabnik.Ime, d.Cas);
}

// Zaloga je null, ko je bila porabljena ali zavržena v celoti in je iz shrambe izginila
public record PorabaOdgovor(DogodekOdgovor Dogodek, ZalogaOdgovor? Zaloga, string Sporocilo);
