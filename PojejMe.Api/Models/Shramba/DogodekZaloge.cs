using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Models.Shramba;

public enum VrstaDogodka { Porabljeno, Zavrzeno }

// UZ-2.7: zapis, da je bil del zaloge porabljen ali zavržen (podatki za statistiko zavržkov)
// zapis v shrambi po porabi izgine, dogodek pa ostane: živilo je povezano z bazo živil, enoto hranimo pri dogodku
public class DogodekZaloge
{
    public int Id { get; set; }

    public int GospodinjstvoId { get; set; }
    public Gospodinjstvo Gospodinjstvo { get; set; } = null!;

    public int ZiviloId { get; set; }
    public Zivilo Zivilo { get; set; } = null!;

    public decimal Kolicina { get; set; }
    public Enota Enota { get; set; }
    public VrstaDogodka Vrsta { get; set; }

    // neobvezno, samo pri zavržku (npr. "pokvarjeno", "preteklo")
    public string? Razlog { get; set; }

    public int UporabnikId { get; set; }
    public Uporabnik Uporabnik { get; set; } = null!;
    public DateTime Cas { get; set; }
}
