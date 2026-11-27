using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Recepti;
using PojejMe.Api.Models.Uporabniki;
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
        .Include(r => r.Koraki)
        .Include(r => r.PrehranskeOmejitve);

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
        Prepisi(recept, vnos, await NaloziOmejitve(vnos));

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
        Prepisi(recept, vnos, await NaloziOmejitve(vnos));
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

    // GET api/recepti/resi-hrano?dni=3 -> UZ-3.5 recepti, ki porabijo živila s kratkim rokom uporabnosti
    [HttpGet("resi-hrano")]
    public async Task<ActionResult<ResiHranoOdgovor>> ResiHranoPredlogi([FromQuery] int dni = 3)
    {
        if (dni < 1 || dni > 30)
        {
            ModelState.AddModelError(nameof(dni), "Število dni mora biti med 1 in 30.");
            return ValidationProblem(ModelState);
        }

        var uporabnikId = TrenutniUporabnikId();
        var uporabnik = await db.Uporabniki.FindAsync(uporabnikId);
        if (uporabnik is null) return Unauthorized();

        var danes = DateOnly.FromDateTime(DateTime.UtcNow);
        var zaloge = await db.ZalogeShrambe.AsNoTracking()
            .Include(z => z.Zivilo)
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId && z.RokUporabnosti != null)
            .ToListAsync();

        var recepti = await ReceptiZVsem().AsNoTracking()
            .Where(r => r.AvtorId == uporabnikId)
            .ToListAsync();

        return ResiHrano.Predlagaj(recepti, zaloge, danes, dni);
    }

    // GET api/recepti/iskanje?ime=pala&maxCas=30&minKcal=100&maxKcal=500&omejitve=1&omejitve=2 -> UZ-3.6
    // vsi filtri so neobvezni in se kombinirajo (IN); kalorije se primerjajo na porcijo
    [HttpGet("iskanje")]
    public async Task<ActionResult<List<ReceptOdgovor>>> Iskanje(
        [FromQuery] string? ime,
        [FromQuery] int? maxCas,
        [FromQuery] decimal? minKcal,
        [FromQuery] decimal? maxKcal,
        [FromQuery] List<int>? omejitve)
    {
        if (maxCas is < 1)
            ModelState.AddModelError(nameof(maxCas), "Največji čas priprave mora biti vsaj 1 minuta.");
        if (minKcal is < 0m || maxKcal is < 0m)
            ModelState.AddModelError(nameof(maxKcal), "Kalorije ne smejo biti negativne.");
        if (minKcal is not null && maxKcal is not null && minKcal > maxKcal)
            ModelState.AddModelError(nameof(minKcal), "Najmanjše število kalorij ne sme biti večje od največjega.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var uporabnikId = TrenutniUporabnikId();
        var poizvedba = ReceptiZVsem().AsNoTracking().Where(r => r.AvtorId == uporabnikId);

        // iskanje po imenu: del imena, ne glede na velike in male črke
        if (!string.IsNullOrWhiteSpace(ime))
        {
            var iskano = ime.Trim();
            poizvedba = poizvedba.Where(r => r.Ime.Contains(iskano));
        }

        // recepti brez vpisanega časa priprave pri tem filtru ne ustrezajo
        if (maxCas is not null)
            poizvedba = poizvedba.Where(r => r.CasPripraveMin != null && r.CasPripraveMin <= maxCas);

        // recept mora ustrezati vsem izbranim omejitvam
        foreach (var omejitevId in (omejitve ?? []).Distinct())
            poizvedba = poizvedba.Where(r => r.PrehranskeOmejitve.Any(o => o.Id == omejitevId));

        var recepti = await poizvedba.OrderBy(r => r.Ime).ToListAsync();

        // kalorije se izračunajo sproti (UZ-3.3), zato ta filter izvedemo po nalaganju
        var rezultat = recepti.Select(ReceptOdgovor.Iz);
        if (minKcal is not null)
            rezultat = rezultat.Where(r => r.HranilneVrednosti.NaPorcijo.EnergijaKcal >= minKcal);
        if (maxKcal is not null)
            rezultat = rezultat.Where(r => r.HranilneVrednosti.NaPorcijo.EnergijaKcal <= maxKcal);

        return rezultat.ToList();
    }

    // POST api/recepti/5/skuhal-sem -> UZ-3.7 odšteje porabljene sestavine iz shrambe gospodinjstva
    // telo (neobvezno): { "steviloPorcij": 2, "kljubTemu": false, "kljuc": "..." }
    [HttpPost("{id:int}/skuhal-sem")]
    public async Task<ActionResult<PripravaOdgovor>> SkuhalSem(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SkuhalSemVnos? vnos)
    {
        vnos ??= new SkuhalSemVnos();

        var uporabnikId = TrenutniUporabnikId();
        var uporabnik = await db.Uporabniki.FindAsync(uporabnikId);
        if (uporabnik is null) return Unauthorized();

        var recept = await ReceptiZVsem().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.AvtorId == uporabnikId);
        if (recept is null) return NotFound();

        // ista priprava (isti ključ) se odšteje samo enkrat
        var kljuc = string.IsNullOrWhiteSpace(vnos.Kljuc) ? null : vnos.Kljuc.Trim();
        if (kljuc is not null)
        {
            var obstojeca = await db.PripraveReceptov.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ReceptId == id && p.Kljuc == kljuc);
            if (obstojeca is not null)
                return Ok(new PripravaOdgovor(obstojeca.Id, recept.Id, recept.Ime, obstojeca.SteviloPorcij,
                    obstojeca.SkuhanoOb, true, [], []));
        }

        // živil s preteklim rokom ne porabimo (enako kot pri UZ-3.4)
        var danes = DateOnly.FromDateTime(DateTime.UtcNow);
        var zaloge = await db.ZalogeShrambe
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId
                        && (z.RokUporabnosti == null || z.RokUporabnosti >= danes))
            .ToListAsync();

        var porcije = vnos.SteviloPorcij ?? recept.SteviloPorcij;
        var nacrt = SkuhaniRecept.Nacrtuj(recept, zaloge, porcije);

        if (nacrt.Manjka.Count > 0 && !vnos.KljubTemu)
            return Conflict(new SkuhalSemZavrnitev(
                "V shrambi ni dovolj vseh sestavin. Za odštevanje tega, kar je na voljo, pošlji \"kljubTemu\": true.",
                nacrt.Manjka));

        // uveljavimo načrt: zmanjšamo zaloge, prazne odstranimo
        foreach (var o in nacrt.Odstetja)
            o.Zaloga.Kolicina = Math.Round(o.Zaloga.Kolicina - o.Odstej, 2);
        foreach (var prazna in nacrt.Odstetja.Select(o => o.Zaloga).Distinct().Where(z => z.Kolicina <= 0))
            db.ZalogeShrambe.Remove(prazna);

        var priprava = new PripravaRecepta
        {
            ReceptId = recept.Id,
            SteviloPorcij = porcije,
            SkuhanoOb = DateTime.UtcNow,
            Kljuc = kljuc
        };
        db.PripraveReceptov.Add(priprava);
        await db.SaveChangesAsync(); // vse spremembe shrambe in zapis priprave se shranijo skupaj

        return CreatedAtAction(nameof(Podrobno), new { id = recept.Id },
            new PripravaOdgovor(priprava.Id, recept.Id, recept.Ime, porcije, priprava.SkuhanoOb, false,
                nacrt.Poraba, nacrt.Manjka));
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

        // UZ-3.6: vse izbrane prehranske omejitve morajo obstajati v šifrantu
        var omejitveIdji = vnos.PrehranskeOmejitveIds.Distinct().ToList();
        var obstojecihOmejitev = await db.PrehranskeOmejitve.CountAsync(o => omejitveIdji.Contains(o.Id));
        if (obstojecihOmejitev != omejitveIdji.Count)
            ModelState.AddModelError(nameof(vnos.PrehranskeOmejitveIds), "Vsaj ena prehranska omejitev ne obstaja.");

        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    private Task<List<PrehranskaOmejitev>> NaloziOmejitve(ReceptVnos vnos)
    {
        var idji = vnos.PrehranskeOmejitveIds.Distinct().ToList();
        return db.PrehranskeOmejitve.Where(o => idji.Contains(o.Id)).ToListAsync();
    }

    private static void Prepisi(Recept recept, ReceptVnos vnos, List<PrehranskaOmejitev> omejitve)
    {
        recept.Ime = vnos.Ime.Trim();
        recept.Opis = string.IsNullOrWhiteSpace(vnos.Opis) ? null : vnos.Opis.Trim();
        recept.SteviloPorcij = vnos.SteviloPorcij;
        recept.CasPripraveMin = vnos.CasPripraveMin;
        recept.PrehranskeOmejitve.Clear();
        recept.PrehranskeOmejitve.AddRange(omejitve);
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
