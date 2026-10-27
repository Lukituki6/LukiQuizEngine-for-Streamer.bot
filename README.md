> [!NOTE]
> Projekt nie jest powiązany ze Streamer.bot, Twitchem, YouTube ani Kick, nie jest przez nie sponsorowany ani oficjalnie wspierany. Streamer.bot, Twitch, YouTube i Kick są znakami towarowymi lub nazwami należącymi do ich odpowiednich właścicieli. Projekt wykorzystuje wyłącznie funkcje dostępne w Streamer.bot i jest udostępniany jako niezależne narzędzie społecznościowe.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# LukiQuizEngine for Streamer.bot

**Aktualne wydanie: v2.2.0 STABLE**

**🌐 Język / Language:** [🇵🇱 Polski](README.md) | [🇬🇧 English](README_EN.md)

- [READMElegacy.md](1.3.0/READMElegacy.md) - archiwalna dokumentacja starszej wersji **LukiQuizEngine v1.3.0**, dostępna w folderze [1.3.0](1.3.0/). Zawiera oryginalną instrukcję instalacji, konfiguracji i obsługi. Informacje oraz przykłady kodu dotyczą wersji 1.3.0 i nie powinny być stosowane bezpośrednio w aktualnej wersji 2.2.0.

LukiQuizEngine to lokalny silnik quizowy dla Streamer.bot z panelem WWW, overlayem OBS, głosowaniem na czacie Twitch, historią rund i trwałym rankingiem.

Do działania wystarczy **jedna Action C# i jeden trigger wiadomości Twitch**. Nie potrzebujesz Node.js, Pythona, osobnej aplikacji `.exe`, zewnętrznej bazy danych ani hostingu.

