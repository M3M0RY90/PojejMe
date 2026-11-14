using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Services;

// ustvarja JWT žetone ob prijavi
// žeton nosi Id uporabnika ("sub") in ZetonVerzija ("verzija"); odjava verzijo poveča, s tem pa stari žetoni zastarajo
public class ZetonStoritev(SymmetricSecurityKey podpisniKljuc)
{
    public const string Izdajatelj = "PojejMe";
    public static readonly TimeSpan Veljavnost = TimeSpan.FromHours(2);

    public (string Zeton, DateTime VeljavenDo) Ustvari(Uporabnik uporabnik)
    {
        var veljavenDo = DateTime.UtcNow.Add(Veljavnost);

        var zeton = new JwtSecurityToken(
            issuer: Izdajatelj,
            audience: Izdajatelj,
            claims:
            [
                new Claim("sub", uporabnik.Id.ToString()),
                new Claim("verzija", uporabnik.ZetonVerzija.ToString())
            ],
            expires: veljavenDo,
            signingCredentials: new SigningCredentials(podpisniKljuc, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(zeton), veljavenDo);
    }
}
