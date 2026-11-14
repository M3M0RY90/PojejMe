using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Uporabniki;
using PojejMe.Api.Services;

namespace PojejMe.Api.Controllers.Uporabniki;

// UZ-1.1 Registracija in prijava: registracija, prijava, odjava, ponastavitev gesla
[ApiController]
[Route("api/auth")]
public class AuthController(
    PojejMeDbContext db, ZetonStoritev zetoni, IConfiguration config, ILogger<AuthController> log) : ControllerBase
{
    // PasswordHasher sam doda sol in uporabi PBKDF2, gesla v bazi nikoli ne hranimo v čistopisu
    private static readonly PasswordHasher<Uporabnik> Hasher = new();

    // ---------- registracija in prijava ----------

    [HttpPost("registracija")]
    public async Task<ActionResult<UporabnikOdgovor>> Registracija(RegistracijaVnos vnos)
    {
        var email = Normaliziraj(vnos.Email);
        var ime = vnos.Ime.Trim();

        if (await db.Uporabniki.AnyAsync(u => u.Email == email))
            return EmailZe();

        // vsak uporabnik mora imeti gospodinjstvo (shramba se veže nanj), zato ob registraciji
        // ustvarimo osebno gospodinjstvo; v UZ-1.3 se lahko uporabnik pridruži drugemu
        var uporabnik = new Uporabnik
        {
            Ime = ime,
            Email = email,
            Gospodinjstvo = new Gospodinjstvo { Ime = $"Gospodinjstvo {ime}" }
        };
        uporabnik.GesloHash = Hasher.HashPassword(uporabnik, vnos.Geslo);

        db.Uporabniki.Add(uporabnik);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // dva hkratna zahtevka z isto e-pošto: enolični indeks v bazi prepreči drugega
            return EmailZe();
        }

        return Created("/api/auth/jaz", UporabnikOdgovor.Iz(uporabnik));
    }

    [HttpPost("prijava")]
    public async Task<ActionResult<PrijavaOdgovor>> Prijava(PrijavaVnos vnos)
    {
        var email = Normaliziraj(vnos.Email);
        var uporabnik = await db.Uporabniki.FirstOrDefaultAsync(u => u.Email == email);

        // enako sporočilo za neznano e-pošto in napačno geslo, da napadalec ne izve, kateri računi obstajajo
        if (uporabnik?.GesloHash is null ||
            Hasher.VerifyHashedPassword(uporabnik, uporabnik.GesloHash, vnos.Geslo) == PasswordVerificationResult.Failed)
        {
            return Problem(
                title: "Napačna e-pošta ali geslo",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (zeton, veljavenDo) = zetoni.Ustvari(uporabnik);
        return new PrijavaOdgovor(zeton, veljavenDo);
    }

    [Authorize]
    [HttpPost("odjava")]
    public async Task<IActionResult> Odjava()
    {
        // povečamo verzijo: vsi žetoni, izdani do zdaj, takoj prenehajo veljati
        var uporabnik = await db.Uporabniki.FindAsync(TrenutniId);
        if (uporabnik is null) return Unauthorized();

        uporabnik.ZetonVerzija++;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // zaščiten klic, ki dokazuje, da žeton deluje (in da po odjavi ne deluje več)
    [Authorize]
    [HttpGet("jaz")]
    public async Task<ActionResult<UporabnikOdgovor>> Jaz()
    {
        var uporabnik = await db.Uporabniki.FindAsync(TrenutniId);
        return uporabnik is null ? Unauthorized() : UporabnikOdgovor.Iz(uporabnik);
    }

    // ---------- ponastavitev gesla ----------

    [HttpPost("pozabljeno-geslo")]
    public async Task<ActionResult<PozabljenoGesloOdgovor>> PozabljenoGeslo(PozabljenoGesloVnos vnos)
    {
        var email = Normaliziraj(vnos.Email);
        var uporabnik = await db.Uporabniki.FirstOrDefaultAsync(u => u.Email == email);

        string? razvojniZeton = null;
        if (uporabnik is not null)
        {
            // naključen žeton; v bazo gre samo njegova zgoščena vrednost, veljaven je 1 uro
            var zeton = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            uporabnik.PonastavitevHash = ZgostiZeton(zeton);
            uporabnik.PonastavitevPoteceOb = DateTime.UtcNow.AddHours(1);
            await db.SaveChangesAsync();

            // pošiljanje e-pošte pride z UZ-1.9; do takrat žeton samo izpišemo v dnevnik
            // in ga lokalno (Ponastavitev:ZetonVOdgovoru) vrnemo v odgovoru
            log.LogInformation("Žeton za ponastavitev gesla za {Email}: {Zeton}", email, zeton);
            if (config.GetValue<bool>("Ponastavitev:ZetonVOdgovoru")) razvojniZeton = zeton;
        }

        return new PozabljenoGesloOdgovor(
            "Če račun s to e-pošto obstaja, smo nanj poslali povezavo za ponastavitev gesla.", razvojniZeton);
    }

    [HttpPost("ponastavi-geslo")]
    public async Task<IActionResult> PonastaviGeslo(PonastavitevVnos vnos)
    {
        var zgostitev = ZgostiZeton(vnos.Zeton.Trim());
        var uporabnik = await db.Uporabniki.FirstOrDefaultAsync(u =>
            u.PonastavitevHash == zgostitev && u.PonastavitevPoteceOb > DateTime.UtcNow);

        // neznan, potekel in že uporabljen žeton dajo isto napako
        if (uporabnik is null)
            return Problem(
                title: "Povezava ni veljavna",
                detail: "Povezava za ponastavitev je potekla ali je bila že uporabljena.",
                statusCode: StatusCodes.Status400BadRequest);

        uporabnik.GesloHash = Hasher.HashPassword(uporabnik, vnos.NovoGeslo);
        uporabnik.PonastavitevHash = null;       // žeton je enkraten
        uporabnik.PonastavitevPoteceOb = null;
        uporabnik.ZetonVerzija++;                // stare prijave prenehajo veljati
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- pomožne metode ----------

    // žeton prijavljenega uporabnika vsebuje njegov Id v zahtevku "sub"
    private int TrenutniId => int.Parse(User.FindFirstValue("sub")!);

    // e-pošto vedno primerjamo v malih črkah
    private static string Normaliziraj(string email) => email.Trim().ToLowerInvariant();

    private static string ZgostiZeton(string zeton) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(zeton)));

    private ObjectResult EmailZe() => Problem(
        title: "E-pošta je že registrirana",
        detail: "Uporabnik s to e-pošto že obstaja. Prijavi se ali ponastavi geslo.",
        statusCode: StatusCodes.Status409Conflict);
}
