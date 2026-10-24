# Historia zmian - LukiQuizEngine

## 2.2.0 - Quality of Life & Competitive Update (STABLE)

Wersja 2.2.0 rozszerza LukiQuizEngine o nowe narzędzia do prowadzenia quizów, opcjonalny system rywalizacji, dodatkową personalizację komend Twitch oraz usprawnienia interfejsu.

### Nowości - Quality of Life

- **Pasek LIVE** - kompaktowe sterowanie aktywną rundą, podgląd czasu, głosów i stanu głosowania.
- **Podsumowanie rundy** - statystyki uczestników, poprawnych odpowiedzi, skuteczności i przyznanych punktów na podstawie zapisanej historii.
- **Ochrona podczas transmisji** - opcjonalne potwierdzenia ryzykownych operacji w panelu ADVANCED, ograniczające przypadkowe kliknięcia.
- **Presety ustawień pytania** - zapisywanie i wczytywanie konfiguracji timera, głosowania, punktacji oraz trybu rundy. Dodano gotowe przykłady presetów i możliwość tworzenia własnych.
- **Przygotowanie następnego pytania** - wczytywanie kolejnego pytania z zestawu bez automatycznego rozpoczynania głosowania.
- **Wyszukiwanie w Ustawieniach i Pomocy** - dwie niezależne wyszukiwarki obsługujące język polski i angielski, wiele słów oraz wyszukiwanie bez rozróżniania wielkości liter i polskich znaków.
- **Przejrzystsze ustawienia** - cztery rozwijane kategorie ustawień, poprawiony układ skrótów klawiaturowych i rozbudowana Pomoc PL/EN.

### Nowości - Streak

- Dodano opcjonalny system serii poprawnych odpowiedzi, domyślnie wyłączony.
- System zapisuje aktualną serię, najlepszą serię i liczbę poprawnych odpowiedzi.
- Dodano konfigurowalne progi bonusowych punktów za serie.
- Domyślne bonusy punktowe wynoszą 0.
- Bonusy respektują istniejące limity punktów.
- System współpracuje z rozliczaniem rund, historią oraz cofaniem i przeliczaniem wyników.

### Nowości - Osiągnięcia

**Zbudowałem na razie samą podstawę, która działa, ale w przyszłości zostanie bardziej rozbudowana o dodatkowe funkcje.**

Dodano opcjonalny system osiągnięć, domyślnie wyłączony.

Dostępnych jest 9 wbudowanych osiągnięć za:

- Pierwszą poprawną odpowiedź.
- 10, 25 i 50 poprawnych odpowiedzi.
- Serie 3, 5 i 10 poprawnych odpowiedzi.
- Udział w 10 i 50 rundach.

Osiągnięcia zapisują daty zdobycia i nie przyznają dodatkowych punktów. Osiągnięcia związane z seriami wymagają również aktywnego systemu Streak.

Wyłączenie systemu nie usuwa zachowanego postępu.

### Nowości - Komendy Twitch

- Dodano możliwość zmiany nazw komend `!punkty`, `!ranking` i prefiksu administracyjnego `!quiz` bez edycji kodu.
- Zachowano dotychczasowe domyślne nazwy i mechanizmy cooldownów.
- Dodano sprawdzanie poprawności i kolizji nazw komend.
- Dodano opcjonalne komunikaty o rundzie na czacie Twitch, konfigurowane w ADVANCED i domyślnie wyłączone.
- Komunikaty obejmują rozpoczęcie, ręczne zamknięcie i ponowne otwarcie głosowania oraz ujawnienie wyniku.
- Komunikat wyniku jest wysyłany po zatwierdzonym zapisie stanu.
- Ulepszono format odpowiedzi `!punkty` i `!ranking`. Poszczególne elementy oraz pozycje rankingu są oddzielane znakiem `|`, zamiast dotychczasowych myślników pełniących funkcję separatorów.

### Interfejs i zgodność

- Zachowano tryby PROSTY, ADVANCED i LEGACY.
- Nowe narzędzia operacyjne i ustawienia rywalizacji udostępniono w ADVANCED.
- Ujednolicono nazwę LukiQuizEngine w nagłówkach wszystkich trybów panelu.
- Zachowano istniejącą nawigację oraz wspólny silnik quizu.
- Rozbudowano tłumaczenia PL/EN i dokumentację Pomocy.
- Zachowano zgodny odczyt starszych danych i ustawień z 2.1.0 BETA.
- Zachowano `localQuiz_data_v2`, dotychczasowe schematy głównego stanu i backupów, identyfikatory `PHoeniX` i `QUPA`, jedną Action C# i jeden trigger czatu Twitch.
- Nowe dane Streak i Osiągnięć są uwzględniane w pełnych backupach. Przywracanie sprzecznych stanów rywalizacji podlega dodatkowej kontroli bezpieczeństwa.

