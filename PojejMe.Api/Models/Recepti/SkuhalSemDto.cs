using System.ComponentModel.DataAnnotations;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Models.Recepti;

// UZ-3.7: kar pošlje odjemalec pri POST /api/recepti/{id}/skuhal-sem (telo je neobvezno)
public class SkuhalSemVnos
{
    // koliko porcij je bilo skuhanih; privzeto toliko, kot jih ima recept
    [Range(1, 50, ErrorMessage = "Število porcij mora biti med 1 in 50.")]
    public int? SteviloPorcij { get; set; }

    // če v shrambi ni vseh sestavin: false -> zavrnemo (409), true -> odštejemo, kar je na voljo
    public bool KljubTemu { get; set; }

    // ključ priprave (npr. GUID), da se ob dvojnem kliku sestavine ne odštejejo dvakrat
    [StringLength(64, ErrorMessage = "Ključ je lahko dolg največ 64 znakov.")]
    public string? Kljuc { get; set; }
}

public record PorabaSestavine(int ZiviloId, string Zivilo, decimal Porabljeno, Enota Enota);

public record PripravaOdgovor(
    int PripravaId,
    int ReceptId,
    string Recept,
    int SteviloPorcij,
    DateTime SkuhanoOb,
    bool ZeZabelezeno,
    List<PorabaSestavine> Poraba,
    List<ManjkajocaSestavina> Manjka);

public record SkuhalSemZavrnitev(string Sporocilo, List<ManjkajocaSestavina> Manjka);
