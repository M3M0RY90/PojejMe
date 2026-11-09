using Microsoft.EntityFrameworkCore;

namespace PojejMe.Api.Data;

// skupni dostop do baze za vse sklope
// vsak sklop doda svoje tabele kot DbSet v ločen razdelek spodaj
public class PojejMeDbContext(DbContextOptions<PojejMeDbContext> options) : DbContext(options)
{
    // --- Sklop 1: uporabniki, gospodinjstvo in statistika ---

    // --- Sklop 2: shramba in nakupovalni seznam ---

    // --- Sklop 3: recepti ---

    // --- Sklop 4: načrtovanje, skupno življenje in stroški ---
}
