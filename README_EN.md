> [!NOTE]
> This project is not affiliated with, endorsed by, or officially supported by Streamer.bot, Twitch, YouTube, or Kick. Streamer.bot, Twitch, YouTube, and Kick are trademarks or names of their respective owners. This project only uses features available in Streamer.bot and is provided as an independent community tool.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# LukiQuizEngine for Streamer.bot

**Current release: v2.2.0 STABLE**

**Language:** [Polski](README.md) | English

- [READMElegacy.md](1.3.0/READMElegacy.md) - archived documentation for the older **LukiQuizEngine v1.3.0**, available in the [1.3.0](1.3.0/) directory. It contains the original installation, configuration, and usage instructions. The information and code examples apply to version 1.3.0 and should not be used directly with the current 2.2.0 version.

LukiQuizEngine is a local quiz engine for Streamer.bot featuring a web control panel, an OBS overlay, Twitch chat voting, round history, and persistent leaderboards.

It requires only **one C# Action and one Twitch chat message trigger**. No Node.js, Python, separate `.exe` application, external database, or hosting service is required.

> [!IMPORTANT]
> Version **2.2.0 STABLE** adds new operator tools, optional competitive systems, and additional Twitch command settings. Streak and Achievements are disabled by default. Before updating, back up your existing data and verify your installation. A stable release does not guarantee that every bug or limitation has been eliminated. Isolated tests do not replace testing with the real Streamer.bot environment and Twitch chat. The known HTTP `/DoAction` security limitation is explained in [Security and known limitations](#security-and-known-limitations).

## Table of contents

- [Features](#features)
- [Quality of Life and competitive features in 2.2.0](#quality-of-life-and-competitive-features-in-220)
- [Files and requirements](#files-and-requirements)
- [Fresh installation](#fresh-installation)
- [First test](#first-test)
- [Panel modes](#panel-modes)
- [Language settings](#language-settings)
- [Twitch voting and commands](#twitch-voting-and-commands)
- [Local quiz simulator](#local-quiz-simulator)
- [Leaderboards, archives, and restoration](#leaderboards-archives-and-restoration)
- [Where data is stored](#where-data-is-stored)
- [Security and known limitations](#security-and-known-limitations)
- [Upgrading an existing installation](#upgrading-an-existing-installation)
- [Troubleshooting](#troubleshooting)
- [Optional voting modification for v1.3.0](#optional-voting-modification-for-v130)
- [Project history and license](#project-history-and-license)

## Features

### Quiz, questions, and voting

- Questions with **2-12 answer options**, including multiple correct answers and individual point values.
- Scoring modes: `adjacent`, `manual`, `none`, and `custom`, including partial points and optional speed bonuses.
- Voting policies: `LAST`, `FIRST`, and `LIMITED`, with the ability to inspect and correct votes through the panel.
- Result visibility settings before revealing answers: `LIVE`, `COUNT_ONLY`, and `HIDDEN`.
- `POLL` mode for running polls without awarding leaderboard points.
- Answer shuffling, question templates, question sets, and JSON import/export.
- Three configurable vote parsing modes: `STRICT`, `WHITESPACE`, and `ATTACHED`.

### Rounds and streaming

- Manual voting controls, timer, pause, resume, time adjustments, and optional `AutoReveal`.
- Round history with filtering, searching, undoing, and recalculating eligible results within the current scoring epoch.
- Team mode with viewer assignments and separate team scores.
- OBS overlay layouts: `FULL`, `COMPACT`, `MINIMAL`, and `NO_LEADERBOARD`.
- Overlay appearance customization, optional visual effects, sounds, and podium display.
- Keyboard shortcuts and installation diagnostics.
- A built-in local quiz simulator for testing quiz behavior without affecting actual rankings.
- A **LIVE toolbar** for controlling the active round in ADVANCED mode.
- **Round summary** with participant statistics, correct answers, and awarded points.
- **Prepare next question** without automatically starting voting.
- Optional round event announcements in Twitch chat.

### Panel and interface

- Three interface modes: `SIMPLE`, `ADVANCED`, and `LEGACY`.
- Improved button styling, visual feedback, active-state highlighting, and more readable controls.
- A Legacy interface inspired by LukiQuizEngine 1.3.0.
- Polish and English language support for the panel and overlay.
- Optional language synchronization between the panel and overlay.
- A shared quiz engine and shared data across all panel modes.
- **Settings and Help search** with support for multiple words and case-insensitive searching that ignores Polish diacritics.
- Four collapsible settings categories and an improved keyboard shortcut layout.
- **Stream Protection** - optional confirmation prompts for risky operations in ADVANCED.
- **Question settings presets** for saving and loading selected quiz configurations.

### Leaderboards and archiving

- Persistent viewer scores and answer statistics.
- CSV leaderboard import/export, full-state backups, and automatic snapshots.
- **Automatic leaderboard archiving before a reset**, provided there is data to preserve.
- **Manual leaderboard archiving without resetting scores**, including optional archive names.
- **Browsing, searching, filtering, and previewing archived leaderboards** directly in the panel.
- **Restoring an archived leaderboard** after entering `PRZYWROC`, with a safety backup of the current results.
- Manual deletion of selected archives with confirmation.
- Optional automatic archive cleanup based on retention settings.
- An **Open Archive Folder** button that opens the local archive directory in Windows File Explorer.

### Streak and achievements

- An optional correct-answer streak system, disabled by default.
- Tracking of the current streak, best streak, and total correct answers.
- Configurable point bonuses for reaching specific streak thresholds.
- An optional system of 9 built-in achievements, disabled by default.
- Viewing unlocked achievements, their requirements, and recorded dates in the ADVANCED panel.
- Preserving existing progress when either system is disabled.

### Detailed chat commands guide

LukiQuizEngine allows viewers to vote and check their scores directly through Twitch chat. The broadcaster and moderators can also manage quiz rounds using administrative commands.

All examples below use the default command names.

#### Viewer commands

| Command | Description |
| --- | --- |
| `!1` to `!12` | Votes for the corresponding answer number, provided that the option exists in the current round. |
| `!punkty` | Displays the viewer's own points, correct answers, and total answered questions. |
| `!ranking` | Displays the TOP 5 participants with the highest scores in Twitch chat. |

**Important notes:**

- Votes are accepted only while voting is open.
- Each question can contain up to 12 answer options.
- Whether viewers can change their votes depends on the selected voting policy (`LAST`, `FIRST`, or `LIMITED`).
- The default `STRICT` mode requires a standalone voting command, such as `!3`. Other modes are explained below.
- The `!punkty` and `!ranking` commands may be subject to individual and global cooldowns.

#### Administrative commands

Administrative commands are available only to the channel broadcaster and Twitch moderators.

| Command | Description |
| --- | --- |
| `!quiz` | Displays a short list of administrative commands. |
| `!quiz pomoc` | Displays the basic command list and usage syntax. |
| `!quiz start Question \| Answer 1 \| Answer 2` | Starts a new quiz round with the specified question and answers, without an automatic timer. |
| `!quiz zamknij` | Closes voting. Viewers can no longer submit new votes. |
| `!quiz wynik 3` | Reveals answer 3 as correct and awards 1 point for each correct vote. |
| `!quiz wynik 3 5` | Reveals answer 3 and awards 5 points for each correct vote. |
| `!quiz wynik 7 3 1` | Awards 3 points for answer 7 and 1 point for each adjacent answer, numbers 6 and 8. |
| `!quiz anuluj` | Cancels the current round without awarding points. |
| `!quiz pokaz` | Shows the current quiz or podium in the OBS overlay, when available. |
| `!quiz ukryj` | Hides the current quiz or podium in the OBS overlay. |

#### How to use the commands: example

Suppose you want to run a quiz with three possible answers.

**1. Start a quiz round**

`!quiz start What is the capital of Poland? | Krakow | Warsaw | Gdansk`

The engine starts a new round with three answer options and no automatic timer.

**2. Viewers submit their votes**

Viewers can type `!1`, `!2`, or `!3` in Twitch chat.

**3. Close voting**

`!quiz zamknij`

Once voting is closed, no additional votes are accepted.

**4. Reveal the correct answer**

`!quiz wynik 2 5`

The engine reveals Warsaw as the correct answer and awards 5 points to each viewer who voted correctly.

**5. Check the leaderboard**

`!ranking`

The bot displays the top five participants in Twitch chat.

#### Additional information

- The syntax `!quiz wynik NUMBER POINTS ADJACENT_POINTS` allows you to award points for the correct answer and its neighboring options. If no point values are specified, the defaults are 1 point for the correct answer and 0 for adjacent answers.
- Manually selecting other partially correct answers and configuring advanced scoring options requires the web panel.
- The engine also supports selected alternative command names, including `!quiz stop`, `!quiz help`, `!quiz cancel`, `!quiz show`, `!quiz hide`, and `!quiz odpowiedz`.
- Administrative commands must respect the current round state. Some operations require an active or appropriately completed voting phase.
- The default command names `!punkty`, `!ranking`, and `!quiz` can be changed in ADVANCED settings. Changing the administrative prefix also changes the prefix used by its subcommands.
- There is no need to create separate commands in Streamer.bot's Commands section. All commands are handled by the existing `QUIZ - Silnik` Action and a single `Twitch > Chat > Chat Message` trigger.
- Switching the panel language to English does not automatically translate Twitch chat commands.

## Quality of Life and competitive features in 2.2.0

New operator tools are available in **ADVANCED**, within the existing LIVE, question editing, leaderboard, and settings sections. No new categories were added to the left navigation. SIMPLE and LEGACY retain their regular leaderboard interfaces. Switching panel modes does not interrupt the tracking of features already enabled in the engine.

### Operator tools

- **LIVE toolbar** displays the current question, round status, remaining time from the engine clock, final accepted votes, and voting lock state. Pause, resume, +10 s, -10 s, close, and reveal use the shared engine. Unavailable operations are disabled, and repeated actions are blocked while waiting for confirmation. Refresh the state if an operation has an uncertain result.
- **Stream Protection** adds confirmation prompts for risky operations while preserving existing required confirmations. It is optional, stored in the current browser, and only applies to ADVANCED. It protects the operator against accidental actions but does not provide HTTP authentication.
- **Round summary** reads round history to display participants, final accepted votes, correct answers, accuracy percentage, total awarded points, and the highest individual reward. Partial points and bonuses are included. Data for all rejected vote attempts and complete pre-round leaderboard positions is unavailable; these metrics are displayed as unavailable. POLL rounds do not have correct-answer accuracy or points. Cancelled and undone rounds do not show awarded points. Recalculation updates the summary. Viewing the summary does not award points.
- **Question settings presets** allow you to save a name and selected settings groups: timer, voting, scoring, and round mode. Example presets include Quick Quiz, Standard Quiz, Tournament, and Poll. You can create, update, and delete custom presets and preview differences before applying them. Applying a preset changes only the draft and does not start a round. Preset format 1 is stored in the current browser's `localStorage`, with a maximum of 30 presets. Presets do not include passwords, viewer data, leaderboards, or archives. Keep the same host and port when updating.
- **Prepare next question** uses the existing question-set queue, including any previously randomized order. It prepares a draft without starting voting, warns before replacing a modified draft, and detects when the question set has ended. Repeated clicks do not skip an already prepared question. Starting the round remains a separate operator action.
- **Settings and Help search** provides two independent search tools. Settings can be searched in ADVANCED, while Help search is available in SIMPLE and ADVANCED. Search supports multiple words and ignores letter case and Polish diacritics. Matching sections expand automatically. Searching does not modify saved settings or execute engine operations.
- **Improved settings layout** introduces four collapsible settings categories and a more readable keyboard shortcut layout that adapts to screen width. Collapsing a category does not change quiz settings.

### Enabling the new systems

In ADVANCED Settings, open **Competitive - optional**. Streak and Achievements have **two independent switches, both OFF by default**.

Save changes after finishing or cancelling the active round. A new round records the enabled systems and bonus configuration at the moment it starts. Previous rounds do not receive retroactive competitive statistics.

Disabling a system pauses its tracking without deleting saved progress. Neither switch automatically enables the other.

### Streak and achievements

Streak increases by one for a final answer marked as correct in a successfully settled QUIZ round. Changed votes are evaluated according to FIRST, LAST, or LIMITED. Multiple correct options qualify equally. Partial points awarded for an answer that is not marked correct do not qualify.

An incorrect answer or failure to vote in an eligible round resets the current streak of a previously tracked viewer. POLL, cancelled, and undone rounds do not count. When Streak is disabled, the streak is paused.

The engine stores the current streak, best streak, and total correct answers tracked by Streak. Thresholds 3, 5, and 10 have a default bonus of **0**.

You can configure up to 20 unique thresholds from 1 to 10000, with bonuses from 0 to 1000 points. A bonus is awarded when the exact threshold is reached. The total reward for one round cannot exceed the existing limit of 2000 points per viewer. Overall leaderboard limits are also preserved. The bonus is part of the same settlement and is included in team points.

The achievement catalog contains **9 built-in achievements**:

- First correct answer.
- 10 correct answers.
- 25 correct answers.
- 50 correct answers.
- A streak of 3 correct answers.
- A streak of 5 correct answers.
- A streak of 10 correct answers.
- Participation in 10 rounds.
- Participation in 50 rounds.

The panel displays achievement requirements, active status, unlocked count, and unlock dates for the selected viewer. Achievements do not award additional points.

Streak-related achievements require both systems to be enabled and only count eligible activity during their enabled period. Enabling the system does not automatically unlock achievements based on older progress. Disabling achievements cannot be used to connect separate correct-answer streaks.

Undoing and recalculating rounds rebuilds the competitive projection using stored settlements and compacted historical data. An achievement may be removed if its requirements are no longer satisfied.

Restarting, refreshing the panel, or revealing the same result again does not award the same reward twice. The new statistics are not the old streak fields from version 1.3.0.


## Files and requirements

| File | Purpose |
| --- | --- |
| `QuizEngine.cs` | C# quiz engine to be pasted into a Streamer.bot Action |
| `panel.html` | Local web control panel |
| `overlay.html` | Browser source for OBS |
| `OTWORZ_PANEL.url` | Optional shortcut to the control panel |
| `PODGLAD_OVERLAYU.url` | Optional shortcut to the demonstration overlay |
| `LICENSE` | MIT license |

The shortcut files may not be included in every package. The panel and overlay can always be opened directly through their HTTP URLs.

### Requirements

- Windows with a compatible version of Streamer.bot.
- Support for `Execute C# Code`, `HTTP Server`, and `WebSocket Server`.
- A modern web browser.
- A connected Twitch account if you want viewers to vote through Twitch chat.
- OBS Studio if you want to display the quiz overlay during a stream.

OBS is not required to operate the web control panel itself.

The code has been reviewed for compatibility with the Streamer.bot **1.0.7** environment, but full compatibility with every host version is not guaranteed.

## Fresh installation

### 1. Download the correct package

Download the appropriate **LukiQuizEngine** package from the GitHub Releases section once the release is available.

Extract it to a permanent directory, for example:

```text
C:\StreamerBot\LukiQuizEngine
```

Do not mix `panel.html`, `overlay.html`, and `QuizEngine.cs` files from different builds.

**All three files must belong to the same release.**

### 2. Create one Action in Streamer.bot

1. Open the **Actions** tab in Streamer.bot.
2. Create a new Action named exactly `QUIZ - Silnik`.
3. Add the sub-action **Core > C# > Execute C# Code**.
4. Paste the entire contents of `QuizEngine.cs`.
5. Click **Find Refs**, followed by **Save and Compile**.
6. Check that the code compiles successfully.
7. Attach **one** `Twitch > Chat > Message` trigger to the same Action.
8. Make sure the Action is enabled.

Do not create separate triggers for `!1`, `!2`, or any other voting commands.

### 3. Configure the WebSocket Server

In **Servers/Clients > WebSocket Server**, use:

```text
Address / Host: 127.0.0.1
Port:           8080
Endpoint:       /
Authentication: ON
Enforce:        ON
Password:       Your own strong, unique password
```

Save the settings and start the server.

If the server was already running while you changed its configuration, restart it.

### 4. Configure the HTTP Server

In **Servers/Clients > HTTP Server**, configure:

```text
Host: 127.0.0.1
Port: 7474
```

Add the following folder mapping:

```text
Path:   quiz
Folder: C:\StreamerBot\LukiQuizEngine
```

The folder must point to the actual location of your LukiQuizEngine files.

Start the HTTP Server.

**Do not change the host to `0.0.0.0` or expose the HTTP Server through a LAN, tunnel, or the internet.**

See the security section for an explanation.

### 5. Connect the web panel

Open the following address in your browser:

```text
http://127.0.0.1:7474/quiz/panel.html
```

You may also use `OTWORZ_PANEL.url` if the shortcut is included in your package.

Enter your WebSocket password, click **Connect Securely** (or **Połącz bezpiecznie** in Polish), and open the **Diagnostics** section.

Some network configuration properties cannot be conclusively verified by the built-in diagnostics.

A successful diagnostics result does not replace manually checking your Streamer.bot server settings.

### 6. Add the overlay to OBS

After connecting the web panel successfully:

1. Click **Copy Overlay URL** in the panel.
2. Open OBS Studio.
3. Add a new **Browser** source.
4. Paste the complete copied URL.
5. Set the resolution to `1920 x 1080`.
6. Save the browser source settings.

Do not use the demonstration address containing `?demo=1` during a real stream.

The complete overlay URL contains the WebSocket password in the URL fragment after `#`.

Demonstration overlay:

```text
http://127.0.0.1:7474/quiz/overlay.html?demo=1
```

## First test

1. Start a test round containing two answer options.
2. Send `!1` from a viewer account in the connected Twitch chat.
3. Check whether the vote appears in the panel and overlay.
4. Close voting, reveal the correct answer, and verify the awarded points.
5. Test the `!punkty` and `!ranking` commands.
6. Run another round using the timer and `AutoReveal`.
7. Test manual leaderboard archiving, archive browsing, and CSV export.
8. Switch between SIMPLE, ADVANCED, and LEGACY panel modes.
9. Test the panel and overlay in both Polish and English.
10. Optionally use the built-in simulator to test scoring and voting behavior without modifying real quiz data.
11. In ADVANCED, check the LIVE toolbar, round summary, and Prepare next question.
12. Test Settings and Help search and save custom command names.
13. If you plan to use Streak or Achievements, test both systems separately before going live.

## Panel modes

LukiQuizEngine 2.2.0 offers three interface modes.

### SIMPLE

Designed for regular quiz operation.

Provides convenient access to essential features, including:

- Questions and voting rounds.
- Templates and question sets.
- Leaderboards.
- Round history.
- Basic controls and help.

### ADVANCED

Provides access to the full set of available controls and tools, including:

- Advanced quiz settings.
- Data management.
- Archive management.
- Overlay customization.
- Diagnostics.
- The local quiz simulator.
- The LIVE toolbar and round summary.
- Question settings presets and Prepare next question.
- Optional Stream Protection.
- Streak and Achievements settings.
- Custom Twitch command names and round announcements.
- Settings search.

### LEGACY

A redesigned interface inspired by the original appearance of **LukiQuizEngine 1.3.0**.

It preserves the familiar visual style while using the modern V2 engine.

Selected V2 features, including round history and leaderboard archives, remain accessible in Legacy mode.

### Shared data and behavior

All three panel modes use the **same quiz engine, leaderboard, and configuration**.

Switching between them does not reset the leaderboard or active quiz settings.

The interface also includes improved color differentiation, active button highlighting, and visual feedback for selected controls.

The new 2.2.0 operator tools are available in ADVANCED. If Streak or Achievements have been enabled, switching to SIMPLE or LEGACY does not disable their tracking in the engine.

## Language settings

LukiQuizEngine 2.2.0 supports:

- **Polish (PL)**
- **English (EN)**

The language can be changed directly in the panel.

The overlay also supports both languages.

The overlay language can either follow the panel language or be configured independently.

When synchronization is enabled, the overlay follows the language selected in the panel.

Changing the interface language does not reset quiz scores or modify voting results.

**Important:** Action identifiers, confirmation phrases, and internal variable names remain unchanged regardless of the selected interface language.

Switching PL/EN does not rename commands. The default command names below can be configured separately in ADVANCED Settings:

```text
!punkty
!ranking
```

Required confirmation phrases such as `PRZYWROC` and `USUN ARCHIWA` must also be entered exactly as shown.

## Twitch voting and commands

| Command | Description |
| --- | --- |
| `!1` ... `!12` | Vote for the corresponding answer number |
| `!punkty` | Display your own score |
| `!ranking` | Display the top leaderboard entries in Twitch chat |
| `!quiz ...` | Moderator commands supported by the quiz engine |

The names listed above are the default command names.

### Custom chat command names

In **ADVANCED -> Settings -> Chat commands and voting -> Custom chat commands**, set the own-score command, ranking command, and admin prefix, then click **Save settings**.

Defaults are `!punkty`, `!ranking`, and `!quiz`. For example, you can use `!wynik`, `!topka`, and `!q`.

Each name must be between 2 and 33 characters: `!`, an ASCII A-Z/a-z letter, followed by letters, digits, `_`, or `-`. The three names must differ, ignoring letter case. Spaces, non-ASCII letters, and numeric prefixes such as `!1` or `!1test` are rejected to avoid voting collisions.

Votes `!1`-`!12` remain unchanged. Choose names unused by other bots or Actions, because the panel does not inspect their configuration.

After a confirmed save, the new names replace the old ones. Previous names are not retained as additional aliases.

Changing the admin prefix also changes its subcommands. For example, after setting `!q`, use `!q help`, `!q zamknij`, or `!q wynik 1`.

Broadcaster/moderator permissions, score/ranking cooldowns, and scoring remain unchanged. Help displays active engine command names rather than an unsaved draft. Restoring defaults changes only the three draft fields and still requires saving.

Command names are persisted settings included in full backups and snapshots. Older state without these fields receives the original default names. Older settings-write requests that omit the fields preserve their current values.

See [MIGRATION.md](MIGRATION.md) for restoration and rollback rules.

Administrative chat commands are subject to the sender's permissions and moderator privileges.

### Chat response formatting

The own-score and leaderboard commands use ` | ` as a separator for improved readability.

In `!punkty`, it separates the score from answer statistics. In `!ranking`, it separates leaderboard entries.

Example output:

```text
Lukituki6: 150 pkt | poprawne 18/25
```

```text
Quiz TOP 5: 1. Lukituki6 - 150 pkt | 2. John - 120 pkt | 3. Blond - 95 pkt
```

The hyphen between a viewer's name and their point total in the leaderboard remains unchanged.

This is a display-only change and does not affect scoring, leaderboard order, or cooldowns.

### Round announcements in chat

In **ADVANCED -> Settings -> Chat commands and voting**, toggle **Round announcements in chat**, then click **Save settings**.

The default is **OFF**. Editing C# code is no longer required.

When enabled, the engine sends the existing messages for starting, manually closing, reopening, and revealing a quiz result or completing a POLL.

Timer expiry without AutoReveal still only closes voting. No new announcement was added for this event.

Result announcements are sent after the round has been saved successfully.

Replies to own-score, ranking, and administrative commands remain available when this option is OFF.

The toggle is part of engine settings, full backups, and snapshots.

Older state without the field defaults to OFF. Restoring an older full backup also restores OFF. Ranking-only restoration keeps the current setting.

A legacy panel settings payload that omits the field preserves its current value. Draft changes take effect only after confirmed saving.

### Vote parsing modes

LukiQuizEngine 2.2.0 supports three vote parsing modes.

| Mode | Accepted examples | Behavior |
| --- | --- | --- |
| `STRICT` | `!1` | Only an exact voting command is accepted |
| `WHITESPACE` | `!1`, `!1 hello` | Allows additional text separated from the command by whitespace |
| `ATTACHED` | `!1`, `!1 hello`, `!1hello` | Also accepts text immediately after the command |

The default mode is `STRICT`.

The selected mode determines how the engine interprets voting messages. The number must still refer to an available answer option.

### Important information about ATTACHED mode

This mode can potentially conflict with other Twitch commands.

For example:

```text
!1v1
```

May be interpreted as a vote for answer `1` when `ATTACHED` mode is enabled.

For streams using many custom chat commands, `STRICT` or `WHITESPACE` may be a safer choice.

The engine also rejects certain invalid, invisible, and control characters to reduce incorrect vote recognition.

## Local quiz simulator

LukiQuizEngine 2.2.0 includes a built-in local simulator, available through the advanced interface.

The simulator allows you to test selected quiz mechanics without running a real Twitch voting session.

### Available simulation features

- Create a test question.
- Use a separate copy of the current question draft.
- Generate fictional viewers.
- Simulate random votes.
- Submit or change individual test votes.
- Test voting policies.
- Test `STRICT`, `WHITESPACE`, and `ATTACHED` vote parsing.
- Test scoring and partial-point rules.
- Test timers, pausing, and resuming.
- Close, reopen, reveal, or cancel test rounds.
- View simulated scores.

The simulator supports generating up to **2,000 fictional participants**.

### Data isolation

The simulator maintains its own data in the memory of the current browser page.

It does **not** modify:

- The real leaderboard.
- Actual voting history.
- Saved ranking archives.
- The active quiz state in Streamer.bot.

Refreshing the page clears the simulation.

### Simulator limitations

The simulator reproduces selected quiz mechanics, but it is not a complete replacement for testing with the real Streamer.bot environment.

It does not fully test:

- Twitch connectivity.
- Streamer.bot Action execution.
- OBS integration.
- Team functionality.
- Chat permissions.
- All live chat rate limits and cooldown interactions.

Always perform a real integration test before using a new release during a live stream.

## Leaderboards, archives, and restoration

### Current leaderboard and CSV

The **Leaderboard** section allows you to:

- View scores.
- Correct existing results.
- Export leaderboard data as CSV.
- Import leaderboard data from CSV.
- Reset the leaderboard.
- Create manual leaderboard archives.

Imports and resets can change the current score data.

Always review the preview and confirmation message before proceeding.

A **full-state backup** is not the same as a CSV leaderboard export.

A CSV leaderboard export is also not a complete backup of Streak and Achievements data. Use a full-state backup to preserve this information.

### Automatic archiving before a reset

Before resetting a non-empty leaderboard, the engine creates a local JSON archive.

If the required archive operation fails, the reset should be rejected.

Do not manually delete operation-control files while an archive or reset operation is in progress.

If the panel stops responding, do not assume the reset was unsuccessful.

Verify the current state before trying again.

### Manual archive creation and browsing

The **Save Leaderboard** button creates an archive without resetting scores.

An optional name can be assigned to the archive.

The archive management section allows you to:

- Browse saved archives.
- Search archives by name.
- Filter archives by type.
- Sort available archives.
- Inspect archive metadata.
- Preview historical leaderboard results.
- Restore an eligible archive.

Opening or closing a historical preview does not change the current leaderboard.

### Restoring an archived leaderboard

1. Select an archive and inspect its preview.
2. Finish or cancel any active round.
3. Click **Restore Leaderboard**.
4. Read the warning and enter `PRZYWROC`.
5. The engine validates the archive and creates a **safety archive of the current leaderboard** before applying the restoration.
6. Verify the restored leaderboard and statistics.

Restoring an archived leaderboard does **not** restore the entire quiz configuration, question templates, overlay settings, or complete active session.

Restoration may be blocked if the current team configuration is incompatible with the selected archive.

Archives, snapshots, and full backups preserve different scopes of data.

If the panel reports an **unconfirmed restoration or reset result**, do not immediately repeat the operation.

First inspect the current leaderboard and use the available restoration status check.

If necessary, consult the Streamer.bot logs.

### Manual archive deletion

The panel supports manual archive cleanup.

You can select eligible archives for deletion through the panel.

Before deleting any archive:

1. Select the archives to remove.
2. Request a cleanup preview.
3. Review the selected files.
4. Check the number and total size of the archives.
5. Enter the required confirmation phrase:

```text
USUN ARCHIWA
```

The engine performs additional validation before deleting the selected files.

Protected archives cannot be removed through the normal cleanup mechanism.

The engine also prevents deletion of all available leaderboard archives.

**Archive deletion is destructive and should not be treated as an ordinary reversible operation.**

Create a separate backup before removing important archives.

### Automatic archive cleanup

LukiQuizEngine also provides optional automatic archive retention settings.

The following options are available:

- Enable or disable automatic cleanup.
- Configure the maximum archive count.
- Configure the maximum archive age in days.
- Optionally include manually created archives.
- Optionally include safety archives.

Automatic cleanup is **disabled by default**.

The default configuration uses a maximum count of `100` and a maximum age of `365` days when the feature is enabled.

Manual archives and safety archives are excluded by default.

Automatic cleanup is triggered during specific leaderboard archive and reset operations.

It is **not** a continuously running background service.

Before activating or expanding the cleanup policy, review its preview and make a separate backup.

The engine protects certain archives, including the newest available archive and archives referenced by unfinished operations.

### Archive integrity and limitations

Newly created archives include a `ContentHash` field used to detect accidental corruption or unexpected content changes.

For compatibility, supported older archives without this field may still be accepted.

A standard SHA-256 hash is **not a digital signature**.

It does not protect against someone who can intentionally modify archive files and their associated hash information.

## Where data is stored

| Data | Storage location / mechanism |
| --- | --- |
| Current V2 state and leaderboard | Persistent Streamer.bot variable `localQuiz_data_v2` |
| Public state used by the overlay | Streamer.bot variable `localQuiz_public_v2` |
| Automatic snapshots | Streamer.bot variable `localQuiz_snapshots_v2` |
| JSON leaderboard archives (automatic, manual, safety) | `%LOCALAPPDATA%\LukiQuizEngine\RankingArchives` |
| Interface preferences and browser draft | Browser `localStorage` |
| Question settings presets | Browser `localStorage` |
| Stream Protection setting | Browser `localStorage` |
| Streak and Achievements settings and progress | Additional fields in `localQuiz_data_v2` |
| Full-state backup | File exported from the web panel on request |
| WebSocket password | Memory of the running client and, for OBS, the fragment of the full overlay URL |

Variables associated with `v1` belong to the older engine or migration process.

They are **not the primary leaderboard storage location for V2**.

Do not delete them without creating suitable backups and ensuring that migration has been completed successfully.

The AppData archive directory is associated with the current Windows user.

If you move LukiQuizEngine to another computer or Windows account, copy the required archives separately.

Replacing the HTML and C# files does not automatically transfer locally stored archives or presets saved in browser storage.

## Security and known limitations

> [!WARNING]
> **Known security limitation: Streamer.bot HTTP `/DoAction`.**
>
> The current LukiQuizEngine version does not provide independent authentication for every administrative Action invocation made through the HTTP API.

### Required local configuration

- **HTTP Server:** `127.0.0.1:7474`, without a public tunnel, reverse proxy, or forwarded port.
- **WebSocket Server:** `127.0.0.1:8080/`, with `Authentication = ON`, `Enforce = ON`, and a strong, unique password.
- Do not configure HTTP or WebSocket to listen on `0.0.0.0`.
- Do not share the complete OBS browser source URL or files containing private viewer information.

### Important security information

The built-in Streamer.bot HTTP Server exposes the `POST /DoAction` endpoint.

This endpoint allows clients to invoke Actions and pass arguments.

**WebSocket Authentication and Enforce do not automatically protect this separate HTTP endpoint.**

A client with access to the HTTP API may potentially invoke administrative LukiQuizEngine operations **as well as other Actions configured in Streamer.bot**.

Restricting the HTTP Server to `127.0.0.1` significantly limits network exposure, but it is not equivalent to independent authentication.

It does not protect against untrusted local processes.

Some scenarios involving requests from external websites to local services also depend on the browser and server configuration.

**LukiQuizEngine does not claim complete protection against such requests.**

Therefore, **never expose the built-in Streamer.bot HTTP Server to untrusted devices or users**.

Do not assume that the HTTP API is safe to use as a publicly accessible administrative interface.

Official documentation:

- [HTTP DoAction](https://docs.streamer.bot/api/http/requests/do-action)
- [HTTP Server](https://docs.streamer.bot/api/http/guide/configuration)
- [WebSocket Server](https://docs.streamer.bot/api/websocket/guide/configuration)

### Password in the overlay URL

The **Copy Overlay URL** button includes the WebSocket credential in the URL fragment after `#`.

This fragment is not normally sent as part of an HTTP request.

However, the complete URL is stored in the OBS browser source configuration.

Treat the full overlay URL as sensitive credential information.

If it is exposed, change your WebSocket password and generate a new overlay URL.

### Important security and data durability limitations

- Panel diagnostics cannot independently prove that the configured servers are accessible only through localhost. Verify the actual addresses in Streamer.bot.
- Full atomicity between JSON archive files and Streamer.bot persistent storage has not been independently confirmed in scenarios involving sudden power loss.
- Local leaderboard data and archives may contain Twitch usernames and user identifiers. Do not publish these files accidentally.
- Built-in safety backups reduce the risk of losing data but do not replace a separate backup of your computer.
- Automatic archive cleanup and manual deletion should be used carefully because they can permanently remove saved files.
- Stream Protection helps prevent accidental operations in the ADVANCED interface but does not resolve the HTTP `/DoAction` security limitation.
- Full backup and snapshot restoration may be blocked when active Streak and Achievements settings or progress exist and the selected backup is incompatible.
- Presets and some interface preferences are stored locally in the browser. Clearing browser storage may permanently remove them.

## Upgrading an existing installation

### From v2.1.0 BETA to v2.2.0 STABLE

1. Finish or cancel the active round.
2. Export the current leaderboard as CSV and create a full-state backup.
3. Preserve the previous installation directory and any important leaderboard archives.
4. Replace the C# code in the same `QUIZ - Silnik` Action with the new `QuizEngine.cs`.
5. Click **Find Refs**, followed by **Save and Compile**.
6. Replace `panel.html` and `overlay.html` together.
7. Keep the existing Twitch message trigger and all persistent V2 variables.
8. Refresh the browser and OBS browser source caches if necessary.
9. Confirm that the panel and engine display version `2.2.0` with the identifiers `PHoeniX` and `QUPA`.
10. Verify your existing leaderboard, round history, templates, question sets, and archives.
11. Test panel modes, language switching, Twitch voting, and the `!punkty` and `!ranking` commands.
12. Check the new ADVANCED settings. Streak and Achievements should remain disabled when loading a 2.1.0 state.
13. If you intend to use the new competitive systems, test their tracking, persistence, and round undo behavior separately.

Do not delete persistent V2 variables or existing archives as part of the update.

Keep the same panel host and port if you want to preserve `localStorage` data, including interface preferences and presets.

Before restoring an older full backup, review the restrictions affecting Streak and Achievements data.

See [MIGRATION.md](MIGRATION.md) for detailed upgrade and restoration rules.

### Historical upgrade from v2.0.2 BETA to v2.1.0 BETA

The following instructions describe the historical transition between earlier releases.

1. Finish or cancel the active round.
2. Export the current leaderboard and a complete state backup.
3. Preserve the previous installation directory and any important ranking archives.
4. Replace the C# Action code with the new `QuizEngine.cs`.
5. Replace `panel.html` and `overlay.html` together.
6. Click **Find Refs** and **Save and Compile** in Streamer.bot.
7. Confirm that the engine and panel display version `2.1.0` with the identifiers `PHoeniX` and `QUPA`.
8. Refresh the browser and OBS browser source caches if necessary.
9. Verify your existing leaderboard, templates, question sets, and archives.
10. Test panel modes, language switching, and Twitch voting.
11. Keep automatic archive cleanup disabled until you have verified the existing archives and created a separate backup.

Do not delete persistent V2 variables or existing archives as part of the update.

### From v2.0.0 or v2.0.1 BETA

Before updating:

1. Finish or cancel the active round.
2. Export the current leaderboard and a full-state backup.
3. Preserve the previous installation directory.
4. Update all three main files together: `QuizEngine.cs`, `panel.html`, and `overlay.html`.
5. Run **Find Refs** and **Save and Compile**.
6. Verify the version and build identifiers.
7. Keep the existing V2 persistent variables and archives.
8. Test the quiz and archive features using non-production data.

Review the changelog and [MIGRATION.md](MIGRATION.md) for intermediate changes and the rules for upgrading to 2.2.0.

### From v1.3.0

Before replacing files, read [MIGRATION.md](MIGRATION.md).

Migration history and known older-version limitations are documented in that separate guide.

Do not migrate while a quiz round is active.

## Troubleshooting

| Problem | What to check |
| --- | --- |
| The page displays `404 Not Found` | Confirm the HTTP Server is running, the `quiz` mapping exists, and the mapped folder contains `panel.html` |
| The panel opens but cannot connect | Check WebSocket at `127.0.0.1:8080`, the password, Authentication, Enforce, and whether the server was restarted after configuration changes |
| The panel connects but the Action does not respond | Check the exact Action name `QUIZ - Silnik`, the current C# code, **Find Refs**, and **Save and Compile** |
| Votes such as `!1` are not working | Check Twitch connectivity, the message trigger, whether voting is open, and whether the answer number is valid |
| Additional text after a voting command is rejected | Check whether the vote parsing mode is `STRICT`, `WHITESPACE`, or `ATTACHED` |
| The `!punkty` or `!ranking` commands do not respond | Check the active command names saved in ADVANCED and their cooldowns |
| Round announcements are not appearing | Check whether the option is enabled in ADVANCED and whether settings were saved |
| Streak or Achievements progress is not being tracked | Check the corresponding switches in ADVANCED, whether a new round was started after enabling them, and whether the round qualifies |
| A full backup or snapshot cannot be restored | Check for active or saved Streak and Achievements progress and whether the backup is compatible |
| The overlay displays demo content or outdated information | Remove `demo=1`, use the URL copied after authentication, and check the browser cache and HTTP mapping |
| The overlay is not using the expected language | Check the language synchronization setting and the overlay language configuration |
| The archive folder does not open | Check Windows, AppData permissions, File Explorer, and the Streamer.bot logs |
| An archive cannot be restored | Finish the round, check team configuration compatibility and leaderboard revision, then reopen the preview |
| Archive cleanup cannot proceed | Check for an active round, protected archives, expired cleanup previews, incomplete operation records, or damaged archive files |
| Automatic cleanup is not running | Verify that it is enabled and that an operation triggering cleanup has occurred |
| A save or restore result is uncertain | Do not blindly repeat a destructive operation. Inspect the current leaderboard and operation status |
| The quiz cannot finish a round after a scoring error | Check `autoFinalizeError` diagnostics, deliberately cancel the problematic round if appropriate, and verify the score data |

## Optional voting modification for v1.3.0

<details>
<summary><strong>Archived guide - additional text after !1 ... !12 commands</strong></summary>

This guide applies to **LukiQuizEngine v1.3.0 only**.

It is preserved for users who still operate the older release.

Version **2.2.0** includes configurable `STRICT`, `WHITESPACE`, and `ATTACHED` vote parsing modes.

Do not replace the regular expressions in the current 2.2.0 engine with the examples below.

By default, version 1.3.0 required votes to be standalone commands:

```text
!1
!2
!3
```

### Option 1 - Additional text after whitespace

Accepts:

```text
!1 hello
```

Does not accept:

```text
!1hello
```

Regular expression:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?:\s+.*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

### Option 2 - Additional text without required whitespace

Accepts:

```text
!1hello
```

However, it may also recognize the beginning of other commands, such as:

```text
!1v1
```

Regular expression:

```csharp
private static readonly Regex VoteRegex =
    new Regex(@"^!(1[0-2]|[1-9])(?![0-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

In version 1.3.0, users could replace only the `VoteRegex` value in `QuizEngine.cs`, choosing one of the two variants.

After making the modification, they needed to click **Save and Compile**.

This was an optional modification rather than the default behavior.

When other Twitch commands could conflict with voting, the whitespace-based variant was generally preferable.

</details>

## Project history and license

- [CHANGELOG.md](CHANGELOG.md) - release history and changes introduced in LukiQuizEngine V2, including version 2.2.0 STABLE.
- [MIGRATION.md](MIGRATION.md) - upgrade instructions from older versions to 2.2.0.
- [READMElegacy.md](1.3.0/READMElegacy.md) - archived documentation for version 1.3.0.
- Older versions of the main README are available through the [GitHub file history](https://github.com/Lukituki6/LukiQuizEngine-for-Streamer.bot/commits/main/README.md).

Historical installation instructions for v1.3.0 should not be treated as current installation instructions for 2.2.0.

**AI assistance:** This project has also been developed with the assistance of artificial intelligence tools, including code analysis and development support.

All important modifications should be tested in the intended Streamer.bot environment.

The project is distributed under the [MIT License](LICENSE), **as is**, without any warranty.

You use it at your own risk, especially when changing configuration settings, importing data, or performing archive management operations.
