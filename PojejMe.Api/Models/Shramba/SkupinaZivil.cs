namespace PojejMe.Api.Models.Shramba;

// šifrant skupin živil (sadje, zelenjava, mlečni izdelki ...), vnaprej napolnjen v PojejMeDbContext
public class SkupinaZivil
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";
}
