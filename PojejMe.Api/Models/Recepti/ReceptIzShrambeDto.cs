using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Models.Recepti;

// UZ-3.4: sestavina recepta, ki je v shrambi ni ali je je premalo
// VShrambi je količina v shrambi, pretvorjena v enoto, ki jo zahteva recept
public record ManjkajocaSestavina(int ZiviloId, string Zivilo, decimal Potrebno, Enota Enota, decimal VShrambi);

// UZ-3.4: predlog recepta glede na sestavine v shrambi gospodinjstva
public record ReceptIzShrambeOdgovor(
    int ReceptId,
    string Ime,
    int SteviloSestavin,
    int SestavinNaVoljo,
    int OdstotekNaVoljo,
    bool ImamVse,
    List<string> NaVoljo,
    List<ManjkajocaSestavina> Manjka);
