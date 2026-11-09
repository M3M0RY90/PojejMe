# Pojej.me

Pametna shramba za zdravo prehrano brez zavržkov – REST API (ASP.NET Core 9, EF Core, MySQL).

- **Produkcija (Azure):** http://pojejme-matic.spaincentral.azurecontainer.io:8080/swagger
- **Kanban plošča:** https://github.com/users/M3M0RY90/projects/2

## Lokalni zagon

Potrebuješ Docker Desktop in .NET 9 SDK.

```bash
docker compose up -d                 # lokalna MySQL baza
dotnet tool restore                  # orodje dotnet-ef
dotnet run --project PojejMe.Api     # API + Swagger
```

API ob zagonu sam izvede migracije. Swagger je na naslovu, ki ga izpiše `dotnet run`, z dodanim `/swagger`.
`/api/status` pove, ali je baza povezana.

## Struktura

```
PojejMe.Api/
  Data/PojejMeDbContext.cs   skupni DbContext, vsak sklop doda svoje DbSet v svoj razdelek
  Data/Migrations/           migracije EF Core
  Models/<Sklop>/            entitete (Uporabniki, Shramba, Recepti, Nacrtovanje)
  Controllers/<Sklop>/       kontrolerji, pot /api/<vir>
```

## Potek dela

1. Na Kanban plošči vzameš kartico, se dodaš kot odgovorni in jo premakneš v *Načrtovanje*.
2. Vejo narediš iz najnovejšega `main`:
   ```bash
   git checkout main && git pull
   git checkout -b uz-2.1-urejanje-zivil
   ```
3. Ko spremeniš entitete, dodaš migracijo:
   ```bash
   dotnet ef migrations add ImeSpremembe --project PojejMe.Api --output-dir Data/Migrations
   ```
4. Push veje in pull request v `main` z `Closes #<številka kartice>`. GitHub Actions preveri, da se slika zgradi.
5. Kolega pregleda PR in ga združi. Ob združitvi se API samodejno namesti na Azure, issue se zapre.

Dve osebi naj hkrati ne dodajata migracij; če pride do konflikta v `PojejMeDbContextModelSnapshot.cs`,
po `git pull` svojo migracijo odstrani (`dotnet ef migrations remove`) in jo dodaj znova.

## CI/CD

Ob pushu v `main` GitHub Actions zgradi Docker sliko, jo objavi na Docker Hubu kot
`memory1966/pojejme-api` (oznaki `latest` in SHA commita) in jo namesti v Azure Container Instances.
Ob pull requestu se slika samo zgradi.
