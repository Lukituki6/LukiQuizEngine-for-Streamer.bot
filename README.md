> [!NOTE]
> This project is not affiliated with, endorsed by, or officially supported by Streamer.bot, Twitch, YouTube, or Kick. Streamer.bot, Twitch, YouTube, and Kick are trademarks or names of their respective owners. This project only uses features available in Streamer.bot and is provided as an independent community tool. 

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# LukiQuizEngine for Streamer.bot

**Aktualne wydanie: v2.0.2 BETA**

- [READMElegacy.md](1.3.0/READMElegacy.md) - archiwalna dokumentacja starszej wersji **LukiQuizEngine v1.3.0**, dostępna w folderze [1.3.0](1.3.0/). Zawiera oryginalną instrukcję instalacji, konfiguracji i obsługi. Informacje oraz przykłady kodu dotyczą wersji 1.3.0 i nie powinny być stosowane bezpośrednio w aktualnej wersji 2.0.2 BETA.

Lokalny silnik quizowy dla Streamer.bot z panelem WWW, overlayem OBS, głosowaniem na czacie Twitch, historią rund i trwałym rankingiem. Wystarczy jedna Action C# i jeden trigger wiadomości Twitch. Nie potrzebujesz Node.js, Pythona, osobnego programu `.exe`, bazy danych ani zewnętrznego hostingu.

