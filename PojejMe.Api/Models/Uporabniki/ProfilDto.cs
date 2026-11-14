namespace PojejMe.Api.Models.Uporabniki;

// kar uporabnik pošlje pri urejanju profila (UZ-1.2)
// seznama nadomestita prejšnje izbire; prazen seznam pomeni "brez omejitev" (pogoj 2)
public class ProfilVnos
{
    public List<int> PrehranskeOmejitveIds { get; set; } = [];
    public List<int> AlergenIds { get; set; } = [];
}

public record ProfilPostavka(int Id, string Ime);

public record ProfilOdgovor(
    int Id,
    string Ime,
    string? Email,
    List<ProfilPostavka> PrehranskeOmejitve,
    List<ProfilPostavka> Alergeni)
{
    public static ProfilOdgovor Iz(Uporabnik u) => new(
        u.Id, u.Ime, u.Email,
        u.PrehranskeOmejitve.OrderBy(o => o.Id).Select(o => new ProfilPostavka(o.Id, o.Ime)).ToList(),
        u.Alergeni.OrderBy(a => a.Id).Select(a => new ProfilPostavka(a.Id, a.Ime)).ToList());
}
