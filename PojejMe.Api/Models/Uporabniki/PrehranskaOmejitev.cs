namespace PojejMe.Api.Models.Uporabniki;

// šifrant prehranskih omejitev, iz katerega uporabnik izbira v profilu (UZ-1.2)
// alergeni so v svojem šifrantu (Alergen), ker jih potrebuje tudi baza živil
public class PrehranskaOmejitev
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";
}
