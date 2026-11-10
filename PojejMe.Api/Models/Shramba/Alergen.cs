namespace PojejMe.Api.Models.Shramba;

// šifrant 14 alergenov, ki jih mora po uredbi EU 1169/2011 navajati označba živil
public class Alergen
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";

    public List<Zivilo> Zivila { get; set; } = [];
}
