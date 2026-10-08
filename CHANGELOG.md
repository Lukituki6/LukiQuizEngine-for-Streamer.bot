# Historia zmian - LukiQuizEngine

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
