> [!NOTE]
> This project is not affiliated with, endorsed by, or officially supported by Streamer.bot, Twitch, YouTube, or Kick. Streamer.bot, Twitch, YouTube, and Kick are trademarks or names of their respective owners. This project only uses features available in Streamer.bot and is provided as an independent community tool. 

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
# Quiz do Streamer.bot + OBS (overlay)

Lokalny system quizowy: panel sterowania w przeglądarce,
overlay do OBS, głosowanie z czatu, punktacja i trwały ranking.

System działa na komputerze streamera. Nie wymaga hostingu, bazy danych,
abonamentu ani dodatkowego klucza API.

## Informacja dotycząca użycia AI

Przez dużą ilość błędów jak i skomplikowanych funkcji, postanowiłem końcowo wesprzeć się pomocą AI. Także kod był pisany wspomagając się sztuczną inteligencją w celu przyśpieszenia procesu pracy.

**AI-assisted**

# Informacje o wydaniu 2.0.0

<details>
<summary><strong>Zmiany z wersji 2.0.0</strong></summary>

### 2.0.0 BETA - duża aktualizacja

**AI-Assisted**

Wersja 2.0.0 przebudowuje quiz praktycznie od podstaw. Nadal wystarczy Streamer.bot, jedna Action z kodem C#, jeden trigger wiadomości Twitcha oraz lokalny panel i overlay HTML. Nie trzeba instalować osobnej aplikacji, Node.js ani zewnętrznej bazy danych.

#### Quiz i głosowanie

- Pytania mogą mieć od 2 do 12 odpowiedzi, w tym kilka poprawnych. Każda odpowiedź może mieć własną liczbę punktów.
- Rozbudowano punktację: tryby `adjacent`, `manual`, `none` i `custom`, punkty częściowe oraz opcjonalny bonus za szybką poprawną odpowiedź.
- Dodano losowanie kolejności odpowiedzi bez zmiany tego, która odpowiedź jest poprawna i ile jest warta.
- Dostępne są trzy zasady głosowania: ostatni głos (`LAST`), pierwszy głos (`FIRST`) oraz ograniczona liczba zmian (`LIMITED`).
- Można wybrać, co widzowie widzą przed ujawnieniem wyników: pełny rozkład głosów (`LIVE`), samą ich liczbę (`COUNT_ONLY`) albo ukryte wyniki (`HIDDEN`).
- W panelu można sprawdzić, kto zagłosował na daną odpowiedź, a także ręcznie zmienić lub usunąć głos przed ujawnieniem wyniku.
- Dodano tryb ankiety (`POLL`), który zbiera głosy, ale nie przyznaje punktów do rankingu quizu.

#### Rundy, czas i wyniki

- Dodano historię rund z oddanymi głosami i przyznanymi punktami.
- Można cofnąć ostatni kwalifikujący się wynik lub przeliczyć zachowaną rundę po poprawieniu odpowiedzi albo punktacji.
- Timer działa po stronie silnika. Obsługuje pauzę, wznowienie, dodawanie i odejmowanie czasu oraz opcjonalne automatyczne ujawnienie wyniku.
- Po ponownym połączeniu panel i overlay pobierają aktualny stan zamiast zaczynać rundę od nowa.
- Można ustawić cooldowny komend `!punkty` i `!ranking`. Moderatorzy i właściciel kanału są z nich zwolnieni.
- Komendy `!1` do `!12`, `!punkty`, `!ranking` oraz moderatorskie `!quiz` pozostają dostępne.

#### Pytania i zestawy

- Zapisane pytania można edytować, powielać i wykorzystywać w wielu zestawach.
- Zestawy działają jak playlisty: można zmieniać kolejność pytań, losować ją i przechodzić do następnego lub poprzedniego pytania.
- Szkic pytania można zapisać w silniku, a następnie wczytać w panelu.
- Dodano import i eksport pytań oraz zestawów w formacie JSON z podglądem i walidacją.
- Przewidziano migrację starych szablonów i szkicu zapisanych w przeglądarce, o ile są nadal dostępne pod tym samym adresem lokalnym.

#### Drużyny, panel i OBS

- Dodano tryb drużynowy z przypisywaniem widzów i osobnym naliczaniem punktów drużyn.
- Rozbudowano lokalny panel o sterowanie rundą, historię, ranking, ustawienia i diagnostykę instalacji.
- Dodano konfigurowalne skróty klawiaturowe, wykrywanie konfliktów oraz blokowanie skrótów podczas wpisywania tekstu.
- Overlay oferuje układy `FULL`, `COMPACT`, `MINIMAL` i `NO_LEADERBOARD`, a także ustawienia położenia, skali, kolorów, rankingu, procentów i timera.
- Dodano opcjonalne animacje, proste dźwięki generowane lokalnie oraz podium TOP 3.
- Podgląd demonstracyjny overlayu działa bez uruchamiania quizu.

#### Dane, migracja i bezpieczeństwo

