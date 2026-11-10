using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Controllers.Shramba;

// UZ-2.1 Urejanje živil: baza živil, ki jo ureja administrator
// branje je dovoljeno vsem, dodajanje/urejanje/brisanje samo administratorju
[ApiController]
[Route("api/zivila")]
public class ZivilaController(PojejMeDbContext db, IConfiguration config) : ControllerBase
{
    // ---------- branje ----------

    [HttpGet]
    public async Task<List<ZiviloOdgovor>> Seznam([FromQuery] string? isci, [FromQuery] int? skupinaId)
    {
        var poizvedba = db.Zivila.Include(z => z.Skupina).Include(z => z.Alergeni).AsQueryable();

        if (!string.IsNullOrWhiteSpace(isci))
            poizvedba = poizvedba.Where(z => z.Ime.Contains(isci));
        if (skupinaId is not null)
            poizvedba = poizvedba.Where(z => z.SkupinaId == skupinaId);

        var zivila = await poizvedba.OrderBy(z => z.Ime).ToListAsync();
        return zivila.Select(ZiviloOdgovor.Iz).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ZiviloOdgovor>> Podrobno(int id)
    {
        var zivilo = await NaloziZivilo(id);
        return zivilo is null ? NotFound() : ZiviloOdgovor.Iz(zivilo);
    }

    // šifranta, iz katerih izbiramo pri vnosu živila
    [HttpGet("skupine")]
    public Task<List<SkupinaZivil>> Skupine() => db.SkupineZivil.OrderBy(s => s.Id).ToListAsync();

    [HttpGet("alergeni")]
    public Task<List<AlergenOdgovor>> Alergeni() =>
        db.Alergeni.OrderBy(a => a.Id).Select(a => new AlergenOdgovor(a.Id, a.Ime)).ToListAsync();

    // ---------- urejanje (samo administrator) ----------

    [HttpPost]
    public async Task<ActionResult<ZiviloOdgovor>> Dodaj(
        ZiviloVnos vnos, [FromHeader(Name = "X-Admin-Kljuc")] string? adminKljuc)
    {
        if (!JeAdministrator(adminKljuc)) return Zavrnjeno();

        var zivilo = new Zivilo();
        var napaka = await Prepisi(vnos, zivilo);
        if (napaka is not null) return napaka;

        db.Zivila.Add(zivilo);
        await db.SaveChangesAsync();

        // 201 Created + glava Location s potjo do novega živila
        return CreatedAtAction(nameof(Podrobno), new { id = zivilo.Id }, ZiviloOdgovor.Iz(zivilo));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ZiviloOdgovor>> Uredi(
        int id, ZiviloVnos vnos, [FromHeader(Name = "X-Admin-Kljuc")] string? adminKljuc)
    {
        if (!JeAdministrator(adminKljuc)) return Zavrnjeno();

        var zivilo = await NaloziZivilo(id);
        if (zivilo is null) return NotFound();

        var napaka = await Prepisi(vnos, zivilo);
        if (napaka is not null) return napaka;

        await db.SaveChangesAsync();
        return ZiviloOdgovor.Iz(zivilo);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Izbrisi(int id, [FromHeader(Name = "X-Admin-Kljuc")] string? adminKljuc)
    {
        if (!JeAdministrator(adminKljuc)) return Zavrnjeno();

        var zivilo = await db.Zivila.FindAsync(id);
        if (zivilo is null) return NotFound();

        db.Zivila.Remove(zivilo);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // baza brisanje zavrne, če živilo še kdo uporablja (npr. v shrambi gospodinjstva, UZ-2.2)
            return Conflict(new ProblemDetails
            {
                Title = "Živila ni mogoče izbrisati",
                Detail = "Živilo je v uporabi (npr. v shrambi kakšnega gospodinjstva).",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }

    // ---------- pomožne metode ----------

    private Task<Zivilo?> NaloziZivilo(int id) =>
        db.Zivila.Include(z => z.Skupina).Include(z => z.Alergeni).FirstOrDefaultAsync(z => z.Id == id);

    // podatke iz vnosa prepiše v entiteto; vrne napako, če vnos ni veljaven, sicer null
    private async Task<ActionResult?> Prepisi(ZiviloVnos vnos, Zivilo zivilo)
    {
        var ime = vnos.Ime.Trim();

        // enako ime že obstaja (MySQL privzeto ne loči velikih in malih črk, zato "Jabolko" == "jabolko")
        if (await db.Zivila.AnyAsync(z => z.Ime == ime && z.Id != zivilo.Id))
            return Conflict(new ProblemDetails
            {
                Title = "Živilo že obstaja",
                Detail = $"Živilo z imenom \"{ime}\" je že v bazi.",
                Status = StatusCodes.Status409Conflict
            });

        var skupina = await db.SkupineZivil.FindAsync(vnos.SkupinaId);
        if (skupina is null)
            return ValidationProblem(Napaka(nameof(vnos.SkupinaId), "Skupina ne obstaja."));

        var ids = vnos.AlergenIds.Distinct().ToList();
        var alergeni = await db.Alergeni.Where(a => ids.Contains(a.Id)).ToListAsync();
        if (alergeni.Count != ids.Count)
            return ValidationProblem(Napaka(nameof(vnos.AlergenIds), "Neznan alergen."));

        zivilo.Ime = ime;
        zivilo.Skupina = skupina;
        zivilo.EnergijaKcal = vnos.EnergijaKcal;
        zivilo.Beljakovine = vnos.Beljakovine;
        zivilo.OgljikoviHidrati = vnos.OgljikoviHidrati;
        zivilo.Sladkorji = vnos.Sladkorji;
        zivilo.Mascobe = vnos.Mascobe;
        zivilo.NasiceneMascobe = vnos.NasiceneMascobe;
        zivilo.Vlaknine = vnos.Vlaknine;
        zivilo.Sol = vnos.Sol;
        zivilo.Alergeni.Clear();
        zivilo.Alergeni.AddRange(alergeni);
        return null;
    }

    private static ValidationProblemDetails Napaka(string polje, string sporocilo) =>
        new(new Dictionary<string, string[]> { [polje] = [sporocilo] });

    // začasna rešitev, dokler sklop 1 ne doda prijave in vlog:
    // administrator je tisti, ki pozna ključ iz nastavitve Admin:Kljuc (na Azuru skrivnost ADMIN_KLJUC)
    private bool JeAdministrator(string? kljuc)
    {
        var pravi = config["Admin:Kljuc"];
        if (string.IsNullOrEmpty(pravi) || string.IsNullOrEmpty(kljuc)) return false;

        // primerjava v konstantnem času, da se ključa ne da uganiti po hitrosti odgovora
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(kljuc), Encoding.UTF8.GetBytes(pravi));
    }

    private ObjectResult Zavrnjeno() => Problem(
        title: "Dostop zavrnjen",
        detail: "Urejanje baze živil je dovoljeno samo administratorju.",
        statusCode: StatusCodes.Status403Forbidden);
}

public record AlergenOdgovor(int Id, string Ime);
