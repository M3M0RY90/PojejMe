using System.ComponentModel.DataAnnotations;
using PojejMe.Api.Models.Shramba;
using PojejMe.Api.Services;

namespace PojejMe.Api.Models.Recepti;

// Kar pošlje odjemalec pri dodajanju (POST) in urejanju (PUT) recepta
public class ReceptVnos
{
    [Required(ErrorMessage = "Ime recepta je obvezno.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ime recepta mora imeti od 2 do 100 znakov.")]
    public string Ime { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Opis je lahko dolg največ 1000 znakov.")]
    public string? Opis { get; set; }

    [Range(1, 50, ErrorMessage = "Število porcij mora biti med 1 in 50.")]
    public int SteviloPorcij { get; set; }

    [MinLength(1, ErrorMessage = "Recept mora imeti vsaj eno sestavino.")]
    public List<SestavinaVnos> Sestavine { get; set; } = [];

    [MinLength(1, ErrorMessage = "Recept mora imeti vsaj en korak priprave.")]
    public List<string> Koraki { get; set; } = [];
}

public class SestavinaVnos
{
    [Range(1, int.MaxValue, ErrorMessage = "Izberi živilo iz baze.")]
    public int ZiviloId { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Količina mora biti večja od 0.")]
    public decimal Kolicina { get; set; }

    [Required(ErrorMessage = "Enota je obvezna.")]
    public Enota? Enota { get; set; }
}

// Kar API vrne
public record SestavinaOdgovor(int ZiviloId, string Zivilo, decimal Kolicina, Enota Enota);

public record ReceptOdgovor(
    int Id,
    string Ime,
    string? Opis,
    int SteviloPorcij,
    string? Slika,
    List<SestavinaOdgovor> Sestavine,
    List<string> Koraki,
    DateTime UstvarjenOb,
    DateTime? PosodobljenOb,
    PovzetekHranil HranilneVrednosti) // UZ-3.3: izračunano samodejno iz sestavin
{
    public static ReceptOdgovor Iz(Recept r) => new(
        r.Id,
        r.Ime,
        r.Opis,
        r.SteviloPorcij,
        r.SlikaPot,
        r.Sestavine.Select(s => new SestavinaOdgovor(s.ZiviloId, s.Zivilo.Ime, s.Kolicina, s.Enota)).ToList(),
        r.Koraki.OrderBy(k => k.Zaporedje).Select(k => k.Opis).ToList(),
        r.UstvarjenOb,
        r.PosodobljenOb,
        HranilnaVrednost.Povzetek(r));
}
