namespace PojejMe.Api.Models.Uporabniki;

// sklop 1 razširja minimalno različico iz sklopa 2 (kdo je dodal živilo, kateremu gospodinjstvu pripada)
// obstoječih lastnosti (Id, Ime, GospodinjstvoId, Gospodinjstvo) ne preimenujte, ker jih uporablja shramba
public class Uporabnik
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";

    // UZ-1.1 prijava: pri testnih uporabnikih (Ana, Bor, Cene) sta prazna, zato se z njimi ni mogoče prijaviti,
    // shramba pa jih še vedno prepozna po glavi X-Uporabnik-Id
    public string? Email { get; set; }
    public string? GesloHash { get; set; }

    // ob vsaki odjavi (in ponastavitvi gesla) se poveča; žeton z manjšo številko ne velja več
    public int ZetonVerzija { get; set; }

    // ponastavitev gesla: v bazi hranimo samo zgoščeno vrednost žetona, tako kot pri geslu
    public string? PonastavitevHash { get; set; }
    public DateTime? PonastavitevPoteceOb { get; set; }

    public int GospodinjstvoId { get; set; }
    public Gospodinjstvo Gospodinjstvo { get; set; } = null!;
}
