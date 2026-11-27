using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// UZ-3.4: predlogi receptov glede na to, kaj je v shrambi gospodinjstva.
// Sestavina je na voljo, če je v shrambi vsaj toliko, kot jo zahteva recept
// (zaloge istega živila se seštejejo, enote se pretvorijo: g/kg, ml/l, ml ~ g; kos samo s kos).
public static class ReceptiIzShrambe
{
    public static List<ReceptIzShrambeOdgovor> Predlagaj(List<Recept> recepti, List<ZalogaShrambe> zaloge, bool vsi)
    {
        var predlogi = new List<ReceptIzShrambeOdgovor>();

        foreach (var recept in recepti)
        {
            if (recept.Sestavine.Count == 0) continue;

            var naVoljo = new List<string>();
            var manjka = new List<ManjkajocaSestavina>();
            var vsebujeSestavinoIzShrambe = false;

            foreach (var s in recept.Sestavine.OrderBy(s => s.Id))
            {
                var vShrambi = KolicinaVShrambi(zaloge, s.ZiviloId, s.Enota);
                if (vShrambi > 0) vsebujeSestavinoIzShrambe = true;

                if (vShrambi >= s.Kolicina)
                    naVoljo.Add(s.Zivilo.Ime);
                else
                    manjka.Add(new ManjkajocaSestavina(s.ZiviloId, s.Zivilo.Ime, s.Kolicina, s.Enota, vShrambi));
            }

            // privzeto predlagamo samo recepte, ki vsebujejo vsaj eno sestavino iz shrambe
            if (!vsi && !vsebujeSestavinoIzShrambe) continue;

            var odstotek = (int)Math.Round(100m * naVoljo.Count / recept.Sestavine.Count, MidpointRounding.AwayFromZero);
            predlogi.Add(new ReceptIzShrambeOdgovor(recept.Id, recept.Ime, recept.Sestavine.Count,
                naVoljo.Count, odstotek, manjka.Count == 0, naVoljo, manjka));
        }

        // najprej recepti, za katere ima uporabnik največ sestavin
        return predlogi.OrderByDescending(p => p.OdstotekNaVoljo).ThenBy(p => p.Ime).ToList();
    }

    // vsota vseh zalog tega živila, pretvorjena v enoto recepta
    private static decimal KolicinaVShrambi(List<ZalogaShrambe> zaloge, int ziviloId, Enota enotaRecepta)
    {
        decimal vsota = 0;
        foreach (var z in zaloge.Where(z => z.ZiviloId == ziviloId))
        {
            var pretvorjeno = Enote.Pretvori(z.Kolicina, z.Enota, enotaRecepta);
            if (pretvorjeno is not null) vsota += pretvorjeno.Value;
        }
        return vsota;
    }
}
