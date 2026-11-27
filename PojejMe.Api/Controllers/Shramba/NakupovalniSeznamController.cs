using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Data;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Controllers.Shramba;

// nakupovalni seznam gospodinjstva: pregled (osnovna različica za UZ-3.8, UZ-2.9 doda ročni vnos itd.)
[ApiController]
[Authorize]
[Route("api/nakupovalni-seznam")]
public class NakupovalniSeznamController(PojejMeDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PostavkaSeznamaOdgovor>>> Seznam()
    {
        var uporabnikId = int.Parse(User.FindFirstValue("sub")!);
        var uporabnik = await db.Uporabniki.FindAsync(uporabnikId);
        if (uporabnik is null) return Unauthorized();

        var postavke = await db.PostavkeNakupovalnegaSeznama.AsNoTracking()
            .Include(p => p.Zivilo)
            .Where(p => p.GospodinjstvoId == uporabnik.GospodinjstvoId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        return postavke.Select(PostavkaSeznamaOdgovor.Iz).ToList();
    }
}
