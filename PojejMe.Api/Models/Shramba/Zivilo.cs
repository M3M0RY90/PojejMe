namespace PojejMe.Api.Models.Shramba;

// živilo iz skupne baze živil (šifrant), ki ga ureja administrator
// hranilne vrednosti so na 100 g živila
public class Zivilo
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";

    public int SkupinaId { get; set; }
    public SkupinaZivil Skupina { get; set; } = null!;

    public decimal EnergijaKcal { get; set; }
    public decimal Beljakovine { get; set; }
    public decimal OgljikoviHidrati { get; set; }
    public decimal Sladkorji { get; set; }
    public decimal Mascobe { get; set; }
    public decimal NasiceneMascobe { get; set; }
    public decimal Vlaknine { get; set; }
    public decimal Sol { get; set; }

    // povezava več-na-več: eno živilo ima lahko več alergenov
    public List<Alergen> Alergeni { get; set; } = [];
}