> [!IMPORTANT]
> Wersja **2.0.2 BETA** jest przeznaczona do testowania w lokalnym środowisku. Przed użyciem na transmisji zrób kopię danych i przetestuj instalację. Znane ograniczenie bezpieczeństwa HTTP `/DoAction` jest opisane w sekcji [Bezpieczeństwo i znane ograniczenia](#bezpieczeństwo-i-znane-ograniczenia).

## Spis treści

- [Co potrafi](#co-potrafi)
- [Pliki i wymagania](#pliki-i-wymagania)
- [Instalacja od zera](#instalacja-od-zera)
- [Pierwszy test](#pierwszy-test)
- [Tryby panelu](#tryby-panelu)
- [Głosowanie i komendy Twitch](#głosowanie-i-komendy-twitch)
- [Rankingi, archiwa i przywracanie](#rankingi-archiwa-i-przywracanie)
- [Gdzie są przechowywane dane](#gdzie-są-przechowywane-dane)
- [Bezpieczeństwo i znane ograniczenia](#bezpieczeństwo-i-znane-ograniczenia)
- [Aktualizacja starszej instalacji](#aktualizacja-starszej-instalacji)
- [Rozwiązywanie problemów](#rozwiązywanie-problemów)
- [Historia projektu i licencja](#historia-projektu-i-licencja)

## Co potrafi

### Quiz, pytania i głosowanie

- Pytania z **2-12 odpowiedziami**, w tym wieloma poprawnymi, z punktami przypisanymi do odpowiedzi.
- Tryby punktowania `adjacent`, `manual`, `none` i `custom`, punkty częściowe oraz opcjonalne premie za szybkość.
- Reguły głosowania `LAST`, `FIRST` i `LIMITED`, z możliwością przeglądania i korygowania głosów w panelu.
- Widoczność wyników przed ujawnieniem: `LIVE`, `COUNT_ONLY` i `HIDDEN`.
- Tryb ankiety `POLL` bez przyznawania punktów do rankingu.
- Losowanie kolejności odpowiedzi, zapis pytań, szablony i zestawy oraz import/eksport JSON.

### Rundy i streaming

- Ręczne sterowanie głosowaniem, timer, pauza, wznowienie, zmiany czasu i opcjonalne `AutoReveal`.
- Historia rund, cofanie i przeliczanie kwalifikujących się wyników w bieżącej epoce punktacji.
- Tryb drużynowy z przypisaniami widzów oraz osobnymi wynikami drużyn.
- Overlay OBS: układy `FULL`, `COMPACT`, `MINIMAL` i `NO_LEADERBOARD`, personalizacja wyglądu, opcjonalne efekty i podium.
- Skróty klawiaturowe i diagnostyka instalacji.

### Ranking i archiwizacja

- Trwała punktacja widzów i statystyki odpowiedzi.
- Import/eksport rankingu CSV, pełny backup stanu oraz automatyczne snapshoty.
- **Automatyczne archiwum rankingu przed resetem**, jeśli ranking zawiera dane do zachowania.
- **Ręczny zapis rankingu bez resetowania punktów**, z opcjonalną nazwą archiwum.
- **Przeglądanie, wyszukiwanie i podgląd historycznych rankingów** z poziomu panelu.
- **Przywracanie wybranego rankingu** po potwierdzeniu `PRZYWROC` i utworzeniu kopii bezpieczeństwa aktualnych wyników.
- Przycisk **Otwórz folder archiwów** otwierający lokalny katalog w Eksploratorze Windows.

## Pliki i wymagania

| Plik | Zastosowanie |
| --- | --- |
| `QuizEngine.cs` | Silnik C# wklejany do Action w Streamer.bot |
| `panel.html` | Lokalny panel WWW |
| `overlay.html` | Źródło przeglądarkowe do OBS |
| `OTWORZ_PANEL.url` | Skrót do panelu po ustawieniu serwera HTTP |
| `PODGLAD_OVERLAYU.url` | Skrót do demonstracyjnego overlayu |
| `LICENSE` | Licencja MIT |

Wymagany jest Windows ze Streamer.bot obsługującym `Execute C# Code`, `HTTP Server` i `WebSocket Server`, aktualna przeglądarka oraz połączone konto Twitch, jeśli chcesz zbierać głosy z czatu. OBS jest potrzebny do wyświetlania overlayu na transmisji, nie do samego działania panelu.

Kod był analizowany pod kątem zgodności ze środowiskiem Streamer.bot **1.0.7**, ale nie deklaruje pełnej kompatybilności ze wszystkimi wersjami hosta.

## Instalacja od zera

### 1. Pobierz odpowiednią paczkę

Pobierz paczkę **LukiQuizEngine** z odpowiedniego wydania w sekcji Releases, gdy zostanie opublikowana. Rozpakuj ją do stałego folderu, np.:

```text
C:\StreamerBot\LukiQuizEngine
```

Nie mieszaj plików `panel.html`, `overlay.html` i `QuizEngine.cs` z różnych buildów. Wszystkie trzy muszą odpowiadać temu samemu wydaniu.

### 2. Dodaj jedną Action w Streamer.bot

1. W zakładce **Actions** utwórz Action o nazwie dokładnie `QUIZ - Silnik`.
2. Dodaj sub-action **Core > C# > Execute C# Code**.
3. Wklej całą zawartość `QuizEngine.cs`.
4. Użyj **Find Refs** i **Save and Compile**. Sprawdź, czy kod się kompiluje.
5. Do tej samej Action przypisz **jeden** trigger `Twitch > Chat > Message`.
6. Upewnij się, że Action jest włączona.

Nie twórz oddzielnych triggerów dla `!1`, `!2` i kolejnych głosów.

### 3. Skonfiguruj WebSocket Server

W **Servers/Clients > WebSocket Server** ustaw:

```text
Address / Host: 127.0.0.1
Port:           8080
Endpoint:       /
Authentication: ON
Enforce:        ON
Password:       własne, silne i unikalne hasło
```

Zapisz ustawienia i uruchom serwer. Jeśli był uruchomiony podczas zmian, uruchom go ponownie.

### 4. Skonfiguruj HTTP Server

W **Servers/Clients > HTTP Server** ustaw:

```text
Host: 127.0.0.1
Port: 7474
```

Dodaj mapowanie:

```text
Path:   quiz
Folder: C:\StreamerBot\LukiQuizEngine
```

Folder musi wskazywać rzeczywistą lokalizację plików. Uruchom HTTP Server. **Nie zmieniaj hosta na `0.0.0.0` i nie udostępniaj serwera przez LAN, tunel ani internet.** Wyjaśnienie znajduje się w sekcji bezpieczeństwa.

### 5. Połącz panel

Otwórz:

```text
http://127.0.0.1:7474/quiz/panel.html
```

Możesz też skorzystać z `OTWORZ_PANEL.url`. Wpisz hasło WebSocket, kliknij **Połącz bezpiecznie** i sprawdź zakładkę **Diagnostyka**. Niektórych właściwości konfiguracji sieciowej diagnostyka nie może jednoznacznie potwierdzić, więc jej wynik nie zastępuje sprawdzenia ustawień Streamer.bot.

### 6. Dodaj overlay do OBS

Po prawidłowym połączeniu panelu użyj **Skopiuj URL overlayu**. W OBS dodaj źródło **Browser / Przeglądarka**, wklej cały skopiowany URL i ustaw rozmiar `1920 × 1080`.

Nie korzystaj z adresu demonstracyjnego `?demo=1` podczas prawdziwej transmisji. Pełny URL zawiera haslo WebSocket we fragmencie po `#`

Podgląd demonstracyjny:

```text
http://127.0.0.1:7474/quiz/overlay.html?demo=1
```

## Pierwszy test

1. Włącz testową rundę z dwoma odpowiedziami.
2. Wyślij `!1` z konta widza na podłączonym czacie Twitch.
3. Sprawdź, czy głos widoczny jest w panelu i na overlayu.
4. Zamknij głosowanie, ujawnij poprawną odpowiedź i sprawdź naliczone punkty.
5. Sprawdź komendy `!punkty` i `!ranking`.
6. Przeprowadź oddzielną rundę z timerem oraz `AutoReveal`.
7. Sprawdź ręczny zapis rankingu, listę archiwów i eksport CSV.

## Tryby panelu

Panel obsługuje dwa tryby interfejsu:

- **PROSTY** - najważniejsze funkcje quizu, pytań, zestawów, rankingu, historii i pomocy.
- **ADVANCED** - pozostałe ustawienia i zaawansowane opcje obsługi.

Oba tryby korzystają z tego samego silnika i tych samych danych. Funkcje archiwizacji rankingów są dostępne w zakładce **Ranking** w obu trybach. Zmiana trybu nie resetuje punktów.

## Głosowanie i komendy Twitch

| Komenda | Działanie |
| --- | --- |
| `!1` ... `!12` | Głosowanie na odpowiedź o podanym numerze |
| `!punkty` | Sprawdzenie własnego wyniku |
| `!ranking` | Wyświetlenie rankingu na czacie |
| `!quiz ...` | Polecenia moderatorskie obsługiwane przez silnik |

Komendy administracyjne czatu podlegają uprawnieniom nadawcy i moderatorów. Automatyczne komunikaty o wydarzeniach rundy są domyślnie wyłączone (`AnnounceRoundEventsInChat = false` w `QuizEngine.cs`).

## Rankingi, archiwa i przywracanie

### Bieżący ranking i CSV

W zakładce **Ranking** możesz przeglądać punkty, korygować wyniki, eksportować i importować CSV. Import i reset zmieniają aktualną punktację, dlatego sprawdzaj podgląd i komunikat potwierdzenia. Pełny backup stanu to **inny rodzaj kopii** niż eksport CSV.

### Automatyczne archiwum przy resecie

Przed resetowaniem niepustego rankingu silnik zapisuje lokalne archiwum JSON. Jeśli wymagany zapis archiwum się nie powiedzie, reset powinien zostać odrzucony. Nie usuwaj ręcznie plików kontrolnych w trakcie operacji i nie zakładaj, że brak odpowiedzi panelu oznacza brak wykonania resetu.

### Ręczny zapis i przeglądanie

Przycisk **Zapisz ranking** tworzy nazwane archiwum bez resetowania wyników. Sekcja archiwum w panelu pozwala wyszukiwać wpisy, zobaczyć datę i typ zapisu, a następnie obejrzeć historyczne wyniki bez zmiany bieżącego rankingu.

### Przywrócenie rankingu

1. Wybierz archiwum i sprawdź jego podgląd.
2. Zakończ albo anuluj aktywną rundę.
3. Kliknij **Przywróć ranking**, sprawdź ostrzeżenie i wpisz `PRZYWROC`.
4. Silnik sprawdza zgodność danych i tworzy **archiwum bezpieczeństwa dotychczasowego rankingu** przed zastosowaniem przywracania.
5. Po operacji sprawdź bieżący ranking i statystyki.

Przywrócenie rankingu **nie przywraca całej konfiguracji quizu**, szablonów, overlayu ani kompletnej aktywnej sesji. Przy niezgodnych ustawieniach drużyn przywrócenie może zostać zablokowane. Archiwa, snapshoty i pełne backupy mają różne zakresy danych.

Jeśli panel zgłasza **niepotwierdzony wynik** resetu lub przywrócenia, nie klikaj ponownie w ciemno. Najpierw sprawdź stan i użyj dostępnej w panelu opcji sprawdzenia wyniku przywracania, a w razie potrzeby zajrzyj do logów Streamer.bot.

### Integralność i limity

Nowo tworzone archiwa posiadają pole `ContentHash` służące do wykrywania uszkodzeń treści. Dla zgodności mogą być odczytywane również starsze archiwa bez tego pola. Zwykły SHA-256 **nie jest podpisem cyfrowym** i nie zabezpiecza przed osobą mogącą celowo modyfikować pliki.

## Gdzie są przechowywane dane

| Dane | Lokalizacja / mechanizm |
| --- | --- |
| Bieżący stan i ranking V2 | Trwała zmienna Streamer.bot `localQuiz_data_v2` |
| Stan publiczny dla overlayu | Streamer.bot `localQuiz_public_v2` |
| Automatyczne snapshoty | Streamer.bot `localQuiz_snapshots_v2` |
| Archiwa rankingu JSON (automatyczne, ręczne, bezpieczeństwa) | `%LOCALAPPDATA%\LukiQuizEngine\RankingArchives` |
| Ustawienia interfejsu i szkic przeglądarki | Lokalna pamięć przeglądarki (`localStorage`) |
| Pełny backup | Eksportowany na żądanie z panelu plik |
| Hasło WebSocket | Pamięć uruchomionego klienta, a dla OBS również fragment pełnego URL overlayu |

Zmienne z oznaczeniem `v1` są danymi starszego silnika lub migracji, **nie podstawowym miejscem zapisu rankingu v2.0.2**. Nie kasuj ich bez wykonania odpowiednich kopii i upewnienia się, że migracja zakończyła się poprawnie.

Folder AppData jest przypisany do użytkownika Windows. Jeśli przenosisz quiz na inny komputer lub konto, skopiuj potrzebne archiwa osobno. Aktualizacja plików HTML i C# nie przenosi archiwów automatycznie.

## Bezpieczeństwo i znane ograniczenia

> [!WARNING]
> **Znane ograniczenie bezpieczeństwa: HTTP `/DoAction` w Streamer.bot.** Bieżąca wersja LukiQuizEngine nie ma niezależnego uwierzytelniania wszystkich administracyjnych wywołań Action przez HTTP.

### Obowiązkowa konfiguracja lokalna

- **HTTP Server:** `127.0.0.1:7474`, bez publicznego tunelu, reverse proxy ani przekierowania portów.
- **WebSocket Server:** `127.0.0.1:8080/`, `Authentication = ON`, `Enforce = ON` oraz silne, indywidualne hasło.
- Nie ustawiaj nasłuchu HTTP ani WebSocket na `0.0.0.0`.
- Nie udostępniaj pełnego adresu źródła OBS ani plików z prywatnymi danymi widzów.

### Ważna informacja o bezpieczeństwie

Wbudowany HTTP Server Streamer.bot udostępnia `POST /DoAction`, umożliwiający wywoływanie akcji i przekazywanie argumentów. **WebSocket Authentication i Enforce nie chronią automatycznie tego osobnego endpointu HTTP.** Klient mający dostęp do HTTP API może potencjalnie uruchamiać administracyjne operacje LukiQuizEngine **oraz inne Action w Streamer.bot**.

Nasłuch tylko na `127.0.0.1` znacząco ogranicza dostęp z sieci, ale nie jest niezależnym uwierzytelnianiem. Nie chroni przed niezaufanymi lokalnymi procesami, a niektóre scenariusze żądań z obcych stron internetowych do usług lokalnych zależą od przeglądarki i konfiguracji serwera. **Nie deklaruje pełnej ochrony przed takimi żądaniami.**

Z tego powodu **nie udostępniaj wbudowanego HTTP Server Streamer.bot niezaufanym urządzeniom lub użytkownikom**. Nie instaluj quizu z założeniem, że HTTP API jest bezpiecznym publicznym panelem administracyjnym.

Oficjalna dokumentacja: [HTTP DoAction](https://docs.streamer.bot/api/http/requests/do-action), [HTTP Server](https://docs.streamer.bot/api/http/guide/configuration), [WebSocket Server](https://docs.streamer.bot/api/websocket/guide/configuration).

### Hasło w URL overlayu

Przycisk **Skopiuj URL overlayu** umieszcza poświadczenie WebSocket w części URL po `#`. Fragment nie jest normalnie wysyłany w żądaniu HTTP, ale cały adres zostaje zapisany w konfiguracji źródła OBS. Traktuj pełny URL jak hasło administracyjne. Jeśli go ujawnisz, zmień hasło WebSocket i wygeneruj nowy adres overlayu.

### Ważne ograniczenia bezpieczeństwa i trwałości

- Diagnostyka panelu nie dowodzi samodzielnie, że serwery są dostępne wyłącznie lokalnie. Sprawdź ustawienie adresów w Streamer.bot.
- Pełna atomowość pomiędzy plikami JSON archiwów a trwałym magazynem Streamer.bot nie została niezależnie potwierdzona przy nagłym wyłączeniu zasilania.
- Lokalne dane rankingu i archiwa mogą zawierać nicki i identyfikatory użytkowników Twitch. Nie publikuj ich przypadkowo.
- Mechanizmy kopii bezpieczeństwa zmniejszają ryzyko utraty danych, ale nie zastępują osobnego backupu komputera.

## Aktualizacja starszej instalacji

**Z v2.0.0 lub v2.0.1 BETA na v2.0.2 BETA:**

1. Zakończ lub anuluj aktywną rundę.
2. Wyeksportuj aktualny ranking i pełny backup stanu, a także zachowaj folder starej instalacji.
3. Podmień razem `QuizEngine.cs` w Action oraz `panel.html` i `overlay.html` w folderze mapowania HTTP.
4. Wykonaj **Find Refs** i **Save and Compile**.
5. Upewnij się, że panel i silnik pokazują `2.0.2`, `PHoeniX` i `QUPA`. Odśwież cache przeglądarki i źródła OBS w razie potrzeby.
6. Nie usuwaj trwałych zmiennych V2 ani starszych archiwów.
7. Sprawdź ranking, działanie rundy i funkcje zapisu archiwum na danych testowych.

**Z v1.3.0:** przed podmianą plików przeczytaj [MIGRATION.md](MIGRATION.md). Historia migracji oraz starsze ograniczenia są opisane w tym osobnym poradniku. Nie wykonuj migracji podczas aktywnej rundy.

## Rozwiązywanie problemów

| Objaw | Co sprawdzić |
| --- | --- |
| Strona pokazuje `404 Not Found` | Włączony HTTP Server, mapowanie `quiz` i właściwy folder zawierający `panel.html` |
| Panel się otwiera, ale nie łączy | WebSocket na `127.0.0.1:8080`, poprawne hasło, Authentication i Enforce, restart serwera po zmianach |
| Panel jest połączony, ale Action nie odpowiada | Dokładna nazwa `QUIZ - Silnik`, aktualny kod C#, **Find Refs** oraz **Save and Compile** |
| Głosy `!1` nie działają | Połączenie Twitch, trigger wiadomości, otwarta runda, prawidłowy numer odpowiedzi |
| Overlay pokazuje demo lub stare dane | Usuń `demo=1`, użyj URL skopiowanego po autoryzacji, sprawdź cache i mapowanie HTTP |
| Nie otwiera się folder archiwów | Sprawdź Windows, uprawnienia do AppData, Eksplorator i komunikat w logach Streamer.bot |
| Nie można przywrócić archiwum | Zakończ rundę, sprawdź zgodność drużyn i rewizję rankingu, ponownie otwórz podgląd |
| Wynik zapisu / restore jest niepewny | Nie ponawiaj destrukcyjnej operacji w ciemno, sprawdź bieżący ranking i status operacji |
| Po błędzie naliczania quiz nie może zakończyć rundy | Sprawdź diagnostykę `autoFinalizeError`; użyj świadomego anulowania problematycznej rundy, a następnie zweryfikuj punkty |

## Opcjonalna modyfikacja głosowania z v1.3.0

<details>
<summary><strong>Archiwalny poradnik - dodatkowy tekst po komendzie !1 ... !12</strong></summary>

Ten poradnik dotyczy **kodu v1.3.0** i jest zachowany dla osób korzystających ze starszego wydania. W **v2.0.2** nie należy podmieniać tych fragmentów bez samodzielnego sprawdzenia zgodności z aktualnym silnikiem.

Domyślnie w 1.3.0 głos musiał mieć postać samodzielnej komendy `!1` ... `!12`.

**Wariant 1 - dopisek po spacji.** Akceptuje np. `!1 siema`, ale nie `!1siema`:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?:\s+.*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

**Wariant 2 - dopisek bez wymaganej spacji.** Akceptuje np. `!1siema`, ale może rozpoznać również początek innych komend, np. `!1v1`:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?![0-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

W wersji 1.3.0 zamieniało się **wyłącznie** wartość `VoteRegex` w `QuizEngine.cs`, wybierając tylko jeden wariant, a następnie wykonywało **Save and Compile**. To modyfikacja opcjonalna, nie domyślne zachowanie quizu. W razie konfliktu z innymi komendami Twitch lepszy jest wariant wymagający spacji.

</details>

## Historia projektu i licencja

- [CHANGELOG.md](CHANGELOG.md) - historia zmian w 2.0.2, 2.0.1 i 2.0.0.
- [MIGRATION.md](MIGRATION.md) - instrukcja przejścia ze starszej wersji 1.3.0.
- Starszą zawartość README można znaleźć w [historii zmian pliku na GitHubie](https://github.com/Lukituki6/LukiQuizEngine-for-Streamer.bot/commits/main/README.md). Historycznych instrukcji 1.3.0 nie należy traktować jako aktualnej instrukcji instalacji 2.0.2.

**Wykorzystanie AI:** projekt był rozwijany również z pomocą narzędzi sztucznej inteligencji, m.in. podczas analizy i w pomocy prac nad kodem. Wszystkie istotne zmiany warto testować w docelowym środowisku Streamer.bot.

Projekt jest udostępniany na zasadach licencji [MIT](LICENSE), „tak jak jest”, bez gwarancji działania. Korzystasz z niego na własną odpowiedzialność, zwłaszcza przy zmianach konfiguracji i importowaniu danych.
