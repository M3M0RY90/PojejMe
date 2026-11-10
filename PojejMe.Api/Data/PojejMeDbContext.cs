using Microsoft.EntityFrameworkCore;
using PojejMe.Api.Models.Shramba;

namespace PojejMe.Api.Data;

// skupni dostop do baze za vse sklope
// vsak sklop doda svoje tabele kot DbSet v ločen razdelek spodaj
public class PojejMeDbContext(DbContextOptions<PojejMeDbContext> options) : DbContext(options)
{
    // --- Sklop 1: uporabniki, gospodinjstvo in statistika ---

    // --- Sklop 2: shramba in nakupovalni seznam ---
    public DbSet<Zivilo> Zivila => Set<Zivilo>();
    public DbSet<SkupinaZivil> SkupineZivil => Set<SkupinaZivil>();
    public DbSet<Alergen> Alergeni => Set<Alergen>();

    // --- Sklop 3: recepti ---

    // --- Sklop 4: načrtovanje, skupno življenje in stroški ---

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Sklop 2 ---
        modelBuilder.Entity<Zivilo>(e =>
        {
            e.Property(z => z.Ime).HasMaxLength(100);
            // baza sama prepreči dve živili z enakim imenom (pogoj 3 pri UZ-2.1)
            e.HasIndex(z => z.Ime).IsUnique();

            // skupine ni mogoče izbrisati, dokler jo uporablja kakšno živilo
            e.HasOne(z => z.Skupina).WithMany().HasForeignKey(z => z.SkupinaId)
                .OnDelete(DeleteBehavior.Restrict);

            // vmesna tabela ZiviloAlergen za povezavo več-na-več
            e.HasMany(z => z.Alergeni).WithMany(a => a.Zivila)
                .UsingEntity(j => j.ToTable("ZiviloAlergen"));

            foreach (var lastnost in new[] { nameof(Zivilo.EnergijaKcal), nameof(Zivilo.Beljakovine),
                         nameof(Zivilo.OgljikoviHidrati), nameof(Zivilo.Sladkorji), nameof(Zivilo.Mascobe),
                         nameof(Zivilo.NasiceneMascobe), nameof(Zivilo.Vlaknine), nameof(Zivilo.Sol) })
            {
                e.Property(lastnost).HasPrecision(6, 2);
            }
        });

        modelBuilder.Entity<SkupinaZivil>(e =>
        {
            e.Property(s => s.Ime).HasMaxLength(50);
            e.HasData(
                new SkupinaZivil { Id = 1, Ime = "Sadje" },
                new SkupinaZivil { Id = 2, Ime = "Zelenjava" },
                new SkupinaZivil { Id = 3, Ime = "Žita in žitni izdelki" },
                new SkupinaZivil { Id = 4, Ime = "Stročnice" },
                new SkupinaZivil { Id = 5, Ime = "Mleko in mlečni izdelki" },
                new SkupinaZivil { Id = 6, Ime = "Meso in mesni izdelki" },
                new SkupinaZivil { Id = 7, Ime = "Ribe in morski sadeži" },
                new SkupinaZivil { Id = 8, Ime = "Jajca" },
                new SkupinaZivil { Id = 9, Ime = "Oreščki in semena" },
                new SkupinaZivil { Id = 10, Ime = "Olja in maščobe" },
                new SkupinaZivil { Id = 11, Ime = "Sladkor in sladkarije" },
                new SkupinaZivil { Id = 12, Ime = "Pijače" },
                new SkupinaZivil { Id = 13, Ime = "Začimbe in omake" },
                new SkupinaZivil { Id = 14, Ime = "Drugo" });
        });

        modelBuilder.Entity<Alergen>(e =>
        {
            e.Property(a => a.Ime).HasMaxLength(50);
            e.HasData(
                new Alergen { Id = 1, Ime = "Gluten" },
                new Alergen { Id = 2, Ime = "Raki" },
                new Alergen { Id = 3, Ime = "Jajca" },
                new Alergen { Id = 4, Ime = "Ribe" },
                new Alergen { Id = 5, Ime = "Arašidi" },
                new Alergen { Id = 6, Ime = "Soja" },
                new Alergen { Id = 7, Ime = "Mleko" },
                new Alergen { Id = 8, Ime = "Oreški" },
                new Alergen { Id = 9, Ime = "Listna zelena" },
                new Alergen { Id = 10, Ime = "Gorčično seme" },
                new Alergen { Id = 11, Ime = "Sezamovo seme" },
                new Alergen { Id = 12, Ime = "Žveplov dioksid in sulfiti" },
                new Alergen { Id = 13, Ime = "Volčji bob" },
                new Alergen { Id = 14, Ime = "Mehkužci" });
        });
    }
}
