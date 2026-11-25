namespace PojejMe.Api.Models.Recepti;

// UZ-3.2: kalorije in makrohranila
public record Hranila(decimal EnergijaKcal, decimal Beljakovine, decimal OgljikoviHidrati, decimal Mascobe);

// kar vrne GET /api/recepti/{id}/hranilna-vrednost
public record HranilnaVrednostOdgovor(
    int ReceptId,
    string Recept,
    int SteviloPorcij,
    Hranila Skupaj,
    Hranila NaPorcijo,
    List<string> Opozorila);
