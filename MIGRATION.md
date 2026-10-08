# Przejście z LukiQuizEngine 1.3.0 na 2.0.0 BETA

Ten poradnik dotyczy aktualizacji istniejącej instalacji. Jeżeli instalujesz quiz po raz pierwszy, skorzystaj z instrukcji w `README.md`.


## Zanim zaczniesz

1. Zakończ i rozlicz bieżącą rundę albo ją anuluj. Aktualizację najlepiej wykonać wtedy, gdy quiz nie jest aktywny.
2. W wersji 1.3.0 wyeksportuj ranking do CSV i zrób kopię folderu oraz danych Streamer.bot.
3. Zachowaj poprzednie pliki `QuizEngine.cs`, `panel.html` i `overlay.html`. Pozwoli to wrócić do poprzedniej wersji, jeśli będziesz chciał, w release na moim repo dalej zostanie stara wersja 1.3.0.
4. Nie usuwaj globalnych zmiennych Streamer.bot ani danych przeglądarki (`localStorage`).

## Aktualizacja plików

1. Umieść nowe `panel.html` i `overlay.html` w katalogu udostępnianym przez lokalny HTTP Server Streamer.bot. Możesz też przestawić mapowanie HTTP na nowy katalog.
2. W istniejącej Action `QUIZ - Silnik` **zastąp cały kod C#** zawartością nowego `QuizEngine.cs`. Nie zostawiaj dwóch aktywnych kopii silnika.
3. Użyj `Find Refs`, a następnie `Save and Compile`. Sprawdź, czy kod kompiluje się bez błędów.
4. Zachowaj trigger `Twitch > Chat > Message` przypisany do tej samej Action. Nie trzeba tworzyć osobnych triggerów dla odpowiedzi.
5. Zostaw włączone `Authentication` i `Enforce` dla lokalnego WebSocket Server. Panel i overlay wymagają tych zabezpieczeń.
6. Otwórz panel, np. `http://127.0.0.1:7474/quiz/panel.html?v=2.0.0-beta`, wpisz hasło WebSocket i kliknij **Połącz bezpiecznie**. Jeśli masz inne porty lub mapowanie HTTP, użyj swoich adresów.
7. Po połączeniu otwórz **Diagnostyka** i uruchom **Test instalacji**.

## Co silnik przenosi z 1.3.0

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

## Szkice i szablony z przeglądarki

Aby przenieść stare lokalne szablony i szkic, otwórz nowy panel z tego samego originu co wcześniej: **ten sam host i port**. `localhost` oraz `127.0.0.1` to dla przeglądarki dwa różne miejsca przechowywania danych.

Panel szuka dawnych danych w `localQuiz.templates.v1` i `localQuiz.draft`. Bieżący szkic V2 jest zapisywany w `localQuiz.draft.v2` oraz może być zapisany w silniku. Do odczytu wersji z silnika służy przycisk **Wczytaj szkic z silnika**.

Jeżeli po aktualizacji nie widzisz starych szablonów, sprawdź najpierw, czy otworzyłeś panel pod tym samym adresem lokalnym. Nie czyść danych przeglądarki, dopóki nie upewnisz się, że migracja się udała.

## Sprawdzenie po aktualizacji

1. Porównaj liczbę użytkowników i punkty w nowym rankingu z eksportem CSV z 1.3.0.
2. Sprawdź szablony, zestawy i szkic. Zwróć uwagę na ewentualne komunikaty o błędach migracji.
3. Połącz panel i **wygeneruj nowy URL overlayu**. Wklej go do źródła przeglądarkowego OBS. Adres zawiera hasło WebSocket, więc nie udostępniaj go.
4. Przeprowadź krótką rundę testową: rozpoczęcie, głos `!1`/`!2`, zamknięcie, ujawnienie wyniku i ranking.
5. Jeśli korzystasz z dodatkowych funkcji, sprawdź timer, zapis szablonów, cofanie i przeliczanie wyniku, import CSV, drużyny oraz backup.
6. Wyeksportuj pełny backup z 2.0.0 i przechowuj go w bezpiecznym miejscu. Może zawierać identyfikatory i nazwy widzów oraz historię głosów.

## Co zrobić, jeśli migracja się nie uda

Nie usuwaj starych danych ani nie resetuj rankingu w ciemno. Sprawdź logi Action w Streamer.bot, zachowaj kopię istniejących zmiennych i porównaj je z wcześniejszym eksportem CSV. Silnik odrzuca niepoprawny JSON i nie powinien zastępować uszkodzonego stanu pustym rankingiem.

Pamiętaj, że test połączenia HTTP/WebSocket nie potwierdza działania Twitcha i OBS. Te elementy sprawdź osobno.

## Powrót do 1.3.0

Możesz przywrócić stare pliki i kod Action. Stare zmienne v1 pozostają dostępne, więc poprzedni silnik powinien zobaczyć stan sprzed migracji, o ile te dane nie zostały osobno zmienione.

**Punkty i rundy zdobyte już w 2.0.0 nie przejdą automatycznie do 1.3.0.** Jeśli chcesz zachować nowy ranking przy powrocie, wyeksportuj CSV w 2.0.0 i sprawdź zgodność pliku z importerem 1.3.0 przed jego użyciem. Zestawy, drużyny i historia V2 nie mają bezpośrednich odpowiedników w 1.3.0. Zachowaj również pełny backup 2.0.0.
