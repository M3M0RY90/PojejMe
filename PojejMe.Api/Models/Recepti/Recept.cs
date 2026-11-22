using PojejMe.Api.Models.Shramba;
using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Models.Recepti;

// UZ-3.1: recept, ki ga ustvari registriran uporabnik
public class Recept
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";
    public string? Opis { get; set; }
    public int SteviloPorcij { get; set; }
    public string? SlikaPot { get; set; }

    public int AvtorId { get; set; }
    public Uporabnik Avtor { get; set; } = null!;

    public DateTime UstvarjenOb { get; set; }
    public DateTime? PosodobljenOb { get; set; }

    public List<SestavinaRecepta> Sestavine { get; set; } = [];
    public List<KorakPriprave> Koraki { get; set; } = [];
}

// Ena sestavina recepta: živilo iz baze živil + količina + enota
public class SestavinaRecepta
{
    public int Id { get; set; }
    public int ReceptId { get; set; }
    public Recept Recept { get; set; } = null!;

    public int ZiviloId { get; set; }
    public Zivilo Zivilo { get; set; } = null!;

    public decimal Kolicina { get; set; }
    public Enota Enota { get; set; }
}

// En korak priprave; Zaporedje določa vrstni red (1, 2, 3 ...)
public class KorakPriprave
{
    public int Id { get; set; }
    public int ReceptId { get; set; }
    public Recept Recept { get; set; } = null!;

    public int Zaporedje { get; set; }
    public string Opis { get; set; } = "";
}
