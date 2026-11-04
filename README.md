# Pharmacy

Web aplikacija za apoteku. Backend je ASP.NET Core 8 organizovan kroz Clean Architecture i CQRS, sa JWT autentifikacijom i FluentValidation validacijom. Frontend je Angular 21 aplikacija sa modulima, lazy loadingom i Reactive Forms.

Uputstvo ispod opisuje pokretanje projekta nakon kloniranja repozitorija na Windowsu.

## Sadržaj

- [Preduslovi](#preduslovi)
- [Kloniranje](#kloniranje)
- [Podešavanje baze i lokalnih tajni](#podešavanje-baze-i-lokalnih-tajni)
- [Pokretanje backenda](#pokretanje-backenda)
- [Pokretanje frontenda](#pokretanje-frontenda)
- [Demo nalozi i početni podaci](#demo-nalozi-i-početni-podaci)
- [Blob slike i Stripe testno plaćanje](#blob-slike-i-stripe-testno-plaćanje)
- [Problemi pri pokretanju](#problemi-pri-pokretanju)

## Preduslovi

- Git
- .NET 8 SDK
- SQL Server 2019 ili noviji, lokalno instaliran ili pokrenut u Dockeru
- Node.js `20.19+`, `22.12+` ili `24+` i npm (kompatibilno s Angularom 21)
- PowerShell

## Kloniranje

```powershell
git clone --branch pharmacy-migration-rs1-2025-26 https://github.com/mnejra03/pharmacy-project.git
cd pharmacy-project
```

Ako si već klonirala repozitorij, prebaci se na granu projekta i preuzmi posljednje izmjene:

```powershell
git switch pharmacy-migration-rs1-2025-26
git pull
```

## Podešavanje baze i lokalnih tajni

Backend koristi SQL Server i automatski primjenjuje EF Core migracije pri pokretanju u Development okruženju. Prije pokretanja API-ja pokreni SQL Server i napravi lokalni konfiguracijski fajl:

`Market.Backend/Market.API/appsettings.Local.json`

Fajl je ignorisan u Gitu. Unesi konekciju koja odgovara tvojoj SQL Server instanci. Primjer za SQL Server Express uz Windows autentifikaciju:

```json
{
  "ConnectionStrings": {
    "Main": "Server=localhost\\SQLEXPRESS;Database=PharmacyDevelopment;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
  },
  "Jwt": {
    "Key": "lokalni-razvojni-kljuc-duzi-od-32-znaka"
  }
}
```

Za podrazumijevanu instancu SQL Servera koristi `Server=localhost;`. Ako koristiš SQL autentifikaciju, postavi `User ID` i `Password` umjesto `Trusted_Connection=True`. Za Docker SQL Server možeš koristiti, na primjer, `Server=localhost,1433;User ID=sa;Password=<jaka-lozinka>;TrustServerCertificate=True;Encrypt=False`.

Nemoj commitati `appsettings.Local.json`, lozinke, JWT ključeve, Stripe tajne ili Azure Storage ključeve. Za CI i deployment postavi ih kroz varijable okruženja ili siguran secret store.

## Pokretanje backenda

U prvom PowerShell prozoru, iz glavnog direktorija repozitorija:

```powershell
dotnet restore .\Market.Backend\Market.Backend.sln
dotnet dev-certs https --trust
dotnet run --project .\Market.Backend\Market.API\Market.API.csproj
```

API i Swagger:

- API: `https://localhost:7260`
- Swagger: `https://localhost:7260/swagger`
- HTTP API: `http://localhost:5177`

Pri prvom pokretanju backend primijeni migracije i u Development okruženju doda početne demo podatke. SQL Server mora biti dostupan; baza se kreira kroz migracije.

## Pokretanje frontenda

Otvori drugi PowerShell prozor iz glavnog direktorija repozitorija:

```powershell
cd .\Market.Frontend\rs1-frontend-2025-26
npm ci
npm start
```

Otvori `http://localhost:4200`. Frontend razvojna konfiguracija već poziva API na `https://localhost:7260`; backend zato treba pokrenuti prije korištenja aplikacije.

Korisne komande iz frontend direktorija:

```powershell
npm run build
npm test
```

## Demo nalozi i početni podaci

U svježoj Development bazi seju se demo korisnici, katalog, oglasi i primjeri narudžbi:

| Uloga | Email | Lozinka |
|---|---|---|
| Administrator | `admin@pharmacy.local` | `Admin123!` |
| Farmaceut | `pharmacist@pharmacy.local` | `Pharmacist123!` |
| Korisnik | `customer@pharmacy.local` | `Customer123!` |

Seeder dopunjava nedostajuće demo podatke; postojeće podatke u bazi ne briše. Demo nalozi i lozinke služe samo za lokalni razvoj i ne smiju se koristiti u produkciji.
Na svježoj Development bazi dodaje se i primjer zahtjeva za dopunu zaliha na čekanju, vidljiv farmaceutu i administratoru.

## Blob slike i Stripe testno plaćanje

Azure Blob Storage i Stripe nisu potrebni za osnovno pokretanje aplikacije.

- Bez `ConnectionStrings:AzureBlobStorage`, uploadi se čuvaju u lokalnom storageu backenda. Za korištenje postojećih slika s Bloba dodaj Storage connection string u `appsettings.Local.json`.
- Za kartično plaćanje dodaj Stripe testni secret key pod `Stripe:SecretKey`. Koristi Stripe testne ključeve i testne kartice; ključeve ne stavljaj u Git.

Primjer dodatnih lokalnih postavki (dodaj ih u isti JSON objekat iznad):

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

U stvarnom fajlu spoji ove vrijednosti u postojeće `ConnectionStrings` i `Stripe` sekcije, tako da se svaki JSON ključ pojavljuje samo jednom.

## Problemi pri pokretanju

- **API ne može otvoriti bazu:** provjeri je li SQL Server pokrenut i da li `ConnectionStrings:Main` odgovara njegovom serveru, autentifikaciji i portu.
- **Login vraća 401:** provjeri da API koristi `Development`, da je migracija/seeding završio bez greške i da koristiš jedan od demo naloga iz tabele.
- **Swagger ne učitava definiciju:** pogledaj grešku API-ja u terminalu i provjeri `https://localhost:7260/swagger/v1/swagger.json`.
- **Frontend ne može pozvati API:** pokreni backend, koristi `https://localhost:7260` i prihvati lokalni HTTPS certifikat. Provjeri `src/environments/environment.ts` ako si promijenila port.
- **Nema slika s Bloba:** provjeri Azure Storage connection string i postoje li kontejneri/objekti koji se koriste u URL-ovima podataka.
- **Plaćanje ne radi:** dodaj važeći Stripe testni secret key i koristi testnu karticu iz Stripe dokumentacije.

## Funkcionalnosti

Katalog proizvoda i brendova, kategorije, korpa sa spremanjem za kasnije, narudžbe i checkout, favoriti, ocjene i recenzije, recepti, obavijesti, chat, korisnički profili, admin upravljanje korisnicima/proizvodima/narudžbama/oglasima, direktno zaprimanje zaliha i zahtjevi farmaceuta za nabavku koje administrator odobrava ili odbija, slike i Stripe testno plaćanje.

Mapa prenesenih funkcionalnosti, poznatih ograničenja i odluke o zadržavanju kataloga nalazi se u [MIGRATION_MAP.md](MIGRATION_MAP.md).

## Reset lozinke i email

Forma **Zaboravljena lozinka** ne otkriva da li korisnicki email postoji. Reset token je jednokratan i vrijedi 30 minuta; nakon uspjesnog resetovanja opozivaju se aktivne refresh sesije.

Za slanje emaila podesi SMTP u `Market.Backend/Market.API/appsettings.Local.json` ili kroz sigurne varijable okruzenja:

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

U `Development` okruzenju, ako SMTP nije podesen, API ispisuje reset link u terminalu. U produkciji SMTP mora biti konfigurisan za isporuku emaila. Ne commitaj SMTP lozinku ni druge tajne.
