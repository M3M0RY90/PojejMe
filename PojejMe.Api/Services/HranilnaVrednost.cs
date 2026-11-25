using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// UZ-3.2: izračun hranilne vrednosti recepta iz njegovih sestavin.
// Hranilne vrednosti živil so v bazi podane na 100 g.
public static class HranilnaVrednost
{
    public static HranilnaVrednostOdgovor Izracunaj(Recept recept)
    {
        decimal kcal = 0, beljakovine = 0, ogljikoviHidrati = 0, mascobe = 0;
        var opozorila = new List<string>();

        foreach (var s in recept.Sestavine)
        {
            var grami = VGrame(s.Kolicina, s.Enota);
            if (grami is null)
            {
                opozorila.Add($"Sestavina {s.Zivilo.Ime} je podana v kosih, zato ni upoštevana v izračunu.");
                continue;
            }

            var faktor = grami.Value / 100m;
            kcal += s.Zivilo.EnergijaKcal * faktor;
            beljakovine += s.Zivilo.Beljakovine * faktor;
            ogljikoviHidrati += s.Zivilo.OgljikoviHidrati * faktor;
            mascobe += s.Zivilo.Mascobe * faktor;
        }

        var porcije = Math.Max(recept.SteviloPorcij, 1);
        var skupaj = new Hranila(Zaokrozi(kcal), Zaokrozi(beljakovine), Zaokrozi(ogljikoviHidrati), Zaokrozi(mascobe));
        var naPorcijo = new Hranila(Zaokrozi(kcal / porcije), Zaokrozi(beljakovine / porcije),
            Zaokrozi(ogljikoviHidrati / porcije), Zaokrozi(mascobe / porcije));

        return new HranilnaVrednostOdgovor(recept.Id, recept.Ime, recept.SteviloPorcij, skupaj, naPorcijo, opozorila);
    }

    // ml in l štejemo kot g in kg (gostota približno 1 g/ml); za kos masa ni znana
    private static decimal? VGrame(decimal kolicina, Enota enota) => enota switch
    {
        Enota.g or Enota.ml => kolicina,
        Enota.kg or Enota.l => kolicina * 1000,
        _ => null
    };

    private static decimal Zaokrozi(decimal vrednost) => Math.Round(vrednost, 1, MidpointRounding.AwayFromZero);
}
