# PharmacyProjectNew

Web aplikacija za apoteku sastavljena od ASP.NET Core 8 backenda i Angular 21 frontenda. Backend koristi Clean Architecture, CQRS, JWT autentifikaciju i FluentValidation. Frontend koristi Angular Reactive Forms i validatore.

Ovaj README opisuje kloniranje, lokalno podešavanje baze i pokretanje aplikacije na Windowsu.

## Sadržaj

- [Struktura projekta](#struktura-projekta)
- [Preduslovi](#preduslovi)
- [Kloniranje](#kloniranje)
- [Baza podataka i backup](#baza-podataka-i-backup)
- [Lokalna konfiguracija i tajne](#lokalna-konfiguracija-i-tajne)
- [Pokretanje backenda](#pokretanje-backenda)
- [Pokretanje frontenda](#pokretanje-frontenda)
- [Demo nalozi i početni podaci](#demo-nalozi-i-početni-podaci)
- [Validacija](#validacija)
- [Dijagrami](#dijagrami)
- [Blob slike i Stripe testno plaćanje](#blob-slike-i-stripe-testno-plaćanje)
- [Problemi pri pokretanju](#problemi-pri-pokretanju)
- [Funkcionalnosti](#funkcionalnosti)

## Struktura projekta

```text
PharmacyProjectNew/
├── Market.Backend/       # ASP.NET Core API, Application, Domain, Infrastructure, tests
├── Market.Frontend/      # Angular aplikacija
├── db-backups/           # SQL Server .bak backup baze
├── dokumenti/            # Mermaid dijagrami sistema i poslovnih flowova
├── MIGRATION_MAP.md
└── README.md
```

## Preduslovi

- Git
- .NET 8 SDK
- SQL Server 2019 ili noviji, lokalno instaliran ili pokrenut u Dockeru
- Node.js `20.19+`, `22.12+` ili `24+` i npm (kompatibilno s Angularom 21)
- PowerShell

## Kloniranje

```powershell
git clone https://github.com/mnejra03/PharmacyProjectNew.git
cd PharmacyProjectNew
```

Glavna grana repozitorija je `main`.

## Baza podataka i backup

U repozitoriju se nalazi SQL Server backup [db-backups/MarketaV10.bak](db-backups/MarketaV10.bak). Backup je napravljen s `COPY_ONLY` i kompresijom, a provjeren je SQL Server komandom `RESTORE VERIFYONLY`.

Backup možeš vratiti u SQL Server Management Studio (SSMS):

1. Pokreni SQL Server i poveži se na svoju instancu.
2. Desni klik na **Databases** → **Restore Database...**.
3. Kao izvor odaberi **Device**, klikni `...`, zatim **Add** i izaberi `db-backups/MarketaV10.bak`.
4. Kao odredišnu bazu postavi `MarketaV10` i pokreni restore.
5. U lokalnoj konfiguraciji podesi connection string prema toj instanci.

Ako želiš praznu razvojnu bazu, možeš preskočiti restore. Backend u `Development` okruženju automatski primjenjuje EF Core migracije i dodaje demo podatke pri pokretanju.

## Lokalna konfiguracija i tajne

Prije pokretanja API-ja napravi lokalni konfiguracijski fajl:

`Market.Backend/Market.API/appsettings.Local.json`

Fajl je ignorisan u Gitu. Unesi connection string koji odgovara tvojoj SQL Server instanci. Primjer za SQL Server Express uz Windows autentifikaciju:

```json
{
  "ConnectionStrings": {
    "Main": "Server=localhost\\SQLEXPRESS;Database=MarketaV10;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
  },
  "Jwt": {
    "Key": "lokalni-razvojni-kljuc-duzi-od-32-znaka"
  }
}
```

Za podrazumijevanu instancu SQL Servera koristi `Server=localhost;`. Ako koristiš SQL autentifikaciju, postavi `User ID` i `Password` umjesto `Trusted_Connection=True`. Za Docker SQL Server možeš koristiti, na primjer, `Server=localhost,1433;User ID=sa;Password=<jaka-lozinka>;TrustServerCertificate=True;Encrypt=False`.

Nemoj commitati `appsettings.Local.json`, lozinke, JWT ključeve, Stripe tajne ili Azure Storage ključeve. Za CI i deployment postavi ih kroz varijable okruženja ili siguran secret store.

## Pokretanje backenda

Iz glavnog direktorija repozitorija, u prvom PowerShell prozoru:

```powershell
dotnet restore .\Market.Backend\Market.Backend.sln
dotnet dev-certs https --trust
dotnet run --project .\Market.Backend\Market.API\Market.API.csproj
```

API i Swagger:

- API: `https://localhost:7260`
- Swagger: `https://localhost:7260/swagger`
- HTTP API: `http://localhost:5177`

SQL Server mora biti pokrenut i dostupan. Pri prvom pokretanju na praznoj bazi backend u `Development` okruženju primjenjuje migracije i dodaje početne demo podatke.

## Pokretanje frontenda

Otvori drugi PowerShell prozor iz glavnog direktorija repozitorija:

```powershell
cd .\Market.Frontend\rs1-frontend-2025-26
npm ci
npm start
```

Otvori `http://localhost:4200`. Frontend razvojna konfiguracija poziva API na `https://localhost:7260`, zato backend treba biti pokrenut prije korištenja aplikacije.

Za produkcijski build:

```powershell
npm run build
```

## Demo nalozi i početni podaci

Na svježoj razvojnoj bazi seju se demo korisnici, katalog, oglasi i primjeri narudžbi:

| Uloga | Email | Lozinka |
|---|---|---|
| Administrator | `admin@pharmacy.local` | `Admin123!` |
| Farmaceut | `pharmacist@pharmacy.local` | `Pharmacist123!` |
| Korisnik | `customer@pharmacy.local` | `Customer123!` |

Seeder dopunjava nedostajuće demo podatke; postojeće podatke u bazi ne briše. Demo nalozi i lozinke služe samo za lokalni razvoj i ne smiju se koristiti u produkciji. Na svježoj razvojnoj bazi dodaje se i primjer zahtjeva za dopunu zaliha na čekanju, vidljiv farmaceutu i administratoru.

## Validacija

- Backend komande i upiti validiraju se FluentValidation klasama kroz MediatR validation pipeline.
- Angular forme koriste Reactive Forms i `Validators` za provjeru unosa u pregledniku.
- Validacija na frontendu ne zamjenjuje backend validaciju; API ponovo provjerava podatke prije obrade.

## Dijagrami

Mermaid izvorne datoteke nalaze se u folderu `dokumenti/`:

- [Arhitektura sistema](dokumenti/system-architecture.mmd)
- [Flow kupovine i plaćanja](dokumenti/purchase-flow.mmd)
- [Flow autentifikacije i osvježavanja tokena](dokumenti/authentication-flow.mmd)

`.mmd` dijagrame možeš pregledati u editoru koji podržava Mermaid ili kopirati njihov sadržaj u Mermaid Live Editor.

## Blob slike i Stripe testno plaćanje

Azure Blob Storage i Stripe nisu potrebni za osnovno pokretanje aplikacije.

- Bez `ConnectionStrings:AzureBlobStorage`, uploadi se čuvaju u lokalnom storageu backenda. Za korištenje Azure Blob Storagea dodaj storage connection string u `appsettings.Local.json`.
- Za kartično plaćanje podesi i `Stripe:SecretKey` i `Stripe:PublishableKey` Stripe testnog okruženja. Koristi testne kartice i nikada ne stavljaj ključeve u Git.

Primjer dodatnih lokalnih postavki (spoji ih u postojeći JSON objekat):

```json
{
  "ConnectionStrings": {
    "AzureBlobStorage": "<Azure Storage connection string>"
  },
  "Stripe": {
    "SecretKey": "<Stripe test secret key>",
    "PublishableKey": "<Stripe test publishable key>"
  }
}
```

U stvarnom fajlu spoji vrijednosti u postojeće `ConnectionStrings` i `Stripe` sekcije tako da se svaki JSON ključ pojavljuje samo jednom.

## Problemi pri pokretanju

- **API ne može otvoriti bazu:** provjeri je li SQL Server pokrenut i da li `ConnectionStrings:Main` odgovara instanci, autentifikaciji i portu.
- **Login vraća 401:** provjeri da API koristi `Development`, da su migracije i seeding završili bez greške i da koristiš demo nalog iz tabele.
- **Swagger ne učitava definiciju:** pogledaj grešku API-ja u terminalu i provjeri `https://localhost:7260/swagger/v1/swagger.json`.
- **Frontend ne može pozvati API:** pokreni backend, koristi `https://localhost:7260` i prihvati lokalni HTTPS certifikat. Provjeri `src/environments/environment.ts` ako si promijenila port.
- **Nema slika s Bloba:** provjeri Azure Storage connection string i postoje li objekti na URL-ovima koje koristi baza.
- **Plaćanje ne radi:** provjeri da su oba Stripe test ključa podešena i koristi testnu karticu.

## Funkcionalnosti

Aplikacija obuhvata katalog s kategorijama i filterima, korpu sa spremanjem artikala za kasnije, checkout i Stripe testno plaćanje, favorite, ocjene i recenzije, recepte, obavijesti, chat, korisničke profile i administratorsko upravljanje korisnicima, proizvodima, narudžbama i oglasima. Podržani su upload slika i zahtjevi farmaceuta za nabavku koje administrator može odobriti ili odbiti.

Mapa prenesenih funkcionalnosti, poznatih ograničenja i odluka o zadržavanju kataloga nalazi se u [MIGRATION_MAP.md](MIGRATION_MAP.md).

## Reset lozinke i email

Forma **Zaboravljena lozinka** ne otkriva da li korisnički email postoji. Reset token je jednokratan i vrijedi 30 minuta; nakon uspješnog resetovanja opozivaju se aktivne refresh sesije.

Za slanje emaila podesi SMTP u `Market.Backend/Market.API/appsettings.Local.json` ili kroz sigurne varijable okruženja:

```json
{
  "Frontend": { "BaseUrl": "http://localhost:4200" },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "From": "noreply@example.com",
    "Username": "<SMTP username>",
    "Password": "<SMTP password>"
  }
}
```

U `Development` okruženju, ako SMTP nije podešen, API ispisuje reset link u terminalu. U produkciji SMTP mora biti konfigurisan za isporuku emaila. Nemoj commitati SMTP lozinku ni druge tajne.
