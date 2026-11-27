namespace PojejMe.Api.Models.Recepti;

// UZ-3.7: zapis, da je bil recept skuhan (ena priprava = eno odštevanje iz shrambe)
public class PripravaRecepta
{
    public int Id { get; set; }
    public int ReceptId { get; set; }
    public Recept Recept { get; set; } = null!;

    public int SteviloPorcij { get; set; }
    public DateTime SkuhanoOb { get; set; }

    // neobvezen ključ, ki ga pošlje odjemalec; ponovljen zahtevek z istim ključem ne odšteje še enkrat
    public string? Kljuc { get; set; }
}
