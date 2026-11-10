using System.ComponentModel.DataAnnotations;

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
    decimal Kolicina,
    Enota Enota,
    Lokacija Lokacija,
    DateOnly? RokUporabnosti,
    string Dodal,
    DateTime DodanoOb,
    string? Opozorilo)
{
    public static ZalogaOdgovor Iz(ZalogaShrambe z, string? opozorilo = null) => new(
        z.Id, z.ZiviloId, z.Zivilo.Ime, z.Kolicina, z.Enota, z.Lokacija,
        z.RokUporabnosti, z.Dodal.Ime, z.DodanoOb, opozorilo);
}
