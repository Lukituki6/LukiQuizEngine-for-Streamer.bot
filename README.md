> [!NOTE]
> Projekt nie jest powiązany ze Streamer.bot, Twitchem, YouTube ani Kick, nie jest przez nie sponsorowany ani oficjalnie wspierany. Streamer.bot, Twitch, YouTube i Kick są znakami towarowymi lub nazwami należącymi do ich odpowiednich właścicieli. Projekt wykorzystuje wyłącznie funkcje dostępne w Streamer.bot i jest udostępniany jako niezależne narzędzie społecznościowe.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# LukiQuizEngine for Streamer.bot

**Aktualne wydanie: v2.2.0**

**🌐 Język / Language:** [🇵🇱 Polski](README.md) | [🇬🇧 English](README_EN.md)

- [READMElegacy.md](1.3.0/READMElegacy.md) - archiwalna dokumentacja starszej wersji **LukiQuizEngine v1.3.0**, dostępna w folderze [1.3.0](1.3.0/). Zawiera oryginalną instrukcję instalacji, konfiguracji i obsługi. Informacje oraz przykłady kodu dotyczą wersji 1.3.0 i nie powinny być stosowane bezpośrednio w aktualnej wersji 2.2.0.

LukiQuizEngine to lokalny silnik quizowy dla Streamer.bot z panelem WWW, overlayem OBS, głosowaniem na czacie Twitch, historią rund i trwałym rankingiem.

Do działania wystarczy **jedna Action C# i jeden trigger wiadomości Twitch**. Nie potrzebujesz Node.js, Pythona, osobnej aplikacji `.exe`, zewnętrznej bazy danych ani hostingu.

