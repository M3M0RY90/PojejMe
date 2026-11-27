namespace PojejMe.Api.Models.Recepti;

// UZ-3.5: živilo v shrambi, ki mu rok uporabnosti poteče v izbranem številu dni
public record KriticnoZivilo(int ZiviloId, string Zivilo, DateOnly RokUporabnosti, int DniDoRoka);

// UZ-3.5: recept, ki porabi vsaj eno živilo s kratkim rokom
public record ReceptResiHrano(
    int ReceptId,
    string Ime,
    DateOnly NajblizjiRok,
    int DniDoRoka,
    List<KriticnoZivilo> ZivilaZRokom);

// kar vrne GET /api/recepti/resi-hrano
public record ResiHranoOdgovor(int Dni, List<KriticnoZivilo> Zivila, List<ReceptResiHrano> Recepti);
