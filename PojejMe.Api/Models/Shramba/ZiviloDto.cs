using System.ComponentModel.DataAnnotations;

namespace PojejMe.Api.Models.Shramba;

// podatki, ki jih administrator pošlje pri dodajanju ali urejanju živila
// [Range(0, ...)] zavrne negativne hranilne vrednosti še preden pridemo v kontroler (odgovor 400)
public class ZiviloVnos
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Ime { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Izberi skupino živila.")]
    public int SkupinaId { get; set; }

    [Range(0, 900, ErrorMessage = "Energijska vrednost ne sme biti negativna.")]
    public decimal EnergijaKcal { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal Beljakovine { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal OgljikoviHidrati { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal Sladkorji { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal Mascobe { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal NasiceneMascobe { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal Vlaknine { get; set; }

    [Range(0, 100, ErrorMessage = "Vrednost na 100 g mora biti med 0 in 100.")]
    public decimal Sol { get; set; }

    public List<int> AlergenIds { get; set; } = [];
}

// kar API vrne: namesto samih Id-jev tudi imena skupine in alergenov
public record ZiviloOdgovor(
    int Id,
    string Ime,
    int SkupinaId,
    string Skupina,
    decimal EnergijaKcal,
    decimal Beljakovine,
    decimal OgljikoviHidrati,
    decimal Sladkorji,
    decimal Mascobe,
    decimal NasiceneMascobe,
    decimal Vlaknine,
    decimal Sol,
    List<string> Alergeni)
{
    public static ZiviloOdgovor Iz(Zivilo z) => new(
        z.Id, z.Ime, z.SkupinaId, z.Skupina.Ime,
        z.EnergijaKcal, z.Beljakovine, z.OgljikoviHidrati, z.Sladkorji,
        z.Mascobe, z.NasiceneMascobe, z.Vlaknine, z.Sol,
        z.Alergeni.OrderBy(a => a.Id).Select(a => a.Ime).ToList());
}
