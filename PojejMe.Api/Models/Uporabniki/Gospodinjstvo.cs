namespace PojejMe.Api.Models.Uporabniki;

// MINIMALNA različica za potrebe sklopa 2 (shramba pripada gospodinjstvu)
// sklop 1 jo razširi (povabila, vloge ...); obstoječih lastnosti ne preimenujte, ker jih uporablja shramba
public class Gospodinjstvo
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";

    public List<Uporabnik> Clani { get; set; } = [];
}
