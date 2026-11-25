using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Services;

namespace PojejMe.Api.Controllers.Recepti;

// UZ-3.1 Upravljanje receptov: dodajanje, urejanje, slika
[ApiController]
[Authorize]
[Route("api/recepti")]
public class ReceptiController(PojejMeDbContext db, IWebHostEnvironment okolje) : ControllerBase
{
    private static readonly string[] DovoljeneKoncnice = [".jpg", ".jpeg", ".png", ".webp"];
    private const long NajvecjaSlika = 5 * 1024 * 1024; // 5 MB

    private int TrenutniUporabnikId() => int.Parse(User.FindFirstValue("sub")!);

    private IQueryable<Recept> ReceptiZVsem() => db.Recepti
        .Include(r => r.Sestavine).ThenInclude(s => s.Zivilo)
        .Include(r => r.Koraki);

    // GET api/recepti -> moji recepti
    [HttpGet]
    public async Task<ActionResult<List<ReceptOdgovor>>> Seznam()
    {
        var uporabnikId = TrenutniUporabnikId();
        var recepti = await ReceptiZVsem()
            .Where(r => r.AvtorId == uporabnikId)
            .OrderBy(r => r.Ime)
            .ToListAsync();
        return recepti.Select(ReceptOdgovor.Iz).ToList();
    }

    // GET api/recepti/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceptOdgovor>> Podrobno(int id)
    {
        var uporabnikId = TrenutniUporabnikId();
        var recept = await ReceptiZVsem().FirstOrDefaultAsync(r => r.Id == id && r.AvtorId == uporabnikId);
        if (recept is null) return NotFound();
        return ReceptOdgovor.Iz(recept);
    }

