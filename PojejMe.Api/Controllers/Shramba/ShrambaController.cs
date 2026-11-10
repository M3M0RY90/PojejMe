using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Shramba;
using PojejMe.Api.Models.Uporabniki;

namespace PojejMe.Api.Controllers.Shramba;

// UZ-2.2 Dodajanje v shrambo
// shramba pripada gospodinjstvu: vsi člani istega gospodinjstva vidijo iste zaloge
// uporabnika zaenkrat prepoznamo po glavi X-Uporabnik-Id; ko sklop 1 doda prijavo, ga vzamemo iz prijave
[ApiController]
[Route("api/shramba")]
public class ShrambaController(PojejMeDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ZalogaOdgovor>>> Seznam(
        [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var zaloge = await db.ZalogeShrambe
            .Include(z => z.Zivilo).Include(z => z.Dodal)
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId)
            .OrderBy(z => z.Id)
            .ToListAsync();

        return zaloge.Select(z => ZalogaOdgovor.Iz(z)).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ZalogaOdgovor>> Podrobno(
        int id, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        // zaloge drugega gospodinjstva se ne vidi, zato zanjo vrnemo 404
        var zaloga = await db.ZalogeShrambe
            .Include(z => z.Zivilo).Include(z => z.Dodal)
            .FirstOrDefaultAsync(z => z.Id == id && z.GospodinjstvoId == uporabnik.GospodinjstvoId);

        return zaloga is null ? NotFound() : ZalogaOdgovor.Iz(zaloga);
    }

    [HttpPost]
    public async Task<ActionResult<ZalogaOdgovor>> Dodaj(
        ZalogaVnos vnos, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var zivilo = await db.Zivila.FindAsync(vnos.ZiviloId);
        if (zivilo is null)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(vnos.ZiviloId)] = ["Živilo ne obstaja v bazi živil."]
            }));

        var zaloga = new ZalogaShrambe
        {
            GospodinjstvoId = uporabnik.GospodinjstvoId,
            Zivilo = zivilo,
            Kolicina = vnos.Kolicina,
            Enota = vnos.Enota!.Value,
            Lokacija = vnos.Lokacija!.Value,
            RokUporabnosti = vnos.RokUporabnosti,
            Dodal = uporabnik,
            DodanoOb = DateTime.UtcNow
        };
        db.ZalogeShrambe.Add(zaloga);
        await db.SaveChangesAsync();

        // rok v preteklosti: vnos dovolimo, a uporabnika opozorimo (pogoj 4 pri UZ-2.2)
        var danes = DateOnly.FromDateTime(DateTime.UtcNow);
        var opozorilo = vnos.RokUporabnosti < danes ? "Rok uporabnosti je že potekel." : null;

        return CreatedAtAction(nameof(Podrobno), new { id = zaloga.Id }, ZalogaOdgovor.Iz(zaloga, opozorilo));
    }

    private Task<Uporabnik?> NaloziUporabnika(int? id) =>
        id is null ? Task.FromResult<Uporabnik?>(null) : db.Uporabniki.FirstOrDefaultAsync(u => u.Id == id);

    private ObjectResult NeznanUporabnik() => Problem(
        title: "Neznan uporabnik",
        detail: "V glavi X-Uporabnik-Id pošlji Id obstoječega uporabnika.",
        statusCode: StatusCodes.Status401Unauthorized);
}