> [!IMPORTANT]
> Wersja **2.2.0 STABLE** dodaje nowe narzędzia operatora, opcjonalne systemy rywalizacji oraz dodatkowe ustawienia komend Twitch. Streak i Osiągnięcia są domyślnie wyłączone. Przed aktualizacją wykonaj kopię istniejących danych i sprawdź instalację. Wydanie stabilne nie oznacza całkowitego braku błędów ani ograniczeń. Testy izolowane nie zastępują testów prawdziwego Streamer.bot i czatu Twitch. Znane ograniczenie bezpieczeństwa HTTP `/DoAction` zostało opisane w sekcji [Bezpieczeństwo i znane ograniczenia](#bezpieczeństwo-i-znane-ograniczenia).

## Spis treści

- [Co potrafi](#co-potrafi)
- [Quality of Life i rywalizacja w 2.2.0](#quality-of-life-i-rywalizacja-w-220)
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
- **Pasek LIVE** ułatwiający sterowanie aktywną rundą w trybie ADVANCED.
- **Podsumowanie rundy** zawierające statystyki uczestników, poprawnych odpowiedzi i przyznanych punktów.
- **Przygotowanie następnego pytania** bez automatycznego rozpoczynania głosowania.
- Opcjonalne komunikaty o wydarzeniach rundy na czacie Twitch.

### Panel i interfejs

- Trzy tryby interfejsu: `PROSTY`, `ADVANCED` i `LEGACY`.
- Ulepszony wygląd przycisków, wizualne oznaczanie aktywnych elementów i czytelniejsze sterowanie.
- Tryb Legacy inspirowany wyglądem LukiQuizEngine 1.3.0.
- Obsługa języka polskiego i angielskiego w panelu oraz overlayu.
- Opcjonalna synchronizacja języka panelu i overlayu.
- Wspólny silnik quizu i te same dane we wszystkich trybach panelu.
- **Wyszukiwanie w Ustawieniach i Pomocy** z obsługą kilku słów oraz wyszukiwaniem bez rozróżniania wielkości liter i polskich znaków.
- Cztery rozwijane kategorie ustawień i usprawniony układ skrótów klawiaturowych.
- **Ochrona podczas transmisji** - opcjonalne potwierdzenia ryzykownych operacji w ADVANCED.
- **Presety ustawień pytania** - zapisywanie i wczytywanie wybranych konfiguracji quizu.

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

### Streak i osiągnięcia

- Opcjonalny system serii poprawnych odpowiedzi, domyślnie wyłączony.
- Zapisywanie aktualnej serii, najlepszego wyniku serii i liczby poprawnych odpowiedzi.
- Konfigurowalne bonusy punktowe za osiągnięcie określonych progów serii.
- Opcjonalny system 9 wbudowanych osiągnięć, domyślnie wyłączony.
- Podgląd zdobytych osiągnięć, warunków ich zdobycia i zapisanych dat w panelu ADVANCED.
- Zachowanie istniejącego postępu po wyłączeniu systemów.


### Szczegółowa instrukcja komend czatu

LukiQuizEngine pozwala widzom głosować i sprawdzać swoje wyniki bezpośrednio na czacie Twitch. Nadawca oraz moderatorzy mogą dodatkowo zarządzać rundami quizu za pomocą komend administracyjnych.

Wszystkie poniższe przykłady korzystają z domyślnych nazw komend.

#### Komendy dla widzów

| Komenda | Działanie |
| --- | --- |
| `!1` do `!12` | Oddaje głos na odpowiedź o wskazanym numerze, jeśli jest dostępna w aktualnej rundzie. |
| `!punkty` | Wyświetla własną liczbę punktów oraz statystyki poprawnych i wszystkich odpowiedzi. |
| `!ranking` | Wyświetla TOP 5 uczestników z największą liczbą punktów na czacie Twitch. |

**Ważne informacje:**

- Głosy można oddawać wyłącznie podczas otwartego głosowania.
- Maksymalna liczba odpowiedzi w pytaniu wynosi 12.
- Możliwość zmiany głosu zależy od wybranej polityki głosowania (`LAST`, `FIRST` lub `LIMITED`).
- Domyślnie obowiązuje tryb `STRICT`, wymagający samodzielnej komendy, np. `!3`. Pozostałe tryby opisano poniżej.
- Komendy `!punkty` i `!ranking` mogą podlegać indywidualnym i globalnym cooldownom.

#### Komendy administracyjne

Komendy administracyjne są dostępne wyłącznie dla właściciela kanału (broadcastera) oraz moderatorów Twitch.

| Komenda | Działanie |
| --- | --- |
| `!quiz` | Wyświetla skróconą pomoc dotyczącą komend administracyjnych. |
| `!quiz pomoc` | Wyświetla listę podstawowych komend i ich składnię. |
| `!quiz start Pytanie \| Odp. 1 \| Odp. 2` | Rozpoczyna nową rundę z podanym pytaniem i odpowiedziami, bez automatycznego timera. |
| `!quiz zamknij` | Zamyka głosowanie. Widzowie nie mogą już oddawać nowych głosów. |
| `!quiz wynik 3` | Ujawnia odpowiedź nr 3 jako poprawną i przyznaje 1 punkt za prawidłowy głos. |
| `!quiz wynik 3 5` | Ujawnia odpowiedź nr 3 i przyznaje 5 punktów za prawidłowy głos. |
| `!quiz wynik 7 3 1` | Przyznaje 3 punkty za odpowiedź nr 7 oraz po 1 punkcie za odpowiedzi sąsiednie, czyli nr 6 i 8. |
| `!quiz anuluj` | Anuluje rundę bez przyznawania punktów. |
| `!quiz pokaz` | Pokazuje aktualny quiz lub podium na overlayu OBS, jeśli są dostępne. |
| `!quiz ukryj` | Ukrywa aktualny quiz lub podium na overlayu OBS. |

#### Jak korzystać z komend? Przykład

Załóżmy, że chcesz przeprowadzić quiz z trzema odpowiedziami.

**1. Rozpoczęcie quizu**

`!quiz start Jaka jest stolica Polski? | Kraków | Warszawa | Gdańsk`

Silnik rozpocznie rundę z trzema odpowiedziami, bez timera.

**2. Głosowanie widzów**

Widzowie mogą wpisywać na czacie `!1`, `!2` lub `!3`.

**3. Zamknięcie głosowania**

`!quiz zamknij`

Po zamknięciu nie będą przyjmowane nowe głosy.

**4. Ujawnienie prawidłowej odpowiedzi**

`!quiz wynik 2 5`

Silnik wskaże Warszawę jako poprawną odpowiedź i przyzna po 5 punktów widzom, którzy prawidłowo zagłosowali.

**5. Sprawdzenie rankingu**

`!ranking`

Bot wyświetli pięciu najlepszych uczestników na czacie Twitch.

#### Dodatkowe informacje

- Składnia `!quiz wynik NUMER PUNKTY CZĘŚCIOWE` umożliwia przyznawanie punktów za odpowiedź poprawną i sąsiednie odpowiedzi. Jeśli nie podasz liczby punktów, domyślnie zostanie przyznany 1 punkt za poprawny głos i 0 za sąsiednie.
- Ręczne wybieranie dowolnych odpowiedzi częściowo poprawnych oraz bardziej zaawansowane ustawienia punktowania są dostępne w panelu WWW.
- Silnik rozpoznaje również wybrane alternatywne nazwy, np. `!quiz stop`, `!quiz help`, `!quiz cancel`, `!quiz show`, `!quiz hide` oraz `!quiz odpowiedz`.
- Komendy administracyjne nie omijają ograniczeń stanu rundy. Niektóre operacje wymagają rozpoczętego lub odpowiednio zakończonego głosowania.
- Domyślne nazwy `!punkty`, `!ranking` i `!quiz` można zmienić w ustawieniach ADVANCED. Po zmianie prefiksu administracyjnego wszystkie jego podkomendy używają nowej nazwy.
- Nie trzeba tworzyć osobnych komend w zakładce Commands Streamer.bot. Komendy są obsługiwane przez istniejącą akcję `QUIZ - Silnik` i jeden trigger `Twitch > Chat > Chat Message`.

## Quality of Life i rywalizacja w 2.2.0

Nowe narzędzia są dostępne w **ADVANCED**, w dotychczasowych sekcjach LIVE, edycji pytania, rankingu i ustawień. Lewa nawigacja nie otrzymała nowych kategorii. PROSTY i LEGACY zachowują zwykły ranking. Przełączenie widoku nie przerywa naliczania funkcji wcześniej włączonych w silniku.

### Narzędzia operatora

- **Pasek LIVE** pokazuje pytanie, stan rundy, czas pozostały według zegara silnika, końcowe zaakceptowane głosy i blokadę głosowania. Pauza, wznowienie, +10 s, -10 s, zamknięcie i ujawnienie korzystają ze wspólnego silnika. Niedostępne operacje są wyłączone; powtarzanie jest blokowane podczas oczekiwania na potwierdzenie. Po niepewnym wyniku odśwież stan.
- **Ochrona podczas transmisji** dodaje potwierdzenie przy ryzykownych operacjach, zachowując dotychczasowe wymagane potwierdzenia. Jest opcjonalna, zapamiętywana w tej przeglądarce i działa tylko w ADVANCED. To ochrona operatora przed pomyłką, nie uwierzytelnianie HTTP.
- **Podsumowanie rundy** odczytuje historię: uczestników, końcowe zaakceptowane głosy, poprawne odpowiedzi, procent poprawności, sumę przyznanych punktów i najwyższą nagrodę. Uwzględnia punkty częściowe i bonusy. Nie ma danych o wszystkich odrzuconych próbach ani pełnej pozycji rankingowej sprzed rundy; te metryki pokazują brak danych. POLL nie ma poprawności ani punktów. Anulowanie i cofnięcie nie pokazują przyznanych punktów; przeliczenie aktualizuje podsumowanie. Sam podgląd nie nalicza nagród.
- **Presety ustawień pytania** pozwalają zapisać nazwę i wybrane grupy: timer, głosowanie, punktację i tryb rundy. Dostępne są przykłady Szybki Quiz, Standardowy Quiz, Turniej i Ankieta. Możesz tworzyć, aktualizować i usuwać własne presety oraz zobaczyć różnice przed wczytaniem. Wczytanie zmienia tylko szkic, bez rozpoczynania rundy. Format 1 jest przechowywany w `localStorage` tej przeglądarki, maksymalnie 30 presetów; nie zawiera haseł, danych widzów, rankingów ani archiwów. Zachowaj ten sam host i port przy aktualizacji.
- **Przygotuj następne pytanie** używa istniejącej kolejki zestawu, w tym wcześniej wylosowanej kolejności. Przygotowuje szkic bez startu głosowania, ostrzega przed nadpisaniem zmienionego szkicu i rozpoznaje koniec zestawu. Wielokrotne kliknięcie nie pomija już przygotowanego pytania. Zwykły przycisk startu pozostaje świadomą decyzją operatora.
- **Wyszukiwanie w Ustawieniach i Pomocy** - dodano dwie niezależne wyszukiwarki. Ustawienia można przeszukiwać w ADVANCED, a Pomoc w trybach PROSTY i ADVANCED. Wyszukiwanie obsługuje kilka słów, ignoruje wielkość liter i polskie znaki. Pasujące sekcje rozwijają się automatycznie. Wyszukiwanie nie modyfikuje zapisanych ustawień ani nie wykonuje operacji silnika.
- **Usprawniony układ ustawień** - cztery rozwijane kategorie oraz czytelniejszy układ skrótów klawiaturowych dostosowujący się do szerokości ekranu. Zwijanie kategorii nie zmienia ustawień quizu.

### Włączanie nowych systemów

W ustawieniach ADVANCED użyj sekcji **Rywalizacja - opcjonalna**. Streak i Osiągnięcia mają **dwa niezależne przełączniki, domyślnie OFF**.

Zapisuj zmianę po zakończeniu lub anulowaniu bieżącej rundy. Nowa runda zapamiętuje stan przełączników i bonusów w chwili rozpoczęcia. Dawne rundy nie otrzymują nowych statystyk wstecz.

Wyłączenie wstrzymuje naliczanie, zachowując dane. Żaden przełącznik nie włącza innego.

### Streak i osiągnięcia

Streak zwiększa się o jeden za końcową odpowiedź oznaczoną jako poprawna w rozliczonym QUIZ. Zmieniony głos jest oceniany zgodnie z FIRST, LAST lub LIMITED. Wiele poprawnych opcji kwalifikuje się jednakowo. Sama punktacja częściowa bez poprawności nie kwalifikuje.

Błędna odpowiedź lub brak głosu w kwalifikującej się rundzie zeruje aktualną serię wcześniej śledzonego widza. POLL, anulowane i cofnięte rundy nie naliczają; przy wyłączonym Streak seria jest wstrzymana.

Zapisywane są aktualna seria, rekord i łączna liczba poprawnych odpowiedzi w systemie Streak. Progi 3, 5 i 10 mają domyślny bonus **0**.

Można ustawić do 20 unikalnych progów od 1 do 10000 i bonus od 0 do 1000. Bonus przyznawany jest przy osiągnięciu dokładnego progu; łączna nagroda rundy nie przekracza istniejącego limitu 2000 punktów na widza. Zachowane są również limity całego rankingu. Bonus jest częścią tego samego rozliczenia, trafia też do punktów drużyny.

Katalog obejmuje **9 wbudowanych osiągnięć**:

- Pierwsza poprawna odpowiedź.
- 10 poprawnych odpowiedzi.
- 25 poprawnych odpowiedzi.
- 50 poprawnych odpowiedzi.
- Seria 3 poprawnych odpowiedzi.
- Seria 5 poprawnych odpowiedzi.
- Seria 10 poprawnych odpowiedzi.
- Udział w 10 rundach.
- Udział w 50 rundach.

Panel pokazuje warunki, aktywność, liczbę zdobytych i datę dla wybranego widza. Osiągnięcia nie przyznają dodatkowych punktów.

Osiągnięcia serii wymagają obu systemów ON i liczą tylko kwalifikujący się okres aktywacji; samo włączenie nie odblokowuje wcześniejszego postępu. Przerwa w poprawnej serii nie może zostać połączona przez wyłączenie osiągnięć.

Cofanie i przeliczanie odtwarzają projekcję z zapisanych rozliczeń oraz bazy skompaktowanej historii. Mogą usunąć osiągnięcie, jeśli jego warunek przestał być spełniony.

Restart, odświeżenie panelu i ponowne ujawnienie nie przyznają drugi raz tej samej nagrody. Nowe statystyki nie są dawnymi polami streak z 1.3.0.

W wersji 2.2.0 osiągnięcia są wbudowane. Tworzenie własnych osiągnięć w panelu, osobna komenda `!odznaki` oraz import i eksport katalogu osiągnięć nie są jeszcze dostępne. **Jest to funkcja, która planuje dodać, więc taki mały spoiler czy coś xd**

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
11. W ADVANCED sprawdź pasek LIVE, podsumowanie rundy i przygotowanie następnego pytania.
12. Przetestuj wyszukiwanie w Ustawieniach i Pomocy oraz zapis własnych nazw komend.
13. Jeśli chcesz korzystać ze Streak lub Osiągnięć, wykonaj osobny test obu systemów przed rozpoczęciem transmisji.

## Tryby panelu

LukiQuizEngine 2.2.0 oferuje trzy tryby interfejsu.

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
- Paska LIVE i podsumowania rundy.
- Presetów ustawień pytania oraz przygotowania następnego pytania.
- Opcjonalnej Ochrony podczas transmisji.
- Ustawień Streak i Osiągnięć.
- Własnych nazw komend Twitch oraz komunikatów rundy na czacie.
- Wyszukiwania w Ustawieniach.

### LEGACY

Interfejs inspirowany oryginalnym wyglądem **LukiQuizEngine 1.3.0**.

Zachowuje znajomy styl wizualny, jednocześnie korzystając z nowoczesnego silnika V2.

Wybrane funkcje V2, w tym historia rund i archiwa rankingów, pozostają dostępne również w trybie Legacy.

### Wspólne dane i działanie

Wszystkie trzy tryby panelu wykorzystują **ten sam silnik quizowy, ranking i konfigurację**.

Przełączanie między nimi nie resetuje punktacji ani aktywnych ustawień quizu.

Interfejs oferuje również ulepszone rozróżnianie przycisków za pomocą kolorów, wyróżnianie aktywnych elementów i wizualne oznaczanie wybranych opcji.

Nowe narzędzia 2.2.0 są dostępne w ADVANCED. Jeżeli Streak lub Osiągnięcia zostały włączone, przejście do trybu PROSTY albo LEGACY nie wyłącza ich naliczania w silniku.

## Ustawienia języka

LukiQuizEngine 2.2.0 obsługuje:

- **Polski (PL)**
- **Angielski (EN)**

Język można zmienić bezpośrednio w panelu.

Overlay również obsługuje oba języki.

Język overlayu może być automatycznie synchronizowany z językiem panelu lub konfigurowany niezależnie.

Jeżeli synchronizacja jest włączona, overlay korzysta z języka wybranego w panelu.

Zmiana języka interfejsu nie resetuje punktów ani nie modyfikuje wyników głosowania.

**Ważne:** identyfikatory Action, wymagane teksty potwierdzeń oraz nazwy zmiennych wewnętrznych pozostają bez zmian niezależnie od wybranego języka.

Przełączenie PL/EN nie zmienia nazw komend. Domyślne nazwy poniżej można osobno skonfigurować w ADVANCED, w Ustawieniach:

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

Powyższe nazwy komend są wartościami domyślnymi.

### Własne nazwy komend czatu

W **ADVANCED -> Ustawienia -> Komendy czatu i głosowanie -> Własne komendy czatu** ustaw nazwę komendy własnego wyniku, rankingu i prefiks komend administracyjnych, a następnie kliknij **Zapisz ustawienia**.

Domyślne wartości to `!punkty`, `!ranking` i `!quiz`. Przykładowo możesz ustawić `!wynik`, `!topka` i `!q`.

Każda nazwa ma od 2 do 33 znaków: `!`, literę ASCII A-Z/a-z, a następnie litery, cyfry, `_` lub `-`. Trzy nazwy muszą różnić się bez względu na wielkość liter. Spacje, polskie znaki i prefiksy cyfr, takie jak `!1` lub `!1test`, są odrzucane, aby uniknąć kolizji z głosowaniem.

Głosy `!1`-`!12` pozostają bez zmian. Wybieraj nazwy nieużywane przez inne boty i Action, ponieważ panel nie sprawdza ich konfiguracji.

Po zatwierdzonym zapisie działają nowe nazwy; stare nie pozostają dodatkowymi aliasami.

Prefiks administracyjny zmienia się wraz z podkomendami: po ustawieniu `!q` używaj np. `!q pomoc`, `!q zamknij` i `!q wynik 1`.

Uprawnienia nadawcy/moderatorów, cooldowny wyniku i rankingu oraz punktowanie pozostają bez zmian. Pomoc pokazuje aktywne nazwy z silnika, a nie niezapisany szkic. Przywrócenie domyślnych nazw zmienia tylko trzy pola szkicu i nadal wymaga zapisania.

Nazwy są trwałymi ustawieniami, uwzględnianymi w pełnych backupach i snapshotach. Starszy stan bez nowych pól otrzymuje oryginalne nazwy. Starsze żądania zapisu ustawień, które pomijają te pola, zachowują ich bieżące wartości.

Zasady przywracania i cofania wersji opisano w [MIGRATION.md](MIGRATION.md).

Administracyjne komendy czatu podlegają uprawnieniom nadawcy i moderatorów.

### Format odpowiedzi na czacie

Komendy własnych punktów i rankingu używają separatora ` | `, aby wiadomości były czytelniejsze.

W `!punkty` oddziela on liczbę punktów od statystyk odpowiedzi, a w `!ranking` kolejne pozycje rankingu.

Przykładowy format:

```text
Lukituki6: 150 pkt | poprawne 18/25
```

```text
Quiz TOP 5: 1. Lukituki6 - 150 pkt | 2. John- 120 pkt | 3. Blond - 95 pkt
```

Myślnik między nazwą widza a liczbą punktów w rankingu pozostaje bez zmian.

Modyfikacja dotyczy wyłącznie wyglądu wiadomości i nie wpływa na punktację, kolejność rankingu ani cooldowny.

### Komunikaty rundy na czacie

W **ADVANCED -> Ustawienia -> Komendy czatu i głosowanie** przełącz **Komunikaty o rundzie na czacie** i kliknij **Zapisz ustawienia**.

Domyślnie opcja jest **OFF**. Nie trzeba edytować kodu C#.

Po włączeniu silnik wysyła dotychczasowe komunikaty rozpoczęcia, ręcznego zamknięcia, ponownego otwarcia i ujawnienia wyniku quizu lub zakończenia ankiety POLL.

Samo wygaśnięcie timera bez AutoReveal nadal tylko zamyka głosowanie; nie dodano nowego komunikatu dla tego przypadku.

Komunikat wyniku jest wysyłany po zatwierdzonym zapisie rundy.

Odpowiedzi na komendy własnych punktów, rankingu i komendy administracyjne pozostają dostępne także przy wyłączonych komunikatach rundy.

Przełącznik jest częścią ustawień silnika, pełnych backupów i snapshotów.

Dotychczasowy stan bez tego pola otrzymuje OFF. Przywrócenie pełnej starszej kopii przywraca OFF; przywrócenie samego rankingu zachowuje bieżące ustawienie.

Starszy panel zapisujący ustawienia bez tego pola zachowuje jego dotychczasową wartość. Zmiany w szkicu zaczynają obowiązywać dopiero po potwierdzonym zapisie.

### Tryby rozpoznawania głosów

LukiQuizEngine 2.2.0 obsługuje trzy sposoby rozpoznawania wiadomości zawierających głos.

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

LukiQuizEngine 2.2.0 zawiera wbudowany lokalny symulator dostępny z poziomu zaawansowanego interfejsu.

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

Eksport rankingu CSV nie stanowi również kompletnej kopii danych Streak i Osiągnięć. Do ich zachowania użyj pełnego backupu.

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

Panel umożliwia ręczne czyszczenie archiwów.

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

LukiQuizEngine udostępnia również opcjonalne ustawienia automatycznego przechowywania i czyszczenia archiwów.

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
| Presety ustawień pytania | Pamięć lokalna przeglądarki (`localStorage`) |
| Ustawienie Ochrony podczas transmisji | Pamięć lokalna przeglądarki (`localStorage`) |
| Ustawienia i postęp Streak oraz Osiągnięć | Dodatkowe pola stanu `localQuiz_data_v2` |
| Pełny backup stanu | Plik eksportowany na żądanie z panelu WWW |
| Hasło WebSocket | Pamięć uruchomionego klienta, a w przypadku OBS również fragment pełnego URL overlayu |

Zmienne oznaczone jako `v1` należą do starszego silnika lub procesu migracji.

**Nie są podstawowym miejscem przechowywania rankingu V2.**

Nie usuwaj ich bez wykonania odpowiednich kopii i upewnienia się, że migracja została zakończona poprawnie.

Folder AppData jest powiązany z aktualnym użytkownikiem systemu Windows.

Jeżeli przenosisz LukiQuizEngine na inny komputer lub konto Windows, skopiuj osobno wszystkie potrzebne archiwa.

Podmiana plików HTML i C# nie przenosi automatycznie lokalnych archiwów ani presetów zapisanych w pamięci przeglądarki.

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
- Ochrona podczas transmisji zabezpiecza przed przypadkowymi operacjami w interfejsie ADVANCED, ale nie usuwa ograniczenia bezpieczeństwa HTTP `/DoAction`.
- Przywracanie pełnych backupów i snapshotów może zostać zablokowane, jeżeli istnieją aktywne ustawienia lub dane Streak i Osiągnięć, a przywracany stan jest z nimi niezgodny.
- Presety i część preferencji interfejsu są przechowywane lokalnie w przeglądarce. Wyczyszczenie jej danych może spowodować ich utratę.

## Aktualizacja starszej instalacji

### Z v2.1.0 BETA do v2.2.0 STABLE

1. Zakończ lub anuluj aktywną rundę.
2. Wyeksportuj aktualny ranking CSV i pełny backup stanu.
3. Zachowaj folder poprzedniej instalacji oraz wszystkie ważne archiwa rankingów.
4. Zastąp kod C# w tej samej Action `QUIZ - Silnik` nową zawartością `QuizEngine.cs`.
5. Wykonaj **Find Refs**, a następnie **Save and Compile**.
6. Podmień razem pliki `panel.html` i `overlay.html`.
7. Zachowaj istniejący trigger wiadomości Twitch i wszystkie trwałe zmienne V2.
8. W razie potrzeby odśwież pamięć podręczną przeglądarki i źródła przeglądarkowego OBS.
9. Upewnij się, że panel i silnik pokazują wersję `2.2.0` oraz identyfikatory `PHoeniX` i `QUPA`.
10. Sprawdź istniejący ranking, historię rund, szablony, zestawy pytań i archiwa.
11. Przetestuj tryby panelu, zmianę języka, głosowanie Twitch oraz komendy `!punkty` i `!ranking`.
12. Sprawdź nowe ustawienia ADVANCED. Streak i Osiągnięcia powinny pozostać wyłączone przy odczycie danych z 2.1.0.
13. Jeżeli korzystasz z nowych systemów rywalizacji, wykonaj osobny test ich naliczania, zapisu oraz cofania rund.

Nie usuwaj trwałych zmiennych V2 ani istniejących archiwów podczas aktualizacji.

Zachowaj ten sam host i port panelu, jeżeli chcesz utrzymać dane `localStorage`, w tym preferencje interfejsu i presety.

Przed przywracaniem starszego pełnego backupu zapoznaj się z ograniczeniami dotyczącymi danych Streak i Osiągnięć.

Szczegółowe zasady aktualizacji i przywracania opisuje [MIGRATION.md](MIGRATION.md).

### Historyczna aktualizacja z v2.0.2 BETA do v2.1.0 BETA

Poniższa instrukcja dotyczy historycznego przejścia pomiędzy wcześniejszymi wydaniami.

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

Zapoznaj się z CHANGELOG i [MIGRATION.md](MIGRATION.md), aby sprawdzić zmiany wprowadzone w wydaniach pośrednich oraz zasady przejścia do 2.2.0.

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
| Komendy `!punkty` lub `!ranking` nie odpowiadają | Sprawdź aktywne nazwy komend zapisane w ADVANCED oraz ich cooldowny |
| Nie pojawiają się komunikaty o rundzie | Sprawdź, czy opcja jest włączona w ADVANCED i czy zapisano ustawienia |
| Streak lub Osiągnięcia nie naliczają postępu | Sprawdź odpowiednie przełączniki w ADVANCED, czy rozpoczęto nową rundę po ich włączeniu i czy runda kwalifikuje się do naliczenia |
| Nie można przywrócić pełnego backupu lub snapshotu | Sprawdź, czy istnieje aktywny lub zapisany postęp Streak i Osiągnięć oraz czy backup jest z nim zgodny |
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

Wersja **2.2.0** ma już konfigurowalne tryby rozpoznawania głosów `STRICT`, `WHITESPACE` i `ATTACHED`.

Nie zastępuj wyrażeń regularnych w aktualnym silniku 2.2.0 poniższymi przykładami.

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

- [CHANGELOG.md](CHANGELOG.md) - historia wydań i zmian wprowadzanych w LukiQuizEngine V2, w tym wersji 2.2.0 STABLE.
- [MIGRATION.md](MIGRATION.md) - instrukcja migracji ze starszych wersji do 2.2.0.
- [READMElegacy.md](1.3.0/READMElegacy.md) - archiwalna dokumentacja wersji 1.3.0.
- Starsze wersje głównego README są dostępne w [historii zmian pliku na GitHubie](https://github.com/Lukituki6/LukiQuizEngine-for-Streamer.bot/commits/main/README.md).

Historycznych instrukcji instalacji dla wersji 1.3.0 nie należy traktować jako aktualnych instrukcji dotyczących 2.2.0.

**Wykorzystanie AI:** przy rozwijaniu projektu korzystałem również z narzędzi sztucznej inteligencji, między innymi podczas analizy kodu i prac programistycznych.

Wszystkie istotne zmiany należy przetestować w docelowym środowisku Streamer.bot.

Projekt udostępniam na warunkach [licencji MIT](LICENSE), **„tak jak jest”**, bez gwarancji działania.

> [!IMPORTANT]
> Korzystasz z niego na własną odpowiedzialność, szczególnie podczas zmieniania konfiguracji, importowania danych i zarządzania archiwami.
