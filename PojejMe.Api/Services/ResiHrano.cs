using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Services;

// UZ-3.5 Reši hrano: prednostni predlogi receptov, ki porabijo živila s kratkim rokom uporabnosti.
// Kritično je živilo, ki mu rok poteče danes ali v naslednjih "dni" dneh (pretečena živila niso več kritična,
// živila brez roka ali z daljšim rokom pa ne).
public static class ResiHrano
{
    public static ResiHranoOdgovor Predlagaj(List<Recept> recepti, List<ZalogaShrambe> zaloge, DateOnly danes, int dni)
    {
        var meja = danes.AddDays(dni);

        // za vsako živilo vzamemo najbližji rok med vsemi njegovimi zalogami
        var kriticna = zaloge
            .Where(z => z.RokUporabnosti is not null && z.RokUporabnosti >= danes && z.RokUporabnosti <= meja)
            .GroupBy(z => z.ZiviloId)
            .Select(g =>
            {
                var rok = g.Min(z => z.RokUporabnosti!.Value);
                return new KriticnoZivilo(g.Key, g.First().Zivilo.Ime, rok, rok.DayNumber - danes.DayNumber);
            })
            .OrderBy(k => k.RokUporabnosti).ThenBy(k => k.Zivilo)
            .ToList();

        var poZivilu = kriticna.ToDictionary(k => k.ZiviloId);

        var predlogi = new List<ReceptResiHrano>();
        foreach (var recept in recepti)
        {
            var zivilaZRokom = recept.Sestavine
                .Select(s => s.ZiviloId).Distinct()
                .Where(poZivilu.ContainsKey)
                .Select(id => poZivilu[id])
                .OrderBy(k => k.RokUporabnosti)
                .ToList();

            if (zivilaZRokom.Count == 0) continue;

            var najblizji = zivilaZRokom[0];
            predlogi.Add(new ReceptResiHrano(recept.Id, recept.Ime, najblizji.RokUporabnosti, najblizji.DniDoRoka, zivilaZRokom));
        }

        // najprej recepti z živilom, ki se najprej pokvari; pri enakem roku tisti, ki reši več živil
        var razvrsceni = predlogi
            .OrderBy(p => p.NajblizjiRok)
            .ThenByDescending(p => p.ZivilaZRokom.Count)
            .ThenBy(p => p.Ime)
            .ToList();

        return new ResiHranoOdgovor(dni, kriticna, razvrsceni);
    }
}
