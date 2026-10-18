# Historia zmian - LukiQuizEngine

## 2.0.2 BETA - archiwa rankingów i poprawki stabilności

Wersja 2.0.2 rozbudowuje system V2 o lokalne archiwa rankingów i możliwość przywracania wyników. Wprowadza również poprawki bezpieczeństwa ścieżek plików, integralności archiwów, diagnostyki połączenia oraz obsługi błędów automatycznego rozliczenia.

### Nowe możliwości

- **Automatyczne archiwizowanie rankingu przed resetem**. Jeżeli ranking zawiera dane, silnik zapisuje i weryfikuje archiwum przed wyzerowaniem punktacji. Błąd wymaganej archiwizacji przerywa reset.
- **Ręczny zapis rankingu** bez resetowania punktów i bez zmiany epoki. Można nadać archiwum własną nazwę.
- **Przeglądanie archiwów w panelu**. Lista archiwów, wyszukiwanie, sortowanie, podgląd historycznych wyników oraz rozróżnienie zapisów automatycznych i ręcznych.
- **Przywracanie historycznego rankingu** po potwierdzeniu `PRZYWROC`. Przed przywróceniem silnik tworzy i weryfikuje kopię bezpieczeństwa bieżących wyników.
- **Przycisk „Otwórz folder archiwów”** dostępny z poziomu zakładki Ranking.
- Funkcje archiwizacji dostępne w panelu zarówno w trybie **PROSTY**, jak i **ADVANCED**.
- Domyślne położenie archiwów w Windows: `%LOCALAPPDATA%\LukiQuizEngine\RankingArchives`.

### Naprawy i odporność na błędy

- **Poprawiono obsługę błędu automatycznego rozliczania rundy**. Nieudana finalizacja, np. z powodu przekroczenia limitu punktów lub rozmiaru zapisu, nie powinna zapętlać błędu i blokować diagnostyki oraz świadomego anulowania problematycznej rundy. Silnik zachowuje poprzedni prawidłowy stan i udostępnia informację diagnostyczną `autoFinalizeError`.
- **Ujednolicono kontrole bezpiecznych ścieżek** dla operacji archiwizacji, w tym automatycznego resetu, katalogów pomocniczych i plików kontrolnych. Rozszerzono kontrolę dowiązań typu `ReparsePoint` w Windows.
- **Dodano `ContentHash` do nowych automatycznych archiwów** i odpowiednią weryfikację przy odczycie. Starsze archiwa bez tego pola pozostają obsługiwane dla zgodności.
- **Doprecyzowano diagnostykę Authentication i Enforce**. Niejednoznaczne zamknięcie połączenia nie jest już przedstawiane jako pewny dowód skutecznego Enforce. Komunikaty rozróżniają potwierdzone połączenie od niezweryfikowanej konfiguracji nasłuchu.
- **Poprawiono obsługę przerwanego ręcznego zapisu archiwum**. Bezpieczne wznowienie jest możliwe, gdy istniejący stan i zapisane dane kontrolne pozwalają odtworzyć identyczne archiwum. W przeciwnym razie operacja jest odrzucana bez przypisywania nowych danych do historycznego zapisu.
- Poprawiono obsługę potwierdzenia operacji otwierania folderu archiwów w komunikacji panelu i silnika.
- Zachowano zabezpieczenia przed podwójnym resetem, ponowionym przywracaniem oraz konfliktami identyfikatorów operacji.

### Zgodność i identyfikatory

- Wersja aplikacji: **2.0.2 BETA**.
- Identyfikator buildu: **`PHoeniX`** (wcześniej `FC1`).
- Identyfikator poprawki: **`QUPA`** (wcześniej `STYLE4`).
- Zachowano istniejące zmienne danych V2, w tym `localQuiz_data_v2`, oraz zgodność formatów archiwów poprzednich buildów.
- Nie jest potrzebny oddzielny serwer, Node.js ani baza danych.

### Znane ograniczenia i uwagi

