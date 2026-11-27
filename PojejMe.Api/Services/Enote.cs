using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// skupna pretvorba enot za recepte in shrambo:
// g <-> kg, ml <-> l, ml in l štejemo kot g in kg (gostota približno 1 g/ml), kos samo s kos
public static class Enote
{
    public static decimal? VGrame(decimal kolicina, Enota enota) => enota switch
    {
        Enota.g or Enota.ml => kolicina,
        Enota.kg or Enota.l => kolicina * 1000,
        _ => null
    };

    // vrne null, če enot ni mogoče pretvoriti (npr. kos v g)
    public static decimal? Pretvori(decimal kolicina, Enota iz, Enota v)
    {
        if (iz == v) return kolicina;
        var grami = VGrame(kolicina, iz);
        if (grami is null || v == Enota.kos) return null;
        return v is Enota.kg or Enota.l ? grami.Value / 1000 : grami.Value;
    }
}