- Dane quizu są przechowywane w trwałym stanie V2 po stronie Streamer.bot. Silnik waliduje je przed zapisem i odczytem.
- Dodano pełny backup stanu, przywracanie kopii i maksymalnie pięć automatycznych snapshotów. CSV nadal obejmuje sam ranking, a nie cały quiz.
- Zmiany punktów przez import CSV, reset i korektę ręczną oddzielają starszą historię od kolejnych przeliczeń. W razie potrzeby trzeba przywrócić odpowiedni backup lub snapshot.
- Migracja z 1.3.0 zachowuje stare zmienne; przed utworzeniem stanu V2 zapisywana jest osobna kopia danych migracyjnych.
- Panel i overlay korzystają z lokalnego HTTP i WebSocket z wymaganym `Authentication` oraz `Enforce`. Pozostawiono kontrole wersji komponentów i ograniczenia rozmiaru importowanych danych.
- Hasło WebSocket nie jest wpisane na stałe do kodu ani dodawane do backupu. **Wygenerowany URL overlayu zawiera jednak hasło i trzeba traktować go jako poufny.**

#### Ważne przed instalacją

- To **wydanie BETA**. Przed wykorzystaniem na transmisji warto przetestować je na swoim Streamer.bot, Twitchu i OBS. Testy w samym panelu nie zastępują sprawdzenia głosowania na prawdziwym czacie ani obrazu i dźwięku w OBS.
- Automatyczne komunikaty o rozpoczęciu i zakończeniu rundy są **domyślnie wyłączone** (`AnnounceRoundEventsInChat = false`). Można je włączyć w `QuizEngine.cs`.
- Aktualna wersja nie prowadzi statystyk serii poprawnych odpowiedzi (`current/best streak`). Starsze pola tych statystyk są usuwane podczas odczytu zgodnych danych.
- Cofanie i przeliczanie działa tylko dla odpowiednich rund zachowanych w bieżącej epoce punktacji. Cofnięcie do 1.3.0 nie przenosi automatycznie wyników zdobytych w 2.0.0.
- Instrukcja przejścia ze starszej wersji jest w [MIGRATION.md](MIGRATION.md).

</details>

<details>
<summary><strong>Aktualizacja z wersji 1.3.0 do 2.0.0</strong></summary>

Ten poradnik dotyczy aktualizacji istniejącej instalacji. Jeżeli instalujesz quiz po raz pierwszy, skorzystaj z instrukcji w `README.md`.

### Zanim zaczniesz

1. Zakończ i rozlicz bieżącą rundę albo ją anuluj. Aktualizację najlepiej wykonać wtedy, gdy quiz nie jest aktywny.
2. W wersji 1.3.0 wyeksportuj ranking do CSV i zrób kopię folderu oraz danych Streamer.bot.
3. Zachowaj poprzednie pliki `QuizEngine.cs`, `panel.html` i `overlay.html`. Pozwoli to wrócić do poprzedniej wersji.
4. Nie usuwaj globalnych zmiennych Streamer.bot ani danych przeglądarki (`localStorage`).
5. Zamknij stary panel i na czas aktualizacji wyłącz lub odśwież stare źródło przeglądarkowe w OBS, żeby nie komunikowało się z nowym silnikiem.

### Aktualizacja plików

1. Umieść nowe `panel.html` i `overlay.html` w katalogu udostępnianym przez lokalny HTTP Server Streamer.bot. Możesz też przestawić mapowanie HTTP na nowy katalog.
2. W istniejącej Action `QUIZ - Silnik` **zastąp cały kod C#** zawartością nowego `QuizEngine.cs`. Nie zostawiaj dwóch aktywnych kopii silnika.
3. Użyj `Find Refs`, a następnie `Save and Compile`. Sprawdź, czy kod kompiluje się bez błędów.
4. Zachowaj trigger `Twitch > Chat > Message` przypisany do tej samej Action. Nie trzeba tworzyć osobnych triggerów dla odpowiedzi.
5. Zostaw włączone `Authentication` i `Enforce` dla lokalnego WebSocket Server. Panel i overlay wymagają tych zabezpieczeń.
6. Otwórz panel, np. `http://127.0.0.1:7474/quiz/panel.html?v=2.0.0-beta`, wpisz hasło WebSocket i kliknij **Połącz bezpiecznie**. Jeśli masz inne porty lub mapowanie HTTP, użyj swoich adresów.
7. Po połączeniu otwórz **Diagnostyka** i uruchom **Test instalacji**.

### Co silnik przenosi z 1.3.0

Nowa wersja przechowuje główne dane w zmiennej `localQuiz_data_v2` (State Schema 2). **To nie jest nazwa `localQuiz_data_2.0.0`.**

Jeśli stan V2 jeszcze nie istnieje, silnik sprawdza dostępne dane z 1.3.0:

- `localQuiz_scores_v1` - trwały ranking;
- `localQuiz_scores_before_import_v1` - trwała kopia rankingu sprzed importu, jeżeli istnieje;
- `localQuiz_state_v1` - poprzedni stan rundy, jeżeli nadal jest dostępny;
- `localQuiz_voterNames_v1` - zapisane nazwy głosujących, jeżeli nadal są dostępne.

Silnik zapisuje osobną, trwałą kopię tych wartości pod nazwą `localQuiz_migration_backup_v1`, waliduje wynik migracji, tworzy snapshot i zapisuje stan V2. Nie usuwa źródłowych zmiennych v1.