### Bezpieczeństwo i testy

- Zachowano ograniczenia zapisu, walidację danych i dotychczasowe mechanizmy ochrony historii.
- Znane ograniczenie HTTP `/DoAction` pozostaje aktualne. Zalecana jest konfiguracja wyłącznie lokalna na `127.0.0.1`, bez udostępniania serwerów do sieci.


## 2.1.0 BETA - Legacy, PL/ENG, ulepszenia interfejsu i nowe narzędzia

Wersja 2.1.0 BETA rozszerza LukiQuizEngine o trzeci tryb panelu, dwujęzyczny interfejs, bardziej czytelne sterowanie quizem, rozbudowaną historię rund, zarządzanie usuwaniem archiwów oraz lokalny symulator.

Aktualizacja zachowuje architekturę V2. Nadal wystarczą Streamer.bot, jedna Action C#, jeden trigger czatu Twitch, lokalny panel WWW i overlay OBS.

### Nowości - wygląd i obsługa panelu

- Dodano trzeci tryb interfejsu `LEGACY`, inspirowany wyglądem LukiQuizEngine 1.3.0.
- Tryb LEGACY wykorzystuje wspólny silnik V2 i obsługuje nowe funkcje, w tym historię rund oraz archiwa rankingów.
- Ulepszono wygląd trybów `PROSTY` i `ADVANCED` bez przebudowy istniejącej nawigacji.
- Wprowadzono bardziej zróżnicowane kolory przycisków zależnie od rodzaju wykonywanej akcji.
- Poprawiono wizualne wyróżnianie aktywnych elementów, zaznaczeń i stanów interfejsu.
- Usprawniono czytelność przycisków, sterowania rundą i sekcji panelu.
- Zachowano możliwość przełączania widoków bez resetowania danych quizu.
- Wybór trybu panelu może zostać zapamiętany lokalnie przez przeglądarkę.

### Nowości - język polski i angielski

- Dodano obsługę języka polskiego i angielskiego w panelu WWW.
- Dodano angielskie tłumaczenia interfejsu overlayu OBS.
- Wprowadzono przełącznik `PL / EN`.
- Dodano możliwość synchronizacji języka panelu i overlayu.
- Umożliwiono również niezależne ustawienie języka overlayu.
- Rozszerzono tłumaczenia komunikatów, przycisków, opisów funkcji oraz elementów dynamicznych.

### Nowości - rozpoznawanie głosów Twitch

Dodano trzy konfigurowalne sposoby rozpoznawania wiadomości zawierających głos.

- `STRICT` - akceptowane są wyłącznie dokładne komendy, np. `!1`.
- `WHITESPACE` - akceptowane są komendy z dodatkowym tekstem oddzielonym białym znakiem, np. `!1 wiadomość`.
- `ATTACHED` - akceptowane są również wiadomości bez spacji po numerze, np. `!1wiadomość`.

Zachowano obsługę odpowiedzi od `!1` do `!12`, zależnie od liczby odpowiedzi w pytaniu.

Dodano kontrolę niektórych niewidocznych i nieprawidłowych znaków w wiadomościach, aby ograniczyć błędne interpretowanie głosów.

Domyślnym wariantem pozostaje `STRICT`.

### Nowości - historia rund

- Rozbudowano narzędzia przeszukiwania historii rund.
- Dodano filtrowanie według treści pytania i widza.
- Dodano filtrowanie według statusu rundy oraz stanu rozliczenia punktów.
- Dodano możliwość ograniczania wyników według dat.
- Dodano opcje sortowania i stronicowania historii.
- Zachowano dotychczasowe mechanizmy cofania i przeliczania kwalifikujących się wyników.

### Nowości - archiwa rankingów

