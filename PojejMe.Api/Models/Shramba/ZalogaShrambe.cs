using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Models.Shramba;

public enum Lokacija { Hladilnik, Zamrzovalnik, Shramba }

public enum Enota { g, kg, ml, l, kos }

// eno živilo v shrambi gospodinjstva (npr. 1 l mleka v hladilniku do 20. 11.)
public class ZalogaShrambe
{
    public int Id { get; set; }

    public int GospodinjstvoId { get; set; }
    public Gospodinjstvo Gospodinjstvo { get; set; } = null!;

    public int ZiviloId { get; set; }
    public Zivilo Zivilo { get; set; } = null!;

    public decimal Kolicina { get; set; }
    public Enota Enota { get; set; }
    public Lokacija Lokacija { get; set; }

    // rok ni obvezen (npr. sol ali sladkor nimata pravega roka)
    public DateOnly? RokUporabnosti { get; set; }

    public int DodalId { get; set; }
    public Uporabnik Dodal { get; set; } = null!;
    public DateTime DodanoOb { get; set; }
}
