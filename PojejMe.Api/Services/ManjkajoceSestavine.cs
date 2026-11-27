using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// UZ-3.8: katere sestavine recepta manjkajo v shrambi in koliko jih je treba dokupiti.
// Razpoložljivost se računa enako kot pri UZ-3.4 (seštevek zalog, pretvorba enot, brez pretečenih živil).
public static class ManjkajoceSestavine
{
    public static List<ManjkajocaPostavka> Izracunaj(Recept recept, List<ZalogaShrambe> zaloge)
    {
        var manjka = new List<ManjkajocaPostavka>();

        foreach (var s in recept.Sestavine.OrderBy(s => s.Id))
        {
            var vShrambi = Math.Round(ReceptiIzShrambe.KolicinaVShrambi(zaloge, s.ZiviloId, s.Enota), 2);
            if (vShrambi >= s.Kolicina) continue;

            manjka.Add(new ManjkajocaPostavka(s.ZiviloId, s.Zivilo.Ime, s.Kolicina, vShrambi,
                s.Kolicina - vShrambi, s.Enota));
        }

        return manjka;
    }
}
