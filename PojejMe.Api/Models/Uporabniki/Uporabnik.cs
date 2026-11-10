namespace PojejMe.Api.Models.Uporabniki;

// MINIMALNA različica za potrebe sklopa 2 (kdo je dodal živilo, kateremu gospodinjstvu pripada)
// sklop 1 doda e-pošto, geslo, prijavo ...
public class Uporabnik
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";

    public int GospodinjstvoId { get; set; }
    public Gospodinjstvo Gospodinjstvo { get; set; } = null!;
}