Wbudowany HTTP Server Streamer.bot udostępnia `/DoAction`, którego nie zabezpiecza automatycznie uwierzytelnienie WebSocket. Projekt przeznaczony jest do lokalnego użytkowania z HTTP i WebSocket przypisanymi do `127.0.0.1`, włączonymi `Authentication` i `Enforce` oraz bez udostępniania portów do sieci. Szczegóły w [README.md](README.md#bezpieczeństwo-i-znane-ograniczenia).
- Nie jest gwarantowana pełna odporność na jednoczesną awarię procesu lub zasilania podczas zapisu do magazynu Streamer.bot i plików archiwów.
- Automatyczne czyszczenie starych archiwów nie jest częścią tej aktualizacji. Użytkownik powinien kontrolować wolne miejsce na dysku.
- Archiwum rankingu nie zastępuje pełnego backupu ustawień, szablonów i pozostałych danych quizu.
- Testy kodu i JavaScript nie zastępują sprawdzenia konkretnej instalacji Streamer.bot, połączenia Twitch i obrazu w OBS. 

### Aktualizacja z 2.0.0 / 2.0.1 BETA

1. Przed aktualizacją wykonaj eksport rankingu i pełny backup.
2. Zastąp kod w Action **QUIZ - Silnik** aktualnym `QuizEngine.cs`. Użyj **Find Refs** i **Save and Compile**.
3. Podmień oba pliki `panel.html` i `overlay.html` z tego samego wydania.
4. Zachowaj dane globalne V2 i istniejące archiwa. Nie usuwaj wcześniejszych danych migracji.
5. Odśwież stronę panelu i źródło OBS. Sprawdź działanie quizu oraz archiwizacji na danych testowych.

---
## 2.0.1 BETA - mała aktualizacja QoL

### Mała zmiana QoL

- Dodano opcję widoku panelu **"Prosty"**, który przypomina mniej więcej widok z wersji 1.3.0 zachowując kluczowe zmiany z 2.0.0
- Oraz **"Advanced"**, który przedstawia wszystkie funkcje 

### Migracja z wersji 2.0.0 BETA

Jeśli zainstalowana była już wersja 2.0.0 BETA to najszybciej jest podmienić po prostu plik w folderze zmapowanym pod bota: **panel.html** 

---
## 2.0.0 BETA - duża aktualizacja

Wersja 2.0.0 przebudowuje quiz praktycznie od podstaw. Nadal wystarczy Streamer.bot, jedna Action z kodem C#, jeden trigger wiadomości Twitcha oraz lokalny panel i overlay HTML. Nie trzeba instalować osobnej aplikacji, Node.js ani zewnętrznej bazy danych.

### Quiz i głosowanie

- Pytania mogą mieć od 2 do 12 odpowiedzi, w tym kilka poprawnych. Każda odpowiedź może mieć własną liczbę punktów.
- Rozbudowano punktację: tryby `adjacent`, `manual`, `none` i `custom`, punkty częściowe oraz opcjonalny bonus za szybką poprawną odpowiedź.
- Dodano losowanie kolejności odpowiedzi bez zmiany tego, która odpowiedź jest poprawna i ile jest warta.
- Dostępne są trzy zasady głosowania: ostatni głos (`LAST`), pierwszy głos (`FIRST`) oraz ograniczona liczba zmian (`LIMITED`).
- Można wybrać, co widzowie widzą przed ujawnieniem wyników: pełny rozkład głosów (`LIVE`), samą ich liczbę (`COUNT_ONLY`) albo ukryte wyniki (`HIDDEN`).
- W panelu można sprawdzić, kto zagłosował na daną odpowiedź, a także ręcznie zmienić lub usunąć głos przed ujawnieniem wyniku.
- Dodano tryb ankiety (`POLL`), który zbiera głosy, ale nie przyznaje punktów do rankingu quizu.

### Rundy, czas i wyniki

- Dodano historię rund z oddanymi głosami i przyznanymi punktami.
- Można cofnąć ostatni kwalifikujący się wynik lub przeliczyć zachowaną rundę po poprawieniu odpowiedzi albo punktacji.
- Timer działa po stronie silnika. Obsługuje pauzę, wznowienie, dodawanie i odejmowanie czasu oraz opcjonalne automatyczne ujawnienie wyniku.
- Po ponownym połączeniu panel i overlay pobierają aktualny stan zamiast zaczynać rundę od nowa.
- Można ustawić cooldowny komend `!punkty` i `!ranking`. Moderatorzy i właściciel kanału są z nich zwolnieni.
- Komendy `!1` do `!12`, `!punkty`, `!ranking` oraz moderatorskie `!quiz` pozostają dostępne.

### Pytania i zestawy

- Zapisane pytania można edytować, powielać i wykorzystywać w wielu zestawach.
- Zestawy działają jak playlisty: można zmieniać kolejność pytań, losować ją i przechodzić do następnego lub poprzedniego pytania.
- Szkic pytania można zapisać w silniku, a następnie wczytać w panelu.
- Dodano import i eksport pytań oraz zestawów w formacie JSON z podglądem i walidacją.
- Przewidziano migrację starych szablonów i szkicu zapisanych w przeglądarce, o ile są nadal dostępne pod tym samym adresem lokalnym.

### Drużyny, panel i OBS

- Dodano tryb drużynowy z przypisywaniem widzów i osobnym naliczaniem punktów drużyn.
- Rozbudowano lokalny panel o sterowanie rundą, historię, ranking, ustawienia i diagnostykę instalacji.
- Dodano konfigurowalne skróty klawiaturowe, wykrywanie konfliktów oraz blokowanie skrótów podczas wpisywania tekstu.
- Overlay oferuje układy `FULL`, `COMPACT`, `MINIMAL` i `NO_LEADERBOARD`, a także ustawienia położenia, skali, kolorów, rankingu, procentów i timera.
- Dodano opcjonalne animacje, proste dźwięki generowane lokalnie oraz podium TOP 3.
- Podgląd demonstracyjny overlayu działa bez uruchamiania quizu.

### Dane, migracja i bezpieczeństwo

- Dane quizu są przechowywane w trwałym stanie V2 po stronie Streamer.bot. Silnik waliduje je przed zapisem i odczytem.
- Dodano pełny backup stanu, przywracanie kopii i maksymalnie pięć automatycznych snapshotów. CSV nadal obejmuje sam ranking, a nie cały quiz.
- Zmiany punktów przez import CSV, reset i korektę ręczną oddzielają starszą historię od kolejnych przeliczeń. W razie potrzeby trzeba przywrócić odpowiedni backup lub snapshot.
- Migracja z 1.3.0 zachowuje stare zmienne, przed utworzeniem stanu V2 zapisywana jest osobna kopia danych migracyjnych.
- Panel i overlay korzystają z lokalnego HTTP i WebSocket z wymaganym `Authentication` oraz `Enforce`. Pozostawiono kontrole wersji komponentów i ograniczenia rozmiaru importowanych danych.
- Hasło WebSocket nie jest wpisane na stałe do kodu ani dodawane do backupu. **Wygenerowany URL overlayu zawiera jednak hasło i trzeba traktować go jako poufny.**

### Ważne przed instalacją

- To **wydanie BETA**. Przed wykorzystaniem na transmisji warto przetestować je na swoim Streamer.bot, Twitchu i OBS. Testy w samym panelu nie zastępują sprawdzenia głosowania na prawdziwym czacie ani obrazu i dźwięku w OBS.
- Automatyczne komunikaty o rozpoczęciu i zakończeniu rundy są **domyślnie wyłączone** (`AnnounceRoundEventsInChat = false`). Można je włączyć w `QuizEngine.cs`.
- Aktualna wersja nie prowadzi statystyk serii poprawnych odpowiedzi (`current/best streak`). Starsze pola tych statystyk są usuwane podczas odczytu zgodnych danych.
- Cofanie i przeliczanie działa tylko dla odpowiednich rund zachowanych w bieżącej epoce punktacji. Cofnięcie do 1.3.0 nie przenosi automatycznie wyników zdobytych w 2.0.0.
- Instrukcja przejścia ze starszej wersji jest w [MIGRATION.md](MIGRATION.md).
