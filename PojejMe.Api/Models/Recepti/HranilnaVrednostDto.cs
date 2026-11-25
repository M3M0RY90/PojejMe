using PojejMe.Api.Models.Shramba;

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

// UZ-3.3: koliko k hranilni vrednosti prispeva posamezna sestavina (null, če je v kosih)
public record PrispevekSestavine(int ZiviloId, string Zivilo, decimal Kolicina, Enota Enota, Hranila? Hranila);

// UZ-3.3: samodejno izračunane hranilne vrednosti, ki so del vsakega odgovora z receptom
public record PovzetekHranil(
    Hranila Skupaj,
    Hranila NaPorcijo,
    List<PrispevekSestavine> Sestavine,
    List<string> Opozorila);