> [!IMPORTANT]
> Wersja **2.2.0** jest przeznaczona do testowania w lokalnym środowisku. Przed użyciem podczas transmisji wykonaj kopię istniejących danych i przetestuj instalację. Znane ograniczenie bezpieczeństwa HTTP `/DoAction` zostało opisane w sekcji [Bezpieczeństwo i znane ograniczenia](#bezpieczeństwo-i-znane-ograniczenia).

## Spis treści

- [Co potrafi](#co-potrafi)
- [Pliki i wymagania](#pliki-i-wymagania)
- [Instalacja od zera](#instalacja-od-zera)
- [Pierwszy test](#pierwszy-test)
- [Tryby panelu](#tryby-panelu)
- [Ustawienia języka](#ustawienia-języka)
- [Głosowanie i komendy Twitch](#głosowanie-i-komendy-twitch)
- [Lokalny symulator quizu](#lokalny-symulator-quizu)
- [Rankingi, archiwa i przywracanie](#rankingi-archiwa-i-przywracanie)
- [Gdzie są przechowywane dane](#gdzie-są-przechowywane-dane)
- [Bezpieczeństwo i znane ograniczenia](#bezpieczeństwo-i-znane-ograniczenia)
- [Aktualizacja starszej instalacji](#aktualizacja-starszej-instalacji)
- [Rozwiązywanie problemów](#rozwiązywanie-problemów)
- [Opcjonalna modyfikacja głosowania z v1.3.0](#opcjonalna-modyfikacja-głosowania-z-v130)
- [Historia projektu i licencja](#historia-projektu-i-licencja)

## Co potrafi

### Quiz, pytania i głosowanie

- Pytania z **2-12 odpowiedziami**, w tym wieloma poprawnymi, z możliwością przypisywania osobnych wartości punktowych.
- Tryby punktowania `adjacent`, `manual`, `none` i `custom`, punkty częściowe oraz opcjonalne premie za szybkość odpowiedzi.
- Reguły głosowania `LAST`, `FIRST` i `LIMITED`, z możliwością przeglądania i korygowania głosów w panelu.
- Tryby widoczności wyników przed ujawnieniem poprawnej odpowiedzi: `LIVE`, `COUNT_ONLY` i `HIDDEN`.
- Tryb ankiety `POLL` bez przyznawania punktów do rankingu.
- Losowanie kolejności odpowiedzi, szablony pytań, zestawy pytań oraz import i eksport JSON.
- Trzy konfigurowalne sposoby rozpoznawania głosów: `STRICT`, `WHITESPACE` i `ATTACHED`.

### Rundy i streaming

- Ręczne sterowanie głosowaniem, timer, pauza, wznowienie, zmiany czasu i opcjonalne `AutoReveal`.
- Historia rund z filtrowaniem, wyszukiwaniem, cofaniem i przeliczaniem kwalifikujących się wyników w bieżącej epoce punktacji.
- Tryb drużynowy z przypisywaniem widzów i osobnymi wynikami drużyn.
- Overlay OBS z układami `FULL`, `COMPACT`, `MINIMAL` i `NO_LEADERBOARD`.
- Personalizacja wyglądu overlayu, opcjonalne efekty wizualne, dźwięki i podium.
- Skróty klawiaturowe oraz diagnostyka instalacji.
- Wbudowany lokalny symulator umożliwiający testowanie działania quizu bez modyfikowania prawdziwego rankingu.

### Panel i interfejs

- Trzy tryby interfejsu: `PROSTY`, `ADVANCED` i `LEGACY`.
- Ulepszony wygląd przycisków, wizualne oznaczanie aktywnych elementów i czytelniejsze sterowanie.
- Tryb Legacy inspirowany wyglądem LukiQuizEngine 1.3.0.
- Obsługa języka polskiego i angielskiego w panelu oraz overlayu.
- Opcjonalna synchronizacja języka panelu i overlayu.
- Wspólny silnik quizu i te same dane we wszystkich trybach panelu.

### Ranking i archiwizacja

- Trwała punktacja widzów oraz statystyki odpowiedzi.
- Import i eksport rankingu CSV, pełny backup stanu oraz automatyczne snapshoty.
- **Automatyczne archiwum rankingu przed resetem**, jeżeli ranking zawiera dane do zachowania.
- **Ręczny zapis rankingu bez resetowania punktów**, z możliwością nadania nazwy archiwum.
- **Przeglądanie, wyszukiwanie, filtrowanie i podgląd archiwalnych rankingów** bezpośrednio w panelu.
- **Przywracanie wybranego rankingu** po wpisaniu `PRZYWROC`, z wcześniejszym utworzeniem archiwum bezpieczeństwa aktualnych wyników.
- Ręczne usuwanie wybranych archiwów po zatwierdzeniu operacji.
- Opcjonalne automatyczne czyszczenie archiwów na podstawie ustawionych zasad przechowywania.
- Przycisk **Otwórz folder archiwów**, uruchamiający lokalny katalog w Eksploratorze plików Windows.

## Pliki i wymagania

| Plik | Zastosowanie |
| --- | --- |
| `QuizEngine.cs` | Silnik quizowy C# wklejany do Action w Streamer.bot |
| `panel.html` | Lokalny panel WWW |
| `overlay.html` | Źródło przeglądarkowe do OBS |
| `OTWORZ_PANEL.url` | Opcjonalny skrót do panelu |
| `PODGLAD_OVERLAYU.url` | Opcjonalny skrót do demonstracyjnego overlayu |
| `LICENSE` | Licencja MIT |

Pliki skrótów mogą nie być dołączone do każdej paczki. Panel i overlay można zawsze otworzyć bezpośrednio za pomocą ich adresów HTTP.

### Wymagania

- Windows z kompatybilną wersją Streamer.bot.
- Obsługa `Execute C# Code`, `HTTP Server` i `WebSocket Server`.
- Aktualna przeglądarka internetowa.
- Połączone konto Twitch, jeżeli chcesz zbierać głosy od widzów na czacie.
- OBS Studio, jeżeli chcesz wyświetlać overlay podczas transmisji.

OBS nie jest wymagany do korzystania z samego panelu WWW.

Kod był analizowany pod kątem zgodności ze środowiskiem Streamer.bot **1.0.7**, ale pełna kompatybilność ze wszystkimi wersjami hosta nie jest gwarantowana.

## Instalacja od zera

### 1. Pobierz odpowiednią paczkę

Pobierz odpowiednią paczkę **LukiQuizEngine** z sekcji GitHub Releases, gdy wydanie będzie dostępne.

Rozpakuj ją do stałego folderu, na przykład:

```text
C:\StreamerBot\LukiQuizEngine
```

Nie mieszaj plików `panel.html`, `overlay.html` i `QuizEngine.cs` pochodzących z różnych buildów.

**Wszystkie trzy pliki muszą pochodzić z tego samego wydania.**

### 2. Dodaj jedną Action w Streamer.bot

1. Otwórz zakładkę **Actions** w Streamer.bot.
2. Utwórz nową Action o dokładnej nazwie `QUIZ - Silnik`.
3. Dodaj sub-action **Core > C# > Execute C# Code**.
4. Wklej całą zawartość pliku `QuizEngine.cs`.
5. Kliknij **Find Refs**, a następnie **Save and Compile**.
6. Sprawdź, czy kod kompiluje się poprawnie.
7. Do tej samej Action przypisz **jeden** trigger `Twitch > Chat > Message`.
8. Upewnij się, że Action jest włączona.

Nie twórz osobnych triggerów dla `!1`, `!2` ani pozostałych komend głosowania.

### 3. Skonfiguruj WebSocket Server

W **Servers/Clients > WebSocket Server** ustaw:

```text
Address / Host: 127.0.0.1
Port:           8080
Endpoint:       /
Authentication: ON
Enforce:        ON
Password:       Własne, silne i unikalne hasło
```

Zapisz ustawienia i uruchom serwer.

Jeżeli serwer był już uruchomiony podczas zmiany konfiguracji, uruchom go ponownie.

### 4. Skonfiguruj HTTP Server

W **Servers/Clients > HTTP Server** ustaw:

```text
Host: 127.0.0.1
Port: 7474
```

Dodaj następujące mapowanie folderu:

```text
Path:   quiz
Folder: C:\StreamerBot\LukiQuizEngine
```

Folder musi wskazywać rzeczywistą lokalizację plików LukiQuizEngine.

Uruchom HTTP Server.

**Nie zmieniaj hosta na `0.0.0.0` ani nie udostępniaj serwera przez LAN, tunel lub internet.**

Wyjaśnienie znajduje się w sekcji dotyczącej bezpieczeństwa.

### 5. Połącz panel WWW

Otwórz w przeglądarce:

```text
http://127.0.0.1:7474/quiz/panel.html
```

Możesz również użyć `OTWORZ_PANEL.url`, jeżeli skrót jest dostępny w paczce.

Wpisz hasło WebSocket, kliknij **Połącz bezpiecznie** i otwórz sekcję **Diagnostyka**.

Niektórych właściwości konfiguracji sieciowej wbudowana diagnostyka nie może jednoznacznie zweryfikować.

Pomyślny wynik diagnostyki nie zastępuje samodzielnego sprawdzenia ustawień serwerów w Streamer.bot.

### 6. Dodaj overlay do OBS

Po prawidłowym połączeniu panelu:

1. Kliknij **Skopiuj URL overlayu** w panelu.
2. Otwórz OBS Studio.
3. Dodaj nowe źródło **Browser / Przeglądarka**.
4. Wklej cały skopiowany adres URL.
5. Ustaw rozdzielczość `1920 × 1080`.
6. Zapisz ustawienia źródła przeglądarkowego.

Podczas prawdziwej transmisji nie korzystaj z demonstracyjnego adresu zawierającego `?demo=1`.

Pełny adres overlayu zawiera hasło WebSocket we fragmencie URL znajdującym się po znaku `#`.

Demonstracyjny overlay:

```text
http://127.0.0.1:7474/quiz/overlay.html?demo=1
```

## Pierwszy test

1. Uruchom testową rundę zawierającą dwie odpowiedzi.
2. Wyślij `!1` z konta widza na podłączonym czacie Twitch.
3. Sprawdź, czy głos pojawia się w panelu i na overlayu.
4. Zamknij głosowanie, ujawnij poprawną odpowiedź i sprawdź naliczone punkty.
5. Przetestuj komendy `!punkty` i `!ranking`.
6. Przeprowadź kolejną rundę z timerem i funkcją `AutoReveal`.
7. Sprawdź ręczny zapis rankingu, przeglądanie archiwów i eksport CSV.
8. Przełącz panel pomiędzy trybami PROSTY, ADVANCED i LEGACY.
9. Przetestuj język polski i angielski w panelu oraz overlayu.
10. Opcjonalnie skorzystaj z wbudowanego symulatora, aby przetestować punktowanie i głosowanie bez modyfikowania prawdziwych danych quizu.

## Tryby panelu

LukiQuizEngine 2.1.0 BETA oferuje trzy tryby interfejsu.

### PROSTY

Przeznaczony do codziennej obsługi quizu.

Zapewnia wygodny dostęp do podstawowych funkcji, takich jak:

- Pytania i rundy głosowania.
- Szablony i zestawy pytań.
- Ranking.
- Historia rund.
- Podstawowe sterowanie i pomoc.

### ADVANCED

Zapewnia dostęp do pełnego zestawu dostępnych ustawień i narzędzi, w tym:

- Zaawansowanych ustawień quizu.
- Zarządzania danymi.
- Zarządzania archiwami.
- Personalizacji overlayu.
- Diagnostyki.
- Lokalnego symulatora quizu.

### LEGACY

Interfejs inspirowany oryginalnym wyglądem **LukiQuizEngine 1.3.0**.

Zachowuje znajomy styl wizualny, jednocześnie korzystając z nowoczesnego silnika V2.

Wybrane funkcje V2, w tym historia rund i archiwa rankingów, pozostają dostępne również w trybie Legacy.

### Wspólne dane i działanie

Wszystkie trzy tryby panelu wykorzystują **ten sam silnik quizowy, ranking i konfigurację**.

Przełączanie między nimi nie resetuje punktacji ani aktywnych ustawień quizu.

Interfejs oferuje również ulepszone rozróżnianie przycisków za pomocą kolorów, wyróżnianie aktywnych elementów i wizualne oznaczanie wybranych opcji.

## Ustawienia języka

LukiQuizEngine 2.1.0 BETA obsługuje:

- **Polski (PL)**
- **Angielski (EN)**

Język można zmienić bezpośrednio w panelu.

Overlay również obsługuje oba języki.

Język overlayu może być automatycznie synchronizowany z językiem panelu lub konfigurowany niezależnie.

Jeżeli synchronizacja jest włączona, overlay korzysta z języka wybranego w panelu.

Zmiana języka interfejsu nie resetuje punktów ani nie modyfikuje wyników głosowania.

**Ważne:** istniejące nazwy komend, identyfikatory Action, wymagane teksty potwierdzeń oraz nazwy zmiennych wewnętrznych pozostają bez zmian niezależnie od wybranego języka.

Przykładowo, poniższe komendy Twitch nadal zachowują oryginalne nazwy:

```text
!punkty
!ranking
```

Wymagane teksty potwierdzeń, takie jak `PRZYWROC` i `USUN ARCHIWA`, również muszą zostać wpisane dokładnie w pokazanej postaci.

## Głosowanie i komendy Twitch

| Komenda | Działanie |
| --- | --- |
| `!1` ... `!12` | Głosowanie na odpowiedź o podanym numerze |
| `!punkty` | Wyświetlenie własnego wyniku |
| `!ranking` | Wyświetlenie najlepszych wyników rankingu na czacie Twitch |
| `!quiz ...` | Komendy moderatorskie obsługiwane przez silnik |

Administracyjne komendy czatu podlegają uprawnieniom nadawcy i moderatorów.

Automatyczne komunikaty o wydarzeniach rundy na czacie Twitch są domyślnie wyłączone:

```csharp
AnnounceRoundEventsInChat = false
```

Ustawienie znajduje się w `QuizEngine.cs`.

### Tryby rozpoznawania głosów

Wersja 2.1.0 wprowadza trzy sposoby rozpoznawania wiadomości zawierających głos.

| Tryb | Akceptowane przykłady | Działanie |
| --- | --- | --- |
| `STRICT` | `!1` | Akceptuje wyłącznie dokładną komendę głosowania |
| `WHITESPACE` | `!1`, `!1 siema` | Pozwala dopisać tekst oddzielony od komendy białym znakiem |
| `ATTACHED` | `!1`, `!1 siema`, `!1siema` | Pozwala również dopisać tekst bezpośrednio po komendzie, bez spacji |

Domyślnym trybem jest `STRICT`.

Wybrany tryb określa sposób interpretowania wiadomości przez silnik. Numer nadal musi odpowiadać jednej z dostępnych odpowiedzi.

### Ważna informacja o trybie ATTACHED

Ten tryb może potencjalnie powodować konflikty z innymi komendami Twitch.

Przykładowo:

```text
!1v1
```

Może zostać zinterpretowane jako głos na odpowiedź `1`, gdy włączony jest tryb `ATTACHED`.

W przypadku transmisji wykorzystujących wiele własnych komend czatu bezpieczniejszym wyborem pod względem unikania konfliktów może być `STRICT` lub `WHITESPACE`.

Silnik odrzuca również określone nieprawidłowe, niewidoczne i sterujące znaki, aby ograniczyć błędne rozpoznawanie głosów.

## Lokalny symulator quizu

Wersja 2.1.0 BETA wprowadza wbudowany lokalny symulator dostępny z poziomu zaawansowanego interfejsu.

Symulator umożliwia testowanie wybranych mechanik quizu bez przeprowadzania prawdziwego głosowania na Twitchu.

### Dostępne funkcje symulatora

- Tworzenie testowego pytania.
- Korzystanie z osobnej kopii aktualnego szkicu pytania.
- Generowanie fikcyjnych widzów.
- Symulowanie losowych głosów.
- Oddawanie i zmienianie pojedynczych głosów testowych.
- Testowanie zasad głosowania.
- Testowanie trybów rozpoznawania głosów `STRICT`, `WHITESPACE` i `ATTACHED`.
- Testowanie punktowania i zasad przyznawania punktów częściowych.
- Testowanie timera, pauzy i wznowienia.
- Zamykanie, ponowne otwieranie, ujawnianie i anulowanie testowych rund.
- Przeglądanie symulowanych wyników.

Symulator obsługuje generowanie do **2000 fikcyjnych uczestników**.

### Oddzielenie danych

Symulator przechowuje własne dane w pamięci bieżącej strony przeglądarki.

**Nie modyfikuje:**

- Prawdziwego rankingu.
- Rzeczywistej historii głosowania.
- Zapisanych archiwów rankingów.
- Aktywnego stanu quizu w Streamer.bot.

Odświeżenie strony powoduje wyczyszczenie symulacji.

### Ograniczenia symulatora

Symulator odtwarza wybrane mechaniki quizu, ale nie zastępuje pełnych testów w rzeczywistym środowisku Streamer.bot.

Nie testuje w pełni:

- Połączenia z Twitchem.
- Wykonywania Action przez Streamer.bot.
- Integracji z OBS.
- Funkcji drużynowych.
- Uprawnień użytkowników czatu.
- Wszystkich ograniczeń częstotliwości wiadomości i mechanizmów cooldown.

Przed wykorzystaniem nowego wydania podczas transmisji należy wykonać również prawdziwy test integracyjny.

## Rankingi, archiwa i przywracanie

### Bieżący ranking i CSV

Sekcja **Ranking** umożliwia:

- Przeglądanie punktów.
- Korygowanie istniejących wyników.
- Eksportowanie rankingu do CSV.
- Importowanie rankingu z CSV.
- Resetowanie rankingu.
- Ręczne tworzenie archiwów rankingu.

Import i reset mogą zmienić aktualne dane punktowe.

Przed wykonaniem operacji zawsze sprawdź podgląd oraz komunikat potwierdzenia.

**Pełny backup stanu** nie jest tym samym co eksport rankingu CSV.

### Automatyczne archiwum przed resetem

Przed zresetowaniem niepustego rankingu silnik tworzy lokalne archiwum JSON.

Jeżeli wymagane utworzenie archiwum nie powiedzie się, reset powinien zostać odrzucony.

Nie usuwaj ręcznie plików kontrolujących operację, gdy trwa archiwizacja lub reset.

Jeżeli panel przestanie odpowiadać, nie zakładaj automatycznie, że reset się nie powiódł.

Przed ponowną próbą zweryfikuj aktualny stan.

### Ręczny zapis i przeglądanie archiwów

Przycisk **Zapisz ranking** tworzy archiwum bez resetowania punktów.

Archiwum można opcjonalnie nazwać.

Sekcja zarządzania archiwami pozwala:

- Przeglądać zapisane archiwa.
- Wyszukiwać archiwa według nazwy.
- Filtrować archiwa według typu.
- Sortować dostępne archiwa.
- Sprawdzać informacje o archiwach.
- Podglądać historyczne wyniki rankingu.
- Przywracać kwalifikujące się archiwa.

Otwieranie lub zamykanie podglądu historycznego rankingu nie zmienia aktualnych wyników.

### Przywracanie archiwalnego rankingu

1. Wybierz archiwum i sprawdź jego podgląd.
2. Zakończ lub anuluj aktywną rundę.
3. Kliknij **Przywróć ranking**.
4. Przeczytaj ostrzeżenie i wpisz `PRZYWROC`.
5. Silnik weryfikuje archiwum i tworzy **archiwum bezpieczeństwa aktualnego rankingu** przed rozpoczęciem przywracania.
6. Sprawdź przywrócony ranking i statystyki.

Przywrócenie archiwalnego rankingu **nie przywraca całej konfiguracji quizu**, szablonów pytań, ustawień overlayu ani kompletnej aktywnej sesji.

Przywrócenie może zostać zablokowane, jeżeli aktualna konfiguracja drużyn nie jest zgodna z wybranym archiwum.

Archiwa rankingów, snapshoty i pełne backupy zachowują różne zakresy danych.

Jeśli panel zgłasza **niepotwierdzony wynik przywrócenia lub resetu**, nie powtarzaj natychmiast operacji.

Najpierw sprawdź aktualny ranking i skorzystaj z dostępnej opcji weryfikacji statusu przywracania.

W razie potrzeby sprawdź również logi Streamer.bot.

### Ręczne usuwanie archiwów

Wersja 2.1.0 BETA dodaje możliwość ręcznego czyszczenia archiwów.

Z poziomu panelu można wybrać kwalifikujące się archiwa do usunięcia.

Przed usunięciem archiwów:

1. Wybierz archiwa przeznaczone do usunięcia.
2. Uruchom podgląd planowanego czyszczenia.
3. Sprawdź listę wybranych plików.
4. Zweryfikuj liczbę i łączny rozmiar archiwów.
5. Wpisz wymagany tekst potwierdzenia:

```text
USUN ARCHIWA
```

Przed usunięciem wybranych plików silnik wykonuje dodatkową walidację.

Chronione archiwa nie mogą zostać usunięte za pomocą standardowego mechanizmu czyszczenia.

Silnik zapobiega również usunięciu wszystkich dostępnych archiwów rankingów.

**Usuwanie archiwów jest operacją destrukcyjną i nie należy traktować jej jak zwykłej czynności, którą można łatwo cofnąć.**

Przed usunięciem ważnych archiwów wykonaj osobną kopię bezpieczeństwa.

### Automatyczne czyszczenie archiwów

Wersja 2.1.0 BETA udostępnia również opcjonalne ustawienia automatycznego przechowywania i czyszczenia archiwów.

Dostępne opcje obejmują:

- Włączanie i wyłączanie automatycznego czyszczenia.
- Konfigurowanie maksymalnej liczby archiwów.
- Konfigurowanie maksymalnego wieku archiwów w dniach.
- Opcjonalne uwzględnianie archiwów utworzonych ręcznie.
- Opcjonalne uwzględnianie archiwów bezpieczeństwa.

Automatyczne czyszczenie jest **domyślnie wyłączone**.

Domyślna konfiguracja po włączeniu funkcji przewiduje maksymalnie `100` archiwów i maksymalny wiek `365` dni.

Archiwa ręczne oraz archiwa bezpieczeństwa są domyślnie wykluczone z automatycznego czyszczenia.

Automatyczne czyszczenie jest wywoływane podczas określonych operacji związanych z archiwizacją i resetowaniem rankingu.

**Nie jest to niezależna usługa działająca stale w tle.**

Przed aktywowaniem funkcji lub rozszerzeniem zasad czyszczenia sprawdź dostępny podgląd i wykonaj osobny backup.

Silnik chroni określone archiwa, w tym najnowsze dostępne archiwum oraz archiwa powiązane z niedokończonymi operacjami.

### Integralność archiwów i ograniczenia

Nowo tworzone archiwa zawierają pole `ContentHash`, które służy do wykrywania przypadkowych uszkodzeń lub nieoczekiwanych zmian zawartości.

Dla zachowania kompatybilności mogą być również akceptowane obsługiwane starsze archiwa bez tego pola.

Zwykły hash SHA-256 **nie jest podpisem cyfrowym**.

Nie zabezpiecza przed osobą, która może celowo zmodyfikować pliki archiwów i odpowiadające im informacje o skrócie.

## Gdzie są przechowywane dane

| Dane | Lokalizacja / mechanizm |
| --- | --- |
| Bieżący stan V2 i ranking | Trwała zmienna Streamer.bot `localQuiz_data_v2` |
| Stan publiczny wykorzystywany przez overlay | Zmienna Streamer.bot `localQuiz_public_v2` |
| Automatyczne snapshoty | Zmienna Streamer.bot `localQuiz_snapshots_v2` |
| Archiwa rankingu JSON (automatyczne, ręczne i bezpieczeństwa) | `%LOCALAPPDATA%\LukiQuizEngine\RankingArchives` |
| Preferencje interfejsu i szkic przeglądarki | Pamięć lokalna przeglądarki (`localStorage`) |
| Pełny backup stanu | Plik eksportowany na żądanie z panelu WWW |
| Hasło WebSocket | Pamięć uruchomionego klienta, a w przypadku OBS również fragment pełnego URL overlayu |

Zmienne oznaczone jako `v1` należą do starszego silnika lub procesu migracji.

**Nie są podstawowym miejscem przechowywania rankingu V2.**

Nie usuwaj ich bez wykonania odpowiednich kopii i upewnienia się, że migracja została zakończona poprawnie.

Folder AppData jest powiązany z aktualnym użytkownikiem systemu Windows.

Jeżeli przenosisz LukiQuizEngine na inny komputer lub konto Windows, skopiuj osobno wszystkie potrzebne archiwa.

Podmiana plików HTML i C# nie przenosi automatycznie lokalnych archiwów.

## Bezpieczeństwo i znane ograniczenia

> [!WARNING]
> **Znane ograniczenie bezpieczeństwa: HTTP `/DoAction` w Streamer.bot.**
>
> Aktualna wersja LukiQuizEngine nie zapewnia niezależnego uwierzytelniania wszystkich administracyjnych wywołań Action wykonywanych przez HTTP API.

### Wymagana konfiguracja lokalna

- **HTTP Server:** `127.0.0.1:7474`, bez publicznego tunelu, reverse proxy i przekierowania portów.
- **WebSocket Server:** `127.0.0.1:8080/`, z `Authentication = ON`, `Enforce = ON` oraz silnym, unikalnym hasłem.
- Nie konfiguruj HTTP ani WebSocket do nasłuchiwania na adresie `0.0.0.0`.
- Nie udostępniaj pełnego adresu źródła przeglądarkowego OBS ani plików zawierających prywatne informacje o widzach.

### Ważna informacja o bezpieczeństwie

Wbudowany HTTP Server Streamer.bot udostępnia endpoint `POST /DoAction`.

Endpoint umożliwia klientom wywoływanie Action i przekazywanie argumentów.

**WebSocket Authentication i Enforce nie zabezpieczają automatycznie tego osobnego endpointu HTTP.**

Klient posiadający dostęp do HTTP API może potencjalnie wywoływać operacje administracyjne LukiQuizEngine **oraz inne Action skonfigurowane w Streamer.bot**.

Ograniczenie HTTP Server do adresu `127.0.0.1` znacząco zmniejsza możliwość dostępu z sieci, ale nie jest równoważne niezależnemu uwierzytelnianiu.

Nie chroni przed niezaufanymi procesami działającymi lokalnie.

Niektóre scenariusze żądań kierowanych z zewnętrznych stron internetowych do usług lokalnych zależą również od konfiguracji przeglądarki i serwera.

**Nie gwarantuję pełnej ochrony przed takimi żądaniami.**

Z tego powodu **nigdy nie udostępniaj wbudowanego HTTP Server Streamer.bot niezaufanym urządzeniom ani użytkownikom**.

Nie zakładaj, że HTTP API jest bezpiecznym, publicznie dostępnym interfejsem administracyjnym.

Oficjalna dokumentacja:

- [HTTP DoAction](https://docs.streamer.bot/api/http/requests/do-action)
- [HTTP Server](https://docs.streamer.bot/api/http/guide/configuration)
- [WebSocket Server](https://docs.streamer.bot/api/websocket/guide/configuration)

### Hasło w adresie URL overlayu

Przycisk **Skopiuj URL overlayu** umieszcza poświadczenie WebSocket we fragmencie adresu URL znajdującym się po `#`.

Fragment ten zwykle nie jest wysyłany w żądaniu HTTP.

Pełny adres URL zostaje jednak zapisany w konfiguracji źródła przeglądarkowego OBS.

Traktuj cały URL overlayu jak poufne dane uwierzytelniające.

Jeżeli adres zostanie ujawniony, zmień hasło WebSocket i wygeneruj nowy URL overlayu.

### Ważne ograniczenia bezpieczeństwa i trwałości danych

- Diagnostyka panelu nie może samodzielnie udowodnić, że skonfigurowane serwery są dostępne wyłącznie lokalnie. Sprawdź rzeczywiste adresy w Streamer.bot.
- Pełna atomowość pomiędzy plikami JSON archiwów a trwałym magazynem Streamer.bot nie została niezależnie potwierdzona w scenariuszach nagłego zaniku zasilania.
- Lokalne rankingi i archiwa mogą zawierać nazwy użytkowników oraz identyfikatory Twitch. Nie publikuj przypadkowo takich plików.
- Wbudowane kopie bezpieczeństwa zmniejszają ryzyko utraty danych, ale nie zastępują osobnego backupu komputera.
- Automatyczne czyszczenie i ręczne usuwanie archiwów wymagają ostrożności, ponieważ mogą trwale usunąć zapisane pliki.

## Aktualizacja starszej instalacji

### Z v2.0.2 BETA do v2.1.0 BETA

1. Zakończ lub anuluj aktywną rundę.
2. Wyeksportuj aktualny ranking i pełny backup stanu.
3. Zachowaj folder poprzedniej instalacji oraz wszystkie ważne archiwa rankingów.
4. Zastąp kod C# w Action nową zawartością pliku `QuizEngine.cs`.
5. Podmień razem pliki `panel.html` i `overlay.html`.
6. W Streamer.bot wykonaj **Find Refs**, a następnie **Save and Compile**.
7. Upewnij się, że silnik i panel pokazują wersję `2.1.0` oraz identyfikatory `PHoeniX` i `QUPA`.
8. W razie potrzeby odśwież pamięć podręczną przeglądarki i źródła przeglądarkowego OBS.
9. Sprawdź istniejący ranking, szablony, zestawy pytań i archiwa.
10. Przetestuj tryby panelu, zmianę języka i głosowanie Twitch.
11. Pozostaw automatyczne czyszczenie archiwów wyłączone, dopóki nie zweryfikujesz istniejących archiwów i nie wykonasz osobnej kopii bezpieczeństwa.

Podczas aktualizacji nie usuwaj trwałych zmiennych V2 ani istniejących archiwów.

### Z v2.0.0 lub v2.0.1 BETA

Przed aktualizacją:

1. Zakończ lub anuluj aktywną rundę.
2. Wyeksportuj aktualny ranking i pełny backup stanu.
3. Zachowaj folder poprzedniej instalacji.
4. Zaktualizuj wszystkie trzy główne pliki razem: `QuizEngine.cs`, `panel.html` i `overlay.html`.
5. Wykonaj **Find Refs** i **Save and Compile**.
6. Zweryfikuj wersję oraz identyfikatory buildu.
7. Zachowaj istniejące trwałe zmienne V2 i archiwa.
8. Przetestuj quiz oraz funkcje archiwizacji na danych nieprodukcyjnych.

Zapoznaj się z CHANGELOG, aby sprawdzić zmiany wprowadzone w wydaniach pośrednich.

### Z v1.3.0

Przed podmianą plików przeczytaj [MIGRATION.md](MIGRATION.md).

Historia migracji oraz znane ograniczenia starszej wersji zostały opisane w osobnym poradniku.

Nie przeprowadzaj migracji podczas aktywnej rundy.

## Rozwiązywanie problemów

| Problem | Co sprawdzić |
| --- | --- |
| Strona wyświetla `404 Not Found` | Sprawdź, czy HTTP Server jest uruchomiony, istnieje mapowanie `quiz`, a wskazany folder zawiera `panel.html` |
| Panel się otwiera, ale nie może się połączyć | Sprawdź WebSocket pod adresem `127.0.0.1:8080`, hasło, Authentication, Enforce i czy serwer został ponownie uruchomiony po zmianach |
| Panel łączy się, ale Action nie odpowiada | Sprawdź dokładną nazwę `QUIZ - Silnik`, aktualny kod C#, **Find Refs** i **Save and Compile** |
| Głosy takie jak `!1` nie działają | Sprawdź połączenie Twitch, trigger wiadomości, czy głosowanie jest otwarte i czy numer odpowiedzi jest prawidłowy |
| Dodatkowy tekst po komendzie głosowania jest odrzucany | Sprawdź, czy wybrano tryb `STRICT`, `WHITESPACE` czy `ATTACHED` |
| Overlay pokazuje demo lub nieaktualne dane | Usuń `demo=1`, użyj adresu skopiowanego po autoryzacji, sprawdź cache i mapowanie HTTP |
| Overlay wyświetla nieodpowiedni język | Sprawdź ustawienia synchronizacji języka oraz konfigurację języka overlayu |
| Folder archiwów się nie otwiera | Sprawdź Windows, uprawnienia AppData, Eksplorator plików i logi Streamer.bot |
| Nie można przywrócić archiwum | Zakończ rundę, sprawdź zgodność konfiguracji drużyn i rewizję rankingu, a następnie ponownie otwórz podgląd |
| Czyszczenie archiwów nie może zostać wykonane | Sprawdź aktywną rundę, chronione archiwa, wygasły podgląd czyszczenia, niedokończone operacje oraz ewentualne uszkodzone pliki |
| Automatyczne czyszczenie nie działa | Sprawdź, czy funkcja jest włączona i czy wykonano operację uruchamiającą czyszczenie |
| Wynik zapisu lub przywrócenia jest niepewny | Nie powtarzaj destrukcyjnej operacji w ciemno. Sprawdź aktualny ranking i status operacji |
| Po błędzie punktowania quiz nie może zakończyć rundy | Sprawdź diagnostykę `autoFinalizeError`, w razie potrzeby świadomie anuluj problematyczną rundę i zweryfikuj punkty |

## Opcjonalna modyfikacja głosowania z v1.3.0

<details>
<summary><strong>Archiwalny poradnik - dodatkowy tekst po komendach !1 ... !12</strong></summary>

Ten poradnik dotyczy wyłącznie **LukiQuizEngine v1.3.0**.

Został zachowany dla osób, które nadal korzystają ze starszego wydania.

Wersja **2.1.0 BETA** ma już konfigurowalne tryby rozpoznawania głosów `STRICT`, `WHITESPACE` i `ATTACHED`.

Nie zastępuj wyrażeń regularnych w aktualnym silniku 2.1.0 poniższymi przykładami.

Domyślnie wersja 1.3.0 wymagała samodzielnych komend głosowania:

```text
!1
!2
!3
```

### Wariant 1 - dodatkowy tekst po białym znaku

Akceptuje:

```text
!1 siema
```

Nie akceptuje:

```text
!1siema
```

Wyrażenie regularne:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?:\s+.*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

### Wariant 2 - dodatkowy tekst bez wymaganej spacji

Akceptuje:

```text
!1siema
```

Może jednak również rozpoznawać początek innych komend, np.:

```text
!1v1
```

Wyrażenie regularne:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?![0-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

W wersji 1.3.0 wystarczyło zastąpić wyłącznie wartość `VoteRegex` w `QuizEngine.cs`, wybierając jeden z dwóch wariantów.

Po wykonaniu zmiany należało kliknąć **Save and Compile**.

Była to opcjonalna modyfikacja, a nie domyślne zachowanie quizu.

W przypadku możliwych konfliktów z innymi komendami Twitch wariant wymagający białego znaku był zazwyczaj lepszym wyborem.

</details>

## Historia projektu i licencja

- [CHANGELOG.md](CHANGELOG.md) - historia wydań i zmian wprowadzanych w LukiQuizEngine V2, w tym wersji 2.1.0 BETA.
- [MIGRATION.md](MIGRATION.md) - instrukcja migracji ze starszej wersji 1.3.0.
- [READMElegacy.md](1.3.0/READMElegacy.md) - archiwalna dokumentacja wersji 1.3.0.
- Starsze wersje głównego README są dostępne w [historii zmian pliku na GitHubie](https://github.com/Lukituki6/LukiQuizEngine-for-Streamer.bot/commits/main/README.md).

Historycznych instrukcji instalacji dla wersji 1.3.0 nie należy traktować jako aktualnych instrukcji dotyczących 2.1.0 BETA.

**Wykorzystanie AI:** przy rozwijaniu projektu korzystałem również z narzędzi sztucznej inteligencji, między innymi podczas analizy kodu i prac programistycznych.

Wszystkie istotne zmiany należy przetestować w docelowym środowisku Streamer.bot.

Projekt udostępniam na warunkach [licencji MIT](LICENSE), **„tak jak jest”**, bez gwarancji działania.
> [!IMPORTANT]
> Korzystasz z niego na własną odpowiedzialność, szczególnie podczas zmieniania konfiguracji, importowania danych i zarządzania archiwami.