- Dodano ręczne usuwanie wybranych archiwów rankingów.
- Dodano podgląd planowanej operacji usuwania przed jej zatwierdzeniem.
- Wprowadzono wymagane potwierdzenie `USUN ARCHIWA`.
- Dodano opcjonalne automatyczne czyszczenie starszych archiwów.
- Umożliwiono skonfigurowanie maksymalnej liczby archiwów oraz maksymalnego wieku zapisów.
- Dodano osobne opcje uwzględniania archiwów ręcznych i bezpieczeństwa podczas automatycznego czyszczenia.
- Zachowano mechanizmy ochrony najnowszych kopii i archiwów powiązanych z niedokończonymi operacjami.
- Przed usunięciem silnik ponownie weryfikuje wybrane pliki.
- Rozbudowano przeglądanie archiwów o dodatkowe informacje, filtrowanie i sortowanie.

Automatyczne czyszczenie jest domyślnie wyłączone. Nie działa jako niezależna usługa w tle. Jest wywoływane przy określonych operacjach zarządzania rankingiem, takich jak reset i ręczny zapis archiwum.

### Nowości - lokalny symulator quizu

- Dodano symulator dostępny w trybie ADVANCED.
- Umożliwiono testowanie rund bez przeprowadzania prawdziwego głosowania na Twitchu.
- Dodano generowanie fikcyjnych widzów oraz symulowanie oddawania i zmieniania głosów.
- Umożliwiono testowanie zasad głosowania, punktacji i timera.
- Dodano symulowanie zamykania, ponownego otwierania, anulowania oraz rozliczania rund.
- Dodano testowanie nowych sposobów rozpoznawania komend głosowania.
- Symulator korzysta z odrębnych danych przechowywanych w pamięci strony.

Symulator nie zmienia prawdziwego rankingu, historii rund ani archiwów. Nie zastępuje testów integracji ze Streamer.bot, Twitchem i OBS.

### Zgodność i migracja

- Zaktualizowano oznaczenie wersji aplikacji do `2.1.0 BETA`.
- Zachowano identyfikator buildu `PHoeniX`.
- Zachowano identyfikator poprawki `QUPA`.
- Zachowano nazwy głównych zmiennych danych V2, w tym `localQuiz_data_v2`.
- Dodano obsługę nowych ustawień z zachowaniem wartości domyślnych dla starszych zapisów.
- Zachowano możliwość odczytywania obsługiwanych archiwów wcześniejszych wydań.
- Rozbudowano konsolidację starszych szablonów pytań ze wspólnym bankiem pytań V2.
- Nie dodano wymogu instalacji Node.js, Pythona, osobnej bazy danych ani zewnętrznego serwera.

### Znane ograniczenia i uwagi

- Wersja 2.1.0 BETA pozostaje wydaniem testowym.
- Wbudowany HTTP Server Streamer.bot nadal udostępnia `/DoAction`, którego nie zabezpiecza automatycznie uwierzytelnianie WebSocket.
- Należy korzystać z lokalnej konfiguracji HTTP i WebSocket na `127.0.0.1`, z włączonymi `Authentication` i `Enforce`, bez udostępniania portów do sieci.
- Usuwanie archiwów jest operacją destrukcyjną. Przed pierwszym użyciem zalecane jest wykonanie osobnego backupu.
- Nie jest gwarantowana pełna odporność na nagłą awarię procesu lub zasilania podczas operacji na plikach i trwałym magazynie Streamer.bot.
- Symulator nie jest zamiennikiem rzeczywistych testów integracyjnych.

### Aktualizacja z 2.0.2 BETA do 2.1.0 BETA

1. Zakończ albo anuluj aktywną rundę.
2. Wyeksportuj pełny backup oraz zachowaj dotychczasowy ranking i archiwa.
3. Zastąp kod w Action `QUIZ - Silnik` nową zawartością `QuizEngine.cs`.
4. Wykonaj `Find Refs`, a następnie `Save and Compile`.
5. Podmień pliki `panel.html` i `overlay.html` na pliki z wydania 2.1.0 BETA.
6. Odśwież panel w przeglądarce oraz źródło przeglądarkowe w OBS.
7. Upewnij się, że panel i silnik używają wersji `2.1.0` oraz identyfikatorów `PHoeniX` i `QUPA`.
8. Sprawdź istniejący ranking, historię rund, szablony i archiwa.
9. Przetestuj przełączanie trybów, języków oraz działanie głosowania.
10. Przed aktywowaniem automatycznego czyszczenia archiwów wykonaj osobną kopię bezpieczeństwa.

Nie usuwaj istniejących zmiennych danych V2 ani wcześniejszych archiwów rankingów.

---

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
