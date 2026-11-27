namespace PojejMe.Api.Models.Shramba;

// nakupovalni seznam gospodinjstva (osnovna različica, dodana pri UZ-3.8; UZ-2.9 jo lahko razširi)
public class PostavkaNakupovalnegaSeznama
{
    public int Id { get; set; }
    public int GospodinjstvoId { get; set; }

    public int ZiviloId { get; set; }
    public Zivilo Zivilo { get; set; } = null!;

    public decimal Kolicina { get; set; }
    public Enota Enota { get; set; }

    public DateTime DodanoOb { get; set; }

    // UZ-3.8: iz katerega recepta je bila postavka dodana (null pri ročnem vnosu)
    public int? IzReceptaId { get; set; }
}

public record PostavkaSeznamaOdgovor(
    int Id,
    int ZiviloId,
    string Zivilo,
    decimal Kolicina,
    Enota Enota,
    int? IzReceptaId,
    DateTime DodanoOb)
{
    public static PostavkaSeznamaOdgovor Iz(PostavkaNakupovalnegaSeznama p) =>
        new(p.Id, p.ZiviloId, p.Zivilo.Ime, p.Kolicina, p.Enota, p.IzReceptaId, p.DodanoOb);
}