Istniejące punkty, liczba poprawnych i oddanych odpowiedzi oraz identyfikatory widzów są przenoszone, o ile dane przejdą walidację. Jeśli stara runda nie została zachowana przez Streamer.bot, nowa wersja nie będzie mogła jej odtworzyć. Dlatego najlepiej aktualizować poza aktywnym quizem.

Jeżeli `localQuiz_data_v2` już istnieje, silnik wczytuje ten stan zamiast ponownie migrować ranking z 1.3.0. Jeśli stan V2 jest uszkodzony, operacja zostaje odrzucona; **nie kasuj zmiennych**, żeby wymusić pusty start.

Uwaga: starsze pola statystyk serii (`CurrentStreak`, `BestStreak`) nie są już częścią aktualnego rankingu i mogą zostać usunięte podczas odczytu danych.

### Szkice i szablony z przeglądarki

Aby przenieść stare lokalne szablony i szkic, otwórz nowy panel z tego samego originu co wcześniej: **ten sam host i port**. `localhost` oraz `127.0.0.1` to dla przeglądarki dwa różne miejsca przechowywania danych.

Panel szuka dawnych danych w `localQuiz.templates.v1` i `localQuiz.draft`. Bieżący szkic V2 jest zapisywany w `localQuiz.draft.v2` oraz może być zapisany w silniku. Do odczytu wersji z silnika służy przycisk **Wczytaj szkic z silnika**.

Jeżeli po aktualizacji nie widzisz starych szablonów, sprawdź najpierw, czy otworzyłeś panel pod tym samym adresem lokalnym. Nie czyść danych przeglądarki, dopóki nie upewnisz się, że migracja się udała.

### Sprawdzenie po aktualizacji

1. Porównaj liczbę użytkowników i punkty w nowym rankingu z eksportem CSV z 1.3.0.
2. Sprawdź szablony, zestawy i szkic. Zwróć uwagę na ewentualne komunikaty o błędach migracji.
3. Połącz panel i **wygeneruj nowy URL overlayu**. Wklej go do źródła przeglądarkowego OBS. Adres zawiera hasło WebSocket, więc nie udostępniaj go.
4. Przeprowadź krótką rundę testową: rozpoczęcie, głos `!1`/`!2`, zamknięcie, ujawnienie wyniku i ranking.
5. Jeśli korzystasz z dodatkowych funkcji, sprawdź timer, zapis szablonów, cofanie i przeliczanie wyniku, import CSV, drużyny oraz backup.
6. Wyeksportuj pełny backup z 2.0.0 i przechowuj go w bezpiecznym miejscu. Może zawierać identyfikatory i nazwy widzów oraz historię głosów.

### Co zrobić, jeśli migracja się nie uda

Nie usuwaj starych danych ani nie resetuj rankingu w ciemno. Sprawdź logi Action w Streamer.bot, zachowaj kopię istniejących zmiennych i porównaj je z wcześniejszym eksportem CSV. Silnik odrzuca niepoprawny JSON i nie powinien zastępować uszkodzonego stanu pustym rankingiem.

Pamiętaj, że test połączenia HTTP/WebSocket nie potwierdza działania Twitcha i OBS. Te elementy sprawdź osobno.

### Powrót do 1.3.0

Możesz przywrócić stare pliki i kod Action. Stare zmienne v1 pozostają dostępne, więc poprzedni silnik powinien zobaczyć stan sprzed migracji, o ile te dane nie zostały osobno zmienione.

**Punkty i rundy zdobyte już w 2.0.0 nie przejdą automatycznie do 1.3.0.** Jeśli chcesz zachować nowy ranking przy powrocie, wyeksportuj CSV w 2.0.0 i sprawdź zgodność pliku z importerem 1.3.0 przed jego użyciem. Zestawy, drużyny i historia V2 nie mają bezpośrednich odpowiedników w 1.3.0. Zachowaj również pełny backup 2.0.0.

</details>



## Spis treści

