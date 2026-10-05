# ProjektWPF – osobisty asystent

ProjektWPF to aplikacja desktopowa (WPF, .NET 9) będąca osobistym asystentem, który pomaga w codziennych czynnościach: od notatek i wydatków, przez filmy i pogodę, po sterowanie Spotify i asystenta głosowego.

## Demo

https://youtu.be/RX7q7QCLsFM

## Funkcje

- Asystent głosowy oparty na OpenAI API (transkrypcja mowy i odpowiedzi czatu)
- Podgląd obciążenia procesora i pamięci RAM
- Panel sterowania odtwarzaczem Spotify
- Tworzenie i edytowanie notatek
- Lista wydatków
- Lista ulubionych filmów z ocenami i danymi pobieranymi z API (OMDb)
- Pogoda na podstawie adresu IP użytkownika
- Minutnik z alarmem
- Rejestracja i logowanie użytkowników (hasła przechowywane jako hash)

## Technologie

- WPF (.NET 9, MVVM) + MahApps.Metro
- Entity Framework Core 9 + SQL Server (LocalDB)
- OpenAI API, Spotify Web API, OMDb API, OpenWeatherMap API
- NAudio, System.Speech
- Testy: projekt `TestyWpf`

## Struktura projektu

```
ProjektWPF/
├── ProjektWPF/        # aplikacja WPF
│   ├── Data/          # AppDbContext, Session, PasswordHasher
│   ├── Models/        # modele bazy danych
│   ├── Migrations/    # migracje EF Core
│   ├── Services/      # integracje (AI, Spotify, pogoda, filmy, alarm...)
│   ├── ViewModels/    # logika widoków (MVVM)
│   └── *.xaml         # widoki
└── TestyWpf/          # testy jednostkowe
```

## Wymagania

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (instalowany razem z Visual Studio)
- Klucze API opisane poniżej

## Konfiguracja

Klucze API aplikacja odczytuje ze zmiennych środowiskowych:

| Zmienna | Do czego służy |
|---|---|
| `OPEN_AI_API_KEY` | asystent głosowy (OpenAI) |
| `SPOTIFY_CLIENT_ID` | integracja ze Spotify |
| `SPOTIFY_CLIENT_SECRET` | integracja ze Spotify |
| `OMDb_API_KEY` | dane o filmach (OMDb) |
| `WEATHER_API` | pogoda (OpenWeatherMap) |

Przykład (PowerShell):

```powershell
setx OPEN_AI_API_KEY "twój_klucz"
setx SPOTIFY_CLIENT_ID "twoje_id"
setx SPOTIFY_CLIENT_SECRET "twój_sekret"
setx OMDb_API_KEY "twój_klucz"
setx WEATHER_API "twój_klucz"
```

Po ustawieniu zmiennych uruchom ponownie terminal / Visual Studio.

**Spotify:** w panelu aplikacji Spotify Developer dodaj Redirect URI: `http://127.0.0.1:8080/callback`.

**Baza danych:** domyślny connection string (`(localdb)\mssqllocaldb`, baza `WpfDB`) znajduje się w `Data/AppDbContext.cs`. Migracje zastosujesz poleceniem:

```bash
dotnet ef database update --project ProjektWPF
```

## Instalacja i uruchomienie

```bash
git clone https://github.com/kubacki03/ProjektWPF.git
cd ProjektWPF
dotnet build
dotnet run --project ProjektWPF
```

## Testy

```bash
dotnet test
```

## Kontakt

Pytania i uwagi: polichronowe@gmail.com
