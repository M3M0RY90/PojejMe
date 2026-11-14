using System.ComponentModel.DataAnnotations;

namespace PojejMe.Api.Models.Uporabniki;

// kar uporabnik pošlje pri registraciji (UZ-1.1a)
public class RegistracijaVnos
{
    [Required, StringLength(50, MinimumLength = 1, ErrorMessage = "Ime mora imeti od 1 do 50 znakov.")]
    public string Ime { get; set; } = "";

    [Required, EmailAddress(ErrorMessage = "Neveljavna oblika e-pošte."), StringLength(255)]
    public string Email { get; set; } = "";

    // pogoj 1 pri UZ-1.1a: najmanj 8 znakov
    [Required, MinLength(8, ErrorMessage = "Geslo mora imeti vsaj 8 znakov.")]
    public string Geslo { get; set; } = "";
}

public class PrijavaVnos
{
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Geslo { get; set; } = "";
}

public record PrijavaOdgovor(string Zeton, DateTime VeljavenDo);

public class PozabljenoGesloVnos
{
    [Required]
    public string Email { get; set; } = "";
}

// odgovor je vedno enak, ne glede na to, ali e-pošta obstaja (pogoj 8 pri UZ-1.1)
// RazvojniZeton je izpolnjen samo, če je v nastavitvah vklopljen Ponastavitev:ZetonVOdgovoru (lokalni razvoj)
public record PozabljenoGesloOdgovor(string Sporocilo, string? RazvojniZeton = null);

public class PonastavitevVnos
{
    [Required]
    public string Zeton { get; set; } = "";

    [Required, MinLength(8, ErrorMessage = "Geslo mora imeti vsaj 8 znakov.")]
    public string NovoGeslo { get; set; } = "";
}

// gesla ali njegovega zgoščenega zapisa nikoli ne vračamo
public record UporabnikOdgovor(int Id, string Ime, string? Email, int GospodinjstvoId)
{
    public static UporabnikOdgovor Iz(Uporabnik u) => new(u.Id, u.Ime, u.Email, u.GospodinjstvoId);
}
