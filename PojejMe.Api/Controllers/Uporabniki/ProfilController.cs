using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Controllers.Uporabniki;

// UZ-1.2 Uporabniški profil: prehranske omejitve in alergeni prijavljenega uporabnika
// alergene izbiramo iz obstoječega šifranta GET /api/zivila/alergeni, omejitve iz GET /api/profil/omejitve
[ApiController]
[Authorize]
[Route("api/profil")]
public class ProfilController(PojejMeDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfilOdgovor>> Pridobi()
    {
        var uporabnik = await NaloziUporabnika();
        return uporabnik is null ? Unauthorized() : ProfilOdgovor.Iz(uporabnik);
    }

    [HttpPut]
    public async Task<ActionResult<ProfilOdgovor>> Uredi(ProfilVnos vnos)
    {
        var uporabnik = await NaloziUporabnika();
        if (uporabnik is null) return Unauthorized();

        // vsak poslani Id mora obstajati v šifrantu (pogoj 5); Distinct, da dvakrat poslan Id ne pokvari primerjave
        var omejitveIds = vnos.PrehranskeOmejitveIds.Distinct().ToList();
        var omejitve = await db.PrehranskeOmejitve.Where(o => omejitveIds.Contains(o.Id)).ToListAsync();
        if (omejitve.Count != omejitveIds.Count)
            return ValidationProblem(Napaka(nameof(vnos.PrehranskeOmejitveIds), "Neznana prehranska omejitev."));

        var alergenIds = vnos.AlergenIds.Distinct().ToList();
        var alergeni = await db.Alergeni.Where(a => alergenIds.Contains(a.Id)).ToListAsync();
        if (alergeni.Count != alergenIds.Count)
            return ValidationProblem(Napaka(nameof(vnos.AlergenIds), "Neznan alergen."));

        // nadomestimo celoten seznam: EF sam izbriše odvečne povezave in doda nove
        uporabnik.PrehranskeOmejitve.Clear();
        uporabnik.PrehranskeOmejitve.AddRange(omejitve);
        uporabnik.Alergeni.Clear();
        uporabnik.Alergeni.AddRange(alergeni);
        await db.SaveChangesAsync();

        return ProfilOdgovor.Iz(uporabnik);
    }

    // šifrant omejitev za izbirni seznam v profilu; dostopen brez prijave, tako kot šifrant alergenov
    [AllowAnonymous]
    [HttpGet("omejitve")]
    public Task<List<ProfilPostavka>> Omejitve() =>
        db.PrehranskeOmejitve.OrderBy(o => o.Id).Select(o => new ProfilPostavka(o.Id, o.Ime)).ToListAsync();

    // ---------- pomožne metode ----------

    private Task<Uporabnik?> NaloziUporabnika()
    {
        var id = int.Parse(User.FindFirstValue("sub")!);
        return db.Uporabniki
            .Include(u => u.PrehranskeOmejitve)
            .Include(u => u.Alergeni)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    private static ValidationProblemDetails Napaka(string polje, string sporocilo) =>
        new(new Dictionary<string, string[]> { [polje] = [sporocilo] });
}
