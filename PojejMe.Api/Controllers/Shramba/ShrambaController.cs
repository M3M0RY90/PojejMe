using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Shramba;
using PojejMe.Api.Models.Uporabniki;
using PojejMe.Api.Services;

namespace PojejMe.Api.Controllers.Shramba;

// UZ-2.2 Dodajanje v shrambo
// shramba pripada gospodinjstvu: vsi člani istega gospodinjstva vidijo iste zaloge
// uporabnik: prijavljen z žetonom (sklop 1, gumb Authorize v Swaggerju) ali, za testne uporabnike
// Ano, Bora in Ceneta, ki se ne morejo prijaviti, po glavi X-Uporabnik-Id
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
            .Include(z => z.Zivilo).ThenInclude(z => z.Skupina).Include(z => z.Dodal)
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId)
            .OrderBy(z => z.Id)
            .ToListAsync();

        return zaloge.Select(z => ZalogaOdgovor.Iz(z)).ToList();
    }

    // UZ-2.5 Pregled po lokacijah: filter po lokaciji in skupini ter iskanje po delu imena
    [HttpGet("pregled")]
    public async Task<ActionResult<PregledShrambe>> Pregled(
        [FromQuery] Lokacija? lokacija, [FromQuery] string? isci, [FromQuery] int? skupinaId,
        [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var shramba = db.ZalogeShrambe.Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId);

        // koliko zapisov je na vsaki lokaciji (tudi 0), ne glede na filtre
        var stetje = await shramba.GroupBy(z => z.Lokacija)
            .Select(g => new { Lokacija = g.Key, Stevilo = g.Count() })
            .ToListAsync();
        var poLokacijah = Enum.GetValues<Lokacija>()
            .ToDictionary(l => l, l => stetje.FirstOrDefault(s => s.Lokacija == l)?.Stevilo ?? 0);

        var poizvedba = shramba;
        if (lokacija is not null)
            poizvedba = poizvedba.Where(z => z.Lokacija == lokacija);
        if (skupinaId is not null)
            poizvedba = poizvedba.Where(z => z.Zivilo.SkupinaId == skupinaId);
        var iskanje = isci?.Trim();
        if (!string.IsNullOrEmpty(iskanje))
            // MySQL privzeto ne loči velikih in malih črk, zato "mle" najde "Mleko"
            poizvedba = poizvedba.Where(z => z.Zivilo.Ime.Contains(iskanje));

        var zaloge = await poizvedba
            .Include(z => z.Zivilo).ThenInclude(z => z.Skupina).Include(z => z.Dodal)
            .OrderBy(z => z.Zivilo.Ime).ThenBy(z => z.Id)
            .ToListAsync();

        return new PregledShrambe(poLokacijah, zaloge.Count, Sporocilo(zaloge.Count, lokacija, iskanje, skupinaId),
            zaloge.Select(z => ZalogaOdgovor.Iz(z)).ToList());
    }

    // UZ-2.6 Razvrščanje po roku: najprej živila, ki jim rok poteče najprej, živila brez roka na koncu
    [HttpGet("po-roku")]
    public async Task<ActionResult<List<ZalogaOdgovor>>> PoRoku(
        [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var zaloge = await db.ZalogeShrambe
            .Include(z => z.Zivilo).ThenInclude(z => z.Skupina).Include(z => z.Dodal)
            .Where(z => z.GospodinjstvoId == uporabnik.GospodinjstvoId)
            // false (ima rok) se razvrsti pred true (nima roka), zato so živila brez roka na koncu
            .OrderBy(z => z.RokUporabnosti == null)
            .ThenBy(z => z.RokUporabnosti)
            .ThenBy(z => z.Zivilo.Ime)
            .ToListAsync();

        return zaloge.Select(z => ZalogaOdgovor.Iz(z)).ToList();
    }

    // sporočilo, ko ni zadetkov: ločimo prazno lokacijo od iskanja brez zadetkov
    private static string? Sporocilo(int stevilo, Lokacija? lokacija, string? isci, int? skupinaId)
    {
        if (stevilo > 0) return null;

        var filtrirano = !string.IsNullOrEmpty(isci) || skupinaId is not null;
        if (filtrirano)
            return string.IsNullOrEmpty(isci)
                ? "Ni zadetkov za izbrane filtre."
                : $"Ni zadetkov za »{isci}«.";

        return lokacija is null ? "Shramba je prazna." : $"Lokacija {lokacija} je prazna.";
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ZalogaOdgovor>> Podrobno(
        int id, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        // zaloge drugega gospodinjstva se ne vidi, zato zanjo vrnemo 404
        var zaloga = await db.ZalogeShrambe
            .Include(z => z.Zivilo).ThenInclude(z => z.Skupina).Include(z => z.Dodal)
            .FirstOrDefaultAsync(z => z.Id == id && z.GospodinjstvoId == uporabnik.GospodinjstvoId);

        return zaloga is null ? NotFound() : ZalogaOdgovor.Iz(zaloga);
    }

    [HttpPost]
    public async Task<ActionResult<ZalogaOdgovor>> Dodaj(
        ZalogaVnos vnos, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var zivilo = await db.Zivila.Include(z => z.Skupina).FirstOrDefaultAsync(z => z.Id == vnos.ZiviloId);
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
        var danes = Svezost.Danes();
        var opozorilo = vnos.RokUporabnosti < danes ? "Rok uporabnosti je že potekel." : null;

        return CreatedAtAction(nameof(Podrobno), new { id = zaloga.Id }, ZalogaOdgovor.Iz(zaloga, opozorilo));
    }

    // UZ-2.7 Poraba in zavržki
    [HttpPost("{id:int}/poraba")]
    public Task<ActionResult<PorabaOdgovor>> Porabi(
        int id, PorabaVnos vnos, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
        => Odstej(id, vnos.Kolicina, VrstaDogodka.Porabljeno, null, uporabnikId);

    [HttpPost("{id:int}/zavrzek")]
    public Task<ActionResult<PorabaOdgovor>> Zavrzi(
        int id, ZavrzekVnos vnos, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
        => Odstej(id, vnos.Kolicina, VrstaDogodka.Zavrzeno, vnos.Razlog?.Trim(), uporabnikId);

    // zgodovina porabe in zavržkov gospodinjstva, najnovejši najprej
    [HttpGet("dogodki")]
    public async Task<ActionResult<List<DogodekOdgovor>>> Dogodki(
        [FromQuery] VrstaDogodka? vrsta, [FromHeader(Name = "X-Uporabnik-Id")] int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var poizvedba = db.DogodkiZaloge.Where(d => d.GospodinjstvoId == uporabnik.GospodinjstvoId);
        if (vrsta is not null) poizvedba = poizvedba.Where(d => d.Vrsta == vrsta);

        var dogodki = await poizvedba
            .Include(d => d.Zivilo).Include(d => d.Uporabnik)
            .OrderByDescending(d => d.Cas).ThenByDescending(d => d.Id)
            .ToListAsync();

        return dogodki.Select(DogodekOdgovor.Iz).ToList();
    }

    // skupna logika za porabo in zavržek: zmanjša ali odstrani zalogo in zapiše dogodek
    private async Task<ActionResult<PorabaOdgovor>> Odstej(
        int id, decimal? kolicina, VrstaDogodka vrsta, string? razlog, int? uporabnikId)
    {
        var uporabnik = await NaloziUporabnika(uporabnikId);
        if (uporabnik is null) return NeznanUporabnik();

        var zaloga = await db.ZalogeShrambe
            .Include(z => z.Zivilo).ThenInclude(z => z.Skupina).Include(z => z.Dodal)
            .FirstOrDefaultAsync(z => z.Id == id && z.GospodinjstvoId == uporabnik.GospodinjstvoId);
        if (zaloga is null) return NotFound();

        // brez količine: vse, kar je v shrambi
        var kolikor = kolicina ?? zaloga.Kolicina;

        // več, kot je na voljo: zavrnemo in ničesar ne spremenimo (pogoj 2)
        if (kolikor > zaloga.Kolicina)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(PorabaVnos.Kolicina)] =
                    [$"Na voljo je samo {zaloga.Kolicina:0.##} {zaloga.Enota}, ne moreš odšteti {kolikor:0.##} {zaloga.Enota}."]
            }));

        var dogodek = new DogodekZaloge
        {
            GospodinjstvoId = zaloga.GospodinjstvoId,
            Zivilo = zaloga.Zivilo,
            Kolicina = kolikor,
            Enota = zaloga.Enota,
            Vrsta = vrsta,
            Razlog = string.IsNullOrEmpty(razlog) ? null : razlog,
            Uporabnik = uporabnik,
            Cas = DateTime.UtcNow
        };
        db.DogodkiZaloge.Add(dogodek);

        // vse porabljeno ali zavrženo: zapis izgine iz shrambe (pogoja 3 in 4), sicer ostane preostanek (pogoj 1)
        var vse = kolikor == zaloga.Kolicina;
        if (vse) db.ZalogeShrambe.Remove(zaloga);
        else zaloga.Kolicina -= kolikor;

        // dogodek in sprememba zaloge se shranita skupaj (ena transakcija)
        await db.SaveChangesAsync();

        var beseda = vrsta == VrstaDogodka.Porabljeno ? "Porabljeno" : "Zavrženo";
        var sporocilo = vse
            ? $"{beseda}: {zaloga.Zivilo.Ime}. Živila ni več v shrambi."
            : $"{beseda} {kolikor:0.##} {zaloga.Enota}, ostane {zaloga.Kolicina:0.##} {zaloga.Enota}.";

        return new PorabaOdgovor(DogodekOdgovor.Iz(dogodek), vse ? null : ZalogaOdgovor.Iz(zaloga), sporocilo);
    }

    // najprej prijavljeni uporabnik iz žetona, sicer testni uporabnik iz glave X-Uporabnik-Id
    private Task<Uporabnik?> NaloziUporabnika(int? glavaId)
    {
        var id = int.TryParse(User.FindFirstValue("sub"), out var izZetona) ? izZetona : glavaId;
        return id is null ? Task.FromResult<Uporabnik?>(null) : db.Uporabniki.FirstOrDefaultAsync(u => u.Id == id);
    }

    private ObjectResult NeznanUporabnik() => Problem(
        title: "Neznan uporabnik",
        detail: "Prijavi se (Authorize) ali v glavi X-Uporabnik-Id pošlji Id testnega uporabnika (1, 2 ali 3).",
        statusCode: StatusCodes.Status401Unauthorized);
}
