using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Models.Recepti;

// UZ-3.8: sestavina recepta, ki je v shrambi ni (dovolj); Dokupiti = Potrebno - VShrambi (v enoti recepta)
public record ManjkajocaPostavka(int ZiviloId, string Zivilo, decimal Potrebno, decimal VShrambi, decimal Dokupiti, Enota Enota);

// GET /api/recepti/{id}/manjkajoce
public record ManjkajoceOdgovor(int ReceptId, string Recept, bool ImamVse, List<ManjkajocaPostavka> Manjka);

// POST /api/recepti/{id}/manjkajoce-na-seznam
public record NaSeznamOdgovor(
    int ReceptId,
    string Recept,
    List<ManjkajocaPostavka> Dodano,
    List<ManjkajocaPostavka> ZeNaSeznamu,
    string Sporocilo);