- [Możliwości](#możliwości)
- [Zawartość repozytorium](#zawartość-repozytorium)
- [Wymagania](#wymagania)
- [Instalacja od zera](#instalacja-od-zera)
- [Pierwszy test](#pierwszy-test)
- [Obsługa panelu](#obsługa-panelu)
- [Komendy czatu](#komendy-czatu)
- [Punktowanie](#punktowanie)
- [Szkic i szablony](#szkic-i-szablony)
- [Ranking oraz CSV](#ranking-oraz-csv)
- [Gdzie są zapisywane dane](#gdzie-są-zapisywane-dane)
- [Bezpieczeństwo](#bezpieczeństwo)
- [Konfiguracja własna](#konfiguracja-własna)
- [Rozwiązywanie problemów](#rozwiązywanie-problemów)
- [Licencja](#licencja)

## Możliwości

- Od 2 do 12 odpowiedzi w jednym pytaniu.
- Głosowanie widzów komendami `!1`, `!2`, `!3` itd.
- Jeden aktywny głos na osobę; głos można zmienić do zamknięcia rundy.
- Ręczne zamknięcie albo automatyczny timer do 3600 sekund.
- Ujawnienie poprawnej odpowiedzi dopiero przez streamera.
- Pełne punkty za poprawną odpowiedź.
- Opcjonalne punkty częściowe:
  - automatycznie za odpowiedzi bezpośrednio obok poprawnej;
  - za dowolne odpowiedzi ręcznie wskazane w panelu.
- Trwały ranking zapisany przez Streamer.bot.
- Komendy widzów `!punkty` i `!ranking`.
- Komendy administracyjne `!quiz ...` dla broadcastera i moderatorów.
- Korekta punktów z poziomu panelu.
- Eksport i import rankingu CSV.
- Tryb zastąpienia albo połączenia rankingu.
- Cofnięcie ostatniego udanego importu.
- Wiele nazwanych szablonów pytań.
- Automatycznie przywracany szkic roboczy.
- Overlay 1920×1080 z pytaniem, głosami, procentami, timerem i TOP 5.
- Maksymalnie trzy kolumny odpowiedzi.
- Uwierzytelnienie WebSocket hasłem i wymuszone `Enforce`.
- Ochrona przed nieprawidłowym lub złośliwym CSV i zbyt dużymi danymi.

Automatyczne wiadomości o rozpoczęciu, zamknięciu i wyniku rundy są domyślnie
wyciszone. Bot nadal odpowiada na `!punkty`, `!ranking` oraz polecenia
administracyjne.

## Zawartość repozytorium

| Plik | Zastosowanie |
| --- | --- |
| `QuizEngine.cs` | Silnik wklejany do akcji C# w Streamer.bot |
| `panel.html` | Lokalny panel sterowania quizem |
| `overlay.html` | Źródło przeglądarkowe do OBS |
| `OTWORZ_PANEL.url` | Skrót do panelu po konfiguracji serwera HTTP |
| `PODGLAD_OVERLAYU.url` | Skrót do demonstracyjnego wyglądu overlayu |
| `README.md` | Pełna instrukcja |
| `LICENSE` | Licencja MIT |


Projekt nie pobiera bibliotek JavaScript i nie wymaga `npm`, Node.js ani
kompilowania strony.

## Wymagania

- Komputer z uruchomionym Streamer.bot.
- Połączone konto Twitch w Streamer.bot.
- OBS Studio do wyświetlania overlayu.
- Streamer.bot z dostępnymi:
  - `Core > C# > Execute C# Code`;
  - `Servers/Clients > WebSocket Server`;
  - `Servers/Clients > HTTP Server`.
- Aktualna przeglądarka do otwarcia panelu.

Zalecana jest aktualna stabilna wersja Streamer.bot.

## Instalacja od zera

### 1. Pobierz i umieść folder

Pobierz ZIP i rozpakuj folder w stałym miejscu, na przykład:

```text
C:\StreamerBot\QuizLoL
```

Nie przenoś folderu po ustawieniu mapowania HTTP. Jeżeli go przeniesiesz,
zaktualizuj mapowanie w Streamer.bot.

### 2. Połącz Twitch

W Streamer.bot sprawdź konto broadcastera i opcjonalne konto bota:

```text
Platforms > Twitch > Accounts
```

### 3. Utwórz akcję silnika

1. Otwórz kartę **Actions**.
2. Kliknij prawym przyciskiem i dodaj nową akcję.
3. Nazwij ją dokładnie:

   ```text
   QUIZ - Silnik
   ```

4. Do akcji dodaj:

   ```text
   Core > C# > Execute C# Code
   ```

5. Otwórz `QuizEngine.cs`.
6. Skopiuj całą zawartość pliku.
7. Usuń domyślny kod z edytora Streamer.bot i wklej skopiowaną zawartość.
8. Kliknij **Find Refs**.
9. Kliknij **Compile** albo **Save and Compile**.
10. Kompilacja musi zakończyć się bez czerwonych błędów.
11. Zapisz sub-action i upewnij się, że akcja jest włączona.

Nazwa `QUIZ - Silnik` jest używana przez panel i overlay. Jeżeli wpiszesz inną
nazwę, przyciski panelu nie będą wywoływały akcji.

### 4. Dodaj jeden trigger wiadomości Twitch

Do tej samej akcji `QUIZ - Silnik` dodaj trigger:

```text
Twitch > Chat > Message
```

Jeden trigger obsługuje:

- `!1`-`!12`;
- `!punkty`;
- `!ranking`;
- `!quiz ...`.

Nie twórz dwunastu oddzielnych komend w zakładce **Commands**. Kod ignoruje
zwykłe wiadomości, które nie pasują do obsługiwanych poleceń.

### 5. Skonfiguruj bezpieczny WebSocket Server

Otwórz:

```text
Servers/Clients > WebSocket Server
```

Ustaw:

```text
Auto Start:       wyłączone (zalecane)
Address / Host:   127.0.0.1
Port:             8080
Endpoint:         /
Authentication:   włączone
Enforce:          włączone
Password:         własne, unikalne hasło
```

Hasło musi mieć co najmniej 12 znaków. Zalecane jest co najmniej 16 znaków.
Nie używaj hasła z Twitcha, Discorda ani innego konta.

Po zapisaniu zmian uruchom WebSocket Server. Jeżeli był już aktywny, zatrzymaj
go i uruchom ponownie.

`Authentication` uruchamia logowanie klientów. `Enforce` wymusza zalogowanie
przed każdym żądaniem. Panel i overlay odmówią działania, jeżeli `Enforce`
pozostanie wyłączone.

### 6. Skonfiguruj lokalny HTTP Server

Otwórz:

```text
Servers/Clients > HTTP Server
```

Ustaw:

```text
Auto Start: wyłączone (zalecane)
Host:       127.0.0.1
Port:       7474
```

W sekcji **Mappings** dodaj:

```text
Path:   quiz
Folder: C:\StreamerBot\QuizLoL
```

W polu `Folder` wskaż faktyczną lokalizację rozpakowanego wydania. Wartość
`quiz` w polu `Path` odpowiada części `/quiz` w adresie strony.

Uruchom HTTP Server.

### 7. Otwórz i połącz panel

Otwórz w zwykłej przeglądarce:

```text
http://127.0.0.1:7474/quiz/panel.html?v=130
```

Możesz także użyć `OTWORZ_PANEL.url`.

1. Wpisz to samo hasło, które ustawiono w WebSocket Server.
2. Kliknij **Połącz bezpiecznie**.
3. Poczekaj na zielony komunikat:

   ```text
   Bezpiecznie połączono ze Streamer.bot
   ```

Panel nie zapisuje hasła. Po pełnym odświeżeniu karty trzeba wpisać je ponownie.

### 8. Dodaj uwierzytelniony overlay do OBS

Nie kopiuj zwykłego adresu `overlay.html`. Najpierw połącz panel, a następnie:

1. Kliknij w panelu **Skopiuj URL overlayu**.
2. W OBS dodaj źródło **Browser / Przeglądarka**.
3. Wklej cały skopiowany adres do pola URL.
4. Ustaw:

   ```text
   Width:  1920
   Height: 1080
   ```

5. Pozostaw wyłączone:
   - **Shutdown source when not visible**;
   - **Refresh browser when scene becomes active**.
6. Nie dodawaj własnego CSS. Strona ma już przezroczyste tło.

Skopiowany adres zawiera hasło w części po znaku `#`. Fragment nie jest
wysyłany do serwera HTTP, ale pełny URL zostaje zapisany w konfiguracji OBS.
Nie pokazuj go na streamie i nikomu go nie wysyłaj.

Podgląd demonstracyjny, który nie wymaga połączenia:

```text
http://127.0.0.1:7474/quiz/overlay.html?demo=1&v=130
```

Do prawdziwego źródła OBS nie dodawaj `demo=1`.


## Pierwszy test

1. Sprawdź, czy Twitch, WebSocket Server i HTTP Server są połączone.
2. Otwórz panel i zaloguj go hasłem WebSocket.
3. Wpisz pytanie oraz przynajmniej dwie odpowiedzi.
4. Ustaw czas, np. `60`, albo `0` dla ręcznego zamknięcia.
5. Kliknij **Rozpocznij głosowanie**.
6. Napisz na czacie Twitch `!1` z konta innego niż konto bota.
7. Sprawdź, czy liczba głosów zmieniła się w panelu i overlayu.
8. Napisz `!2` tym samym kontem - głos powinien zostać przeniesiony.
9. Kliknij **Zamknij głosowanie**.
10. Wybierz poprawną odpowiedź i wartości punktów.
11. Kliknij **Ujawnij i przyznaj**.
12. Sprawdź na czacie `!punkty` i `!ranking`.
13. Wyeksportuj testowy ranking do CSV.

## Obsługa panelu

- **Rozpocznij głosowanie** - tworzy rundę i pokazuje overlay.
- **Zamknij głosowanie** - blokuje nowe głosy oraz ich zmianę.
- **Ujawnij i przyznaj** - pokazuje wynik i jednorazowo przyznaje punkty.
- **Pokaż overlay** - przywraca widoczność istniejącej rundy.
- **Ukryj overlay** - ukrywa overlay bez kasowania rundy.
- **Anuluj rundę** - usuwa bieżące pytanie bez przyznawania punktów.
- **Korekta** - ustawia nową liczbę punktów wybranego widza.
- **Eksport CSV** - pobiera pełną kopię rankingu.
- **Import CSV** - przygotowuje odtworzenie lub połączenie rankingu.
- **Cofnij ostatni import** - przywraca kopię wykonaną przed ostatnim importem.
- **Wyzeruj ranking** - usuwa ranking po wpisaniu `RESET`.

Punkty są przyznawane tylko raz dla danej rundy. Ponowne kliknięcie ujawnienia
nie powinno ich naliczyć drugi raz.

## Komendy czatu

### Komendy widzów

| Komenda | Działanie |
| --- | --- |
| `!1`-`!12` | Oddaje lub zmienia głos na istniejącą odpowiedź |
| `!punkty` | Pokazuje własne punkty i liczbę poprawnych odpowiedzi |
| `!ranking` | Pokazuje TOP 5 na czacie |

Głos musi składać się wyłącznie z komendy, np. `!3`. Wiadomość
`wybieram !3` nie zostanie uznana za głos.

### Komendy administracyjne

Tylko broadcaster i moderatorzy:

| Komenda | Działanie |
| --- | --- |
| `!quiz pomoc` | Pokazuje skróconą pomoc |
| `!quiz start Pytanie \| Odp. 1 \| Odp. 2...` | Rozpoczyna rundę bez timera |
| `!quiz zamknij` | Zamyka głosowanie |
| `!quiz wynik 3` | Ujawnia odpowiedź nr 3 i przyznaje 1 punkt |
| `!quiz wynik 3 5` | Ujawnia odpowiedź nr 3 i przyznaje 5 punktów |
| `!quiz wynik 7 3 1` | `!7` daje 3 pkt, a `!6` i `!8` po 1 pkt |
| `!quiz anuluj` | Anuluje rundę bez punktów |
| `!quiz pokaz` | Pokazuje overlay |
| `!quiz ukryj` | Ukrywa overlay |

Komenda czatowa `!quiz wynik NUMER PEŁNE CZĘŚCIOWE` używa automatycznego
punktowania odpowiedzi sąsiednich. Dowolne odpowiedzi częściowe wybiera się
w panelu.

## Punktowanie

### Pełne punkty

Pole **Za poprawną** określa liczbę punktów za dokładne trafienie.

### Punkty częściowe automatyczne

Tryb **Automatycznie obok poprawnej** wybiera odpowiedź o jeden numer niższą
i wyższą.

| Ustawienie | Wynik |
| --- | --- |
| Poprawna `!7`, pełne `3`, częściowe `1` | `!7`: 3 pkt; `!6` i `!8`: po 1 pkt |

Przy poprawnej `!1` istnieje tylko sąsiad `!2`. Przy ostatniej odpowiedzi
istnieje tylko sąsiad o jeden numer niższy.

### Punkty częściowe ręczne

Tryb **Wybieram ręcznie** pozwala wskazać jedną lub wiele dowolnych pozycji.
Poprawna odpowiedź jest automatycznie wyłączona z listy częściowej.

| Ustawienie | Wynik |
| --- | --- |
| Poprawna `!4`, częściowe `!2` i `!9` | `!4`: 3 pkt; `!2` i `!9`: po 1 pkt |

Wartość `0` w polu punktów częściowych wyłącza ten mechanizm.

Pole **Poprawne** w rankingu zwiększa się tylko po dokładnym trafieniu.
Odpowiedź częściowa zwiększa punkty i liczbę odpowiedzianych rund, ale nie
liczbę poprawnych odpowiedzi.

## Szkic i szablony

Panel przechowuje lokalnie dwa rodzaje danych:

- **Szkic roboczy** - obecny formularz. Zapisuje się automatycznie i wraca po
  ponownym otwarciu panelu.
- **Szablony pytań** - do 100 nazwanych zestawów. Każdy zapisuje pytanie,
  odpowiedzi, czas, wartości punktów i tryb punktowania częściowego.

Aby zapisać szablon:

1. Uzupełnij formularz.
2. Wpisz nazwę szablonu.
3. Kliknij **Zapisz jako nowy**.

Aby go użyć:

1. Wybierz nazwę z listy.
2. Kliknij **Wczytaj**.
3. Sprawdź pola.
4. Rozpocznij głosowanie.

**Nadpisz** aktualizuje wybrany szablon. **Usuń** kasuje tylko wybrany szablon.

Szkic i szablony należą do konkretnej przeglądarki oraz originu.
`127.0.0.1` i `localhost` mają oddzielną pamięć. Tryb prywatny oraz
wyczyszczenie danych witryny mogą usunąć szkic i szablony, ale nie ranking
zapisany w Streamer.bot.

## Ranking oraz CSV

### Eksport

Przycisk **Eksport CSV** tworzy plik:

```text
ranking-quiz-RRRR-MM-DD.csv
```

Plik ma kodowanie UTF-8 z BOM, separator `;` i kolumny:

```text
Miejsce
ID użytkownika
Użytkownik
Punkty
Poprawne
Odpowiedziane
Ostatnia aktualizacja
```

CSV zawiera nazwy i Twitch User ID widzów. Traktuj go jak kopię prywatnych
danych.

### Import

1. Kliknij **Import CSV**.
2. Wybierz plik.
3. Sprawdź liczbę osób i sumę punktów.
4. Wybierz tryb:
   - **Zastąp cały ranking**;
   - **Połącz z obecnym**.
5. Potwierdź operację.

Tryb **Połącz** nie dodaje punktów z dwóch wpisów. Dane z CSV zastępują dane
pasującej osoby, a osoby nieobecne w CSV pozostają w rankingu.

Importer rozpoznaje separator `;`, `,` albo tabulator. Wymagane są kolumny
użytkownika i punktów. Obsługiwane są również starsze kopie bez Twitch User ID;
takie wpisy są dopasowywane po nazwie widza.
(nie opublikowałem wersji ze starszymi kopiami CSV, ta notka jest głównie po to, abym nie zapomniał XD)

Przed każdym udanym importem silnik zapisuje poprzedni ranking w:

```text
localQuiz_scores_before_import_v1
```

**Cofnij ostatni import** przywraca tę kopię po wpisaniu `COFNIJ`.

Import jest sprawdzany w panelu oraz ponownie w C#. Odrzucane są m.in.
duplikaty, liczby ujemne, niemożliwe statystyki, formuły arkusza, znaki
kontrolne, zbyt długie wartości oraz zbyt duże pliki.

## Gdzie są zapisywane dane

| Dane | Miejsce | Trwałość |
| --- | --- | --- |
| Ranking | `localQuiz_scores_v1` | Globalna trwała |
| Kopia sprzed importu | `localQuiz_scores_before_import_v1` | Globalna trwała |
| Bieżąca runda | `localQuiz_state_v1` | Globalna nietrwała |
| Nazwy głosujących | `localQuiz_voterNames_v1` | Globalna nietrwała |
| Szkic | `localQuiz.draft` | `localStorage` przeglądarki |
| Szablony | `localQuiz.templates.v1` | `localStorage` przeglądarki |
| Lokalny adres WebSocket | `localQuiz.wsUrl` | `localStorage` przeglądarki |
| Hasło WebSocket | RAM karty / fragment URL overlayu | Panel go nie zapisuje |

Ranking przetrwa wyłączenie Streamer.bot i restart komputera. Bieżąca,
niedokończona runda jest celowo nietrwała.

Warto okresowo:

- eksportować ranking do CSV;
- wykonywać kopię zapasową danych Streamer.bot;
- przechowywać kopie poza folderem publicznego repozytorium.

## Bezpieczeństwo

### Co zabezpiecza v1.3.0

- Oficjalne uwierzytelnianie WebSocket challenge + salt + SHA-256.
- Wymagane jednocześnie `Authentication` i `Enforce`.
- Akceptowane tylko `ws://127.0.0.1` oraz `ws://localhost`.
- Panel i overlay działają tylko z lokalnego HTTP.
- Operacje panelu są blokowane przed poprawnym logowaniem.
- Hasło nie trafia do `localStorage`, szkicu, szablonów, CSV ani C#.
- Content Security Policy blokuje zewnętrzne skrypty, formularze, obiekty
  i niedozwolone połączenia.
- Ograniczenia długości komunikatów WebSocket, pytań, odpowiedzi i importu.
- Lista dozwolonych operacji po stronie C#.
- Podwójna walidacja importu CSV.
- Neutralizowanie potencjalnych formuł arkusza w eksporcie.
- Dokładne teksty potwierdzeń przy resecie, imporcie i przywracaniu.

### Ważny kompromis overlayu

Po kliknięciu **Skopiuj URL overlayu** hasło znajduje się po `#`, np.:

```text
overlay.html?v=130#password=...&ws=...
```

Fragment po `#` nie jest wysyłany do HTTP Server, ale pełny adres jest zapisany
przez OBS. Dlatego:

- nie pokazuj pola URL źródła na streamie;
- nie wysyłaj tego adresu innej osobie;
- po zmianie hasła wygeneruj i wklej nowy URL;
- nie publikuj konfiguracji OBS zawierającej ten adres.

### Zasady bezpiecznej konfiguracji

- WebSocket i HTTP ustawiaj na `127.0.0.1`, nigdy `0.0.0.0`.
- Nie przekierowuj portów `7474` ani `8080` na routerze.
- Nie wyłączaj `Enforce`.
- Używaj unikalnego hasła, najlepiej 16+ znaków.
- Nie wpisuj hasła do `QuizEngine.cs`, README ani innych plików repozytorium.
- Nie dodawaj hasła jako `?password=` w adresie.
- Nie udostępniaj uwierzytelnionego URL overlayu.
- Nie używaj tej konfiguracji do zdalnego lub wielokomputerowego dostępu.

Zwykłe `ws://` jest używane celowo, ponieważ połączenie pozostaje na tym samym
komputerze przez loopback. Ta konfiguracja nie chroni przed złośliwym
oprogramowaniem ani osobą mającą dostęp do komputera lub konfiguracji OBS.

## Konfiguracja własna

### Zmiana komend

Na początku `QuizEngine.cs` znajdują się:

```csharp
private const string PointsCommand = "!punkty";
private const string RankingCommand = "!ranking";
private const string AdminCommand = "!quiz";
```

### Włączenie automatycznych wiadomości rundy

Domyślnie:

```csharp
private const bool AnnounceRoundEventsInChat = false;
```

Zmiana na `true` włącza wiadomości o rozpoczęciu, zamknięciu i wyniku rundy.
Odpowiedzi na `!punkty`, `!ranking` i komendy administratora działają niezależnie
od tej opcji.

### Zmiana nazwy akcji

Domyślna nazwa:

```text
QUIZ - Silnik
```

Jeżeli ją zmienisz, ustaw tę samą wartość `ACTION_NAME` w `panel.html` oraz
`overlay.html`. Najprościej pozostawić nazwę domyślną.

### Zmiana rozmiaru tekstu overlayu

W `overlay.html` najważniejsze reguły CSS to:

```css
.question {
  font-size: clamp(24px, 1.75vw, 34px);
}

.answer-text {
  font-size: 16px;
}

.answer-index {
  font-size: 15px;
}

.answer-score {
  font-size: 12px;
}
```

Po zmianie zapisz plik i odśwież źródło przeglądarkowe w OBS.

## Rozwiązywanie problemów

### Panel pokazuje „Włącz Authentication i Enforce”

1. Otwórz WebSocket Server.
2. Włącz **Authentication**.
3. Włącz **Enforce**.
4. Ustaw hasło.
5. Zrestartuj WebSocket Server.
6. Połącz panel ponownie.

Samo pole hasła lub samo `Authentication` nie wystarcza.

### Panel pokazuje „Błędne hasło WebSocket”

Hasło w panelu musi być identyczne z polem `Password` w WebSocket Server.
Sprawdź wielkie litery, spacje i układ klawiatury.

### Panel w ogóle się nie otwiera

Sprawdź:

1. HTTP Server jest uruchomiony.
2. Host to `127.0.0.1`.
3. Port to `7474`.
4. Mapping `quiz` wskazuje aktualny folder.
5. Otwierasz `http://`, a nie plik `file://`.
6. Adres kończy się `panel.html?v=130`.

### Panel się otwiera, ale nie łączy

Sprawdź:

1. WebSocket Server jest uruchomiony.
2. Host to `127.0.0.1`.
3. Port to `8080`.
4. Endpoint to `/`.
5. `Authentication` i `Enforce` są włączone.
6. Portu `8080` nie używa inny program.

### Panel łączy się, ale przyciski nic nie robią

1. Akcja nazywa się dokładnie `QUIZ - Silnik`.
2. Akcja jest włączona.
3. `QuizEngine.cs` został wklejony w całości.
4. Użyto **Find Refs** i **Compile**.
5. W edytorze C# nie ma czerwonych błędów.

### `!1` nie oddaje głosu

1. Do akcji dodano trigger `Twitch > Chat > Message`.
2. Twitch jest połączony.
3. Runda jest otwarta.
4. Wiadomość zawiera wyłącznie `!1`, bez dodatkowego tekstu.
5. Numer istnieje w bieżącym pytaniu.
6. Wiadomość nie została wysłana wewnętrznie przez konto bota.

### `!punkty`, `!ranking` lub `!quiz` nie odpowiada

1. Trigger wiadomości prowadzi do akcji `QUIZ - Silnik`.
2. Konto bota albo broadcastera jest połączone.
3. Komenda zgadza się z wartościami w `QuizEngine.cs`.
4. `!quiz` jest używane przez broadcastera albo moderatora.

### Overlay się nie pojawia

1. Rozpocznij pytanie - bez rundy overlay jest przezroczysty.
2. Użyj URL wygenerowanego przyciskiem **Skopiuj URL overlayu**.
3. Ustaw źródło na 1920×1080.
4. Sprawdź mapping `quiz`.
5. Otwórz podgląd z `?demo=1&v=130`.
6. Odśwież pamięć podręczną źródła w OBS.

### Overlay pokazuje błąd hasła

Połącz panel ponownie, kliknij **Skopiuj URL overlayu** i podmień cały adres
w OBS. Nie dopisuj hasła ręcznie.

### Po zmianie hasła panel działa, ale overlay nie

OBS nadal przechowuje stary URL. Wygeneruj nowy adres w panelu i wklej go do
źródła przeglądarkowego.

### Ranking zniknął

1. Sprawdź trwałą zmienną `localQuiz_scores_v1` w Global Variables.
2. Upewnij się, że nie uruchomiono resetu.
3. Użyj **Cofnij ostatni import**, jeśli problem wystąpił po imporcie.
4. Zaimportuj ostatnią kopię CSV w trybie **Zastąp cały ranking**.
5. Sprawdź kopie zapasowe danych Streamer.bot.


### Twitch jest rozłączony

Logowanie WebSocket nie loguje do Twitcha i nie zmienia tokenów Twitch.
Sprawdź osobno:

```text
Platforms > Twitch > Accounts
```

Panel może połączyć się ze Streamer.bot mimo rozłączonego Twitcha, ale głosy
z czatu wymagają działającej integracji Twitch.

## Źródła techniczne

- [Konfiguracja WebSocket Server](https://docs.streamer.bot/api/websocket/guide/configuration)
- [Uwierzytelnianie WebSocket](https://docs.streamer.bot/api/websocket/guide/authentication)
- [Żądania WebSocket i DoAction](https://docs.streamer.bot/api/websocket/requests)
- [Konfiguracja HTTP Server](https://docs.streamer.bot/api/http/guide/configuration)
- [C# w Streamer.bot: Find Refs i Compile](https://docs.streamer.bot/api/csharp/guide/intro)
- [Trigger Twitch Chat Message](https://docs.streamer.bot/api/triggers/twitch/chat/message)

## Licencja

Projekt jest udostępniony na licencji [MIT](LICENSE). Możesz go używać,
modyfikować i rozpowszechniać zgodnie z jej warunkami.


## Ważne informacje

**Kod nie zawiera tokenów ani danych prywatnych. Działa na kontach zalogowanych lokalnie w Streamer.bot u danej osoby.**

## Zastrzeżenie

Projekt jest udostępniany “tak jak jest”, bez gwarancji działania. Licencja MIT, więcej informacji poniżej.
Używasz go na własną odpowiedzialność. Nie odpowiadam za problemy wynikające z błędnej konfiguracji, aktualizacji Streamer.bot, zmian API platform ani ograniczeń po stronie Twitch/YouTube/Kick.

## Nieoficjalny projekt

Ten projekt nie jest oficjalnym narzędziem Streamer.bot, Twitch, YouTube ani Kick.  
Nie jestem właścicielem ani przedstawicielem żadnej z tych platform.

Streamer.bot, Twitch, YouTube i Kick są znakami/nazwami należącymi do ich właścicieli.  
Projekt korzysta jedynie z funkcji dostępnych w Streamer.bot i jest udostępniany jako niezależne narzędzie społecznościowe.

## Licencja

MIT License

Copyright (c) 2026 Lukituki6

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
