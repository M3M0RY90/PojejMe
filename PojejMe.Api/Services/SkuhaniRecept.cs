using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// UZ-3.7 Skuhal sem: izračuna, koliko je treba odšteti od posameznih zalog v shrambi.
// Najprej porabimo zaloge z najbližjim rokom uporabnosti (manj zavržkov), zaloge brez roka nazadnje.
// Metoda shrambe ne spreminja; vrne načrt, ki ga controller uveljavi samo, če je priprava dovoljena.
public static class SkuhaniRecept
{
    public record OdstetjeZaloge(ZalogaShrambe Zaloga, decimal Odstej);

    public record Nacrt(List<OdstetjeZaloge> Odstetja, List<PorabaSestavine> Poraba, List<ManjkajocaSestavina> Manjka);

    public static Nacrt Nacrtuj(Recept recept, List<ZalogaShrambe> zaloge, int porcije)
    {
        var faktor = (decimal)porcije / Math.Max(recept.SteviloPorcij, 1);
        var preostanek = zaloge.ToDictionary(z => z.Id, z => z.Kolicina);

        var odstetja = new List<OdstetjeZaloge>();
        var poraba = new List<PorabaSestavine>();
        var manjka = new List<ManjkajocaSestavina>();

        foreach (var s in recept.Sestavine.OrderBy(s => s.Id))
        {
            var potrebno = Math.Round(s.Kolicina * faktor, 2);
            var ostane = potrebno;

            var zalogeZivila = zaloge
                .Where(z => z.ZiviloId == s.ZiviloId)
                .OrderBy(z => z.RokUporabnosti ?? DateOnly.MaxValue)
                .ThenBy(z => z.Id);

            foreach (var z in zalogeZivila)
            {
                if (ostane <= 0) break;

                var vZalogi = Enote.Pretvori(preostanek[z.Id], z.Enota, s.Enota);
                if (vZalogi is null || vZalogi <= 0) continue;

                var vzemi = Math.Min(ostane, vZalogi.Value);
                var odstej = Math.Min(Math.Round(Enote.Pretvori(vzemi, s.Enota, z.Enota)!.Value, 2), preostanek[z.Id]);

                preostanek[z.Id] -= odstej;
                odstetja.Add(new OdstetjeZaloge(z, odstej));
                ostane -= vzemi;
            }

            var porabljeno = potrebno - Math.Max(ostane, 0);
            if (porabljeno > 0)
                poraba.Add(new PorabaSestavine(s.ZiviloId, s.Zivilo.Ime, porabljeno, s.Enota));
            if (ostane > 0)
                manjka.Add(new ManjkajocaSestavina(s.ZiviloId, s.Zivilo.Ime, potrebno, s.Enota, porabljeno));
        }

        return new Nacrt(odstetja, poraba, manjka);
    }
}