    // POST api/recepti -> nov recept
    [HttpPost]
    public async Task<ActionResult<ReceptOdgovor>> Dodaj(ReceptVnos vnos)
    {
        var napaka = await PreveriVnos(vnos);
        if (napaka is not null) return napaka;

        var recept = new Recept { AvtorId = TrenutniUporabnikId(), UstvarjenOb = DateTime.UtcNow };
        Prepisi(recept, vnos);

        db.Recepti.Add(recept);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Podrobno), new { id = recept.Id }, await NaloziZaOdgovor(recept.Id));
    }

    // PUT api/recepti/5 -> uredi obstoječ recept (sestavine in koraki se zamenjajo z novimi)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReceptOdgovor>> Uredi(int id, ReceptVnos vnos)
    {
        var uporabnikId = TrenutniUporabnikId();
        var recept = await ReceptiZVsem().FirstOrDefaultAsync(r => r.Id == id && r.AvtorId == uporabnikId);
        if (recept is null) return NotFound();

        var napaka = await PreveriVnos(vnos);
        if (napaka is not null) return napaka;

        db.SestavineReceptov.RemoveRange(recept.Sestavine);
        db.KorakiPriprave.RemoveRange(recept.Koraki);
        Prepisi(recept, vnos);
        recept.PosodobljenOb = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await NaloziZaOdgovor(recept.Id);
    }

    // POST api/recepti/5/slika (multipart/form-data, polje "slika")
    [HttpPost("{id:int}/slika")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ReceptOdgovor>> NaloziSliko(int id, IFormFile slika)
    {
        var uporabnikId = TrenutniUporabnikId();
        var recept = await db.Recepti.FirstOrDefaultAsync(r => r.Id == id && r.AvtorId == uporabnikId);
        if (recept is null) return NotFound();

        var koncnica = Path.GetExtension(slika.FileName).ToLowerInvariant();
        if (!DovoljeneKoncnice.Contains(koncnica))
            ModelState.AddModelError(nameof(slika), "Dovoljene so samo slike .jpg, .jpeg, .png in .webp.");
        if (slika.Length == 0 || slika.Length > NajvecjaSlika)
            ModelState.AddModelError(nameof(slika), "Slika mora biti večja od 0 in manjša od 5 MB.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var mapa = Path.Combine(okolje.WebRootPath ?? Path.Combine(okolje.ContentRootPath, "wwwroot"), "slike-receptov");
        Directory.CreateDirectory(mapa);

        // stara slika se pobriše
        if (recept.SlikaPot is not null)
        {
            var stara = Path.Combine(mapa, Path.GetFileName(recept.SlikaPot));
            if (System.IO.File.Exists(stara)) System.IO.File.Delete(stara);
        }

        var ime = $"{recept.Id}-{Guid.NewGuid():N}{koncnica}";
        await using (var tok = System.IO.File.Create(Path.Combine(mapa, ime)))
            await slika.CopyToAsync(tok);

        recept.SlikaPot = $"/slike-receptov/{ime}";
        recept.PosodobljenOb = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await NaloziZaOdgovor(recept.Id);
    }

    // GET api/recepti/5/hranilna-vrednost -> UZ-3.2 kalorije in makrohranila, skupaj in na porcijo
    [HttpGet("{id:int}/hranilna-vrednost")]
    public async Task<ActionResult<HranilnaVrednostOdgovor>> HranilnaVrednostRecepta(int id)
    {
        var uporabnikId = TrenutniUporabnikId();
        var recept = await ReceptiZVsem().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.AvtorId == uporabnikId);
        if (recept is null) return NotFound();
        return HranilnaVrednost.Izracunaj(recept);
    }

    // GET api/recepti/iz-shrambe -> UZ-3.4 predlogi receptov glede na sestavine v shrambi gospodinjstva
    // ?vsi=true vrne tudi recepte brez sestavin iz shrambe (0 %)
    [HttpGet("iz-shrambe")]
    public async Task<ActionResult<List<ReceptIzShrambeOdgovor>>> IzShrambe([FromQuery] bool vsi = false)
    {
        var uporabnikId = TrenutniUporabnikId();
        var uporabnik = await db.Uporabniki.FindAsync(uporabnikId);
        if (uporabnik is null) return Unauthorized();

        // živila s preteklim rokom uporabnosti ne štejemo kot razpoložljiva
        var danes = DateOnly.FromDateTime(DateTime.UtcNow);
        var zaloge = await db.ZalogeShrambe.AsNoTracking()
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId
                        && (z.RokUporabnosti == null || z.RokUporabnosti >= danes))
            .ToListAsync();

        var recepti = await ReceptiZVsem().AsNoTracking()
            .Where(r => r.AvtorId == uporabnikId)
            .ToListAsync();

        return ReceptiIzShrambe.Predlagaj(recepti, zaloge, vsi);
    }

    // ---- pomožne metode ----

    private async Task<ActionResult?> PreveriVnos(ReceptVnos vnos)
    {
        if (vnos.Koraki.Any(string.IsNullOrWhiteSpace))
            ModelState.AddModelError(nameof(vnos.Koraki), "Korak priprave ne sme biti prazen.");

        var ziviloIdji = vnos.Sestavine.Select(s => s.ZiviloId).Distinct().ToList();
        var obstojecih = await db.Zivila.CountAsync(z => ziviloIdji.Contains(z.Id));
        if (obstojecih != ziviloIdji.Count)
            ModelState.AddModelError(nameof(vnos.Sestavine), "Vsaj eno živilo ne obstaja v bazi živil.");

        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    private static void Prepisi(Recept recept, ReceptVnos vnos)
    {
        recept.Ime = vnos.Ime.Trim();
        recept.Opis = string.IsNullOrWhiteSpace(vnos.Opis) ? null : vnos.Opis.Trim();
        recept.SteviloPorcij = vnos.SteviloPorcij;
        recept.Sestavine = vnos.Sestavine
            .Select(s => new SestavinaRecepta { ZiviloId = s.ZiviloId, Kolicina = s.Kolicina, Enota = s.Enota!.Value })
            .ToList();
        recept.Koraki = vnos.Koraki
            .Select((opis, i) => new KorakPriprave { Zaporedje = i + 1, Opis = opis.Trim() })
            .ToList();
    }

    private async Task<ReceptOdgovor> NaloziZaOdgovor(int id)
    {
        db.ChangeTracker.Clear();
        var recept = await ReceptiZVsem().AsNoTracking().FirstAsync(r => r.Id == id);
        return ReceptOdgovor.Iz(recept);
    }
}
