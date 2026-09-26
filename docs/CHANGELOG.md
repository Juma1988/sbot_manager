# Changelog

## 2026-09-26

### Changed
- Moved each account card's primary action to the bottom and made it switch between **Launch** for stopped bots and **Terminate** for verified running bots.
- Added a persisted avatar double-click toggle for per-character bulk eligibility. Disabled cards are dimmed, struck through, and labeled **DISABLED** while retaining individual controls; they are skipped by bulk commands.
- Removed the **LIVE PROCESS CONTROLS** workspace label.
- Hid the character-list scrollbar while preserving normal scrolling.
- Turning off **Switch to clientless after game entry** now explicitly disables sBot's verified native setting for running bots and future Manager launches.

### Technical
- Serialized in-memory activity-log writes to prevent concurrent lifecycle tasks racing its bounded collection.
- Verified with 95 model/native checks and 41 desktop UI checks, then published and started the updated self-contained development build at `C:\Users\juma\Desktop\sBot Manager`.
- Initialized source control and published the verified project source and documentation to the GitHub `main` branch.

## 2026-09-25

### Changed
- Added opt-in global automatic clientless mode in Settings. It enables each Manager-launched sBot's native **Switch to clientless mode when entered game after** control before Start Game, preserving the delay configured in sBot. Added **Go clientless all** above **Terminate all** for a separate immediate native command to verified running bots.
- Settings now includes **Launch mode** for the Windows-startup bot option: **Launch 1-1** (default, using the saved sequential-launch spacing) or **Launch all**.
- Long-pressing **Launch 1-1** now opens a validated, persistent editor for its 1–300 second launch spacing; a normal click still launches sequentially.
- The per-account training action now waits briefly for sBot's verified Start/Stop control after a native command, preventing stale Stop-training labels after training is stopped.
- Set the public-preview version to **0.0.1** and added global **About & Support** with Ko-fi, InstaPay Egypt, and support-email actions.
- Cleared the local account roster and replaced project test data with generic account names; the original user baseline remains preserved.
- Settings now uses a two-column grouped layout while remaining content-sized and free of an internal scrollbar.
- The account-card status badge now distinguishes **TRAINING**, **READY**, and **RUNNING** from verified native controls, and cards show their latest recovery event.
- Restored configurable Windows notifications in Settings: a master switch plus event switches for recovery limit, unexpected stop, unverifiable bot, and force termination.
- Added **Terminate session bots when exiting** to Settings. When on, every true Manager exit closes only sBots launched by that Manager session (normal close, then force fallback); when off, they keep running. The default is off and pre-existing bots are never targeted.
- When exit termination is Off, Manager now restores only its session-launched sBot windows before closing; related Silkroad client-window visibility is unchanged.
- Renamed the visible application title, Settings dialog, tray text, notifications, and startup guidance to **i1988 - sBot manager** without changing the internal `SBotManager` persistence or Scheduled Task identities.
- Renamed the per-account **Stop bot** control to **Stop training** to match its existing native behavior: stop training only and keep sBot open.
- The per-account training action now changes dynamically between **Stop training** and **Start training** after native state refresh, rather than leaving a stale Stop label.
- Replaced Settings checkboxes with compact slate/mint switches that include explicit OFF/ON text and a thumb that slides over 180 ms, plus hover, press, and keyboard-focus feedback while preserving preference persistence and the guarded Windows-startup dependency.
- Settings now derives its height exclusively from its content, cannot be stretched beyond it, and has no internal scrollbar.
- Added Windows startup preferences: **Start Farm Manager with Windows** creates an elevated per-user Scheduled Task, and **Launch all bots after Windows startup** launches only configured stopped bots in that Windows-startup session. The bot option is disabled until Manager startup is enabled.
- Simplified **Settings** to workspace behavior only. Windows notification controls were removed from the dialog while existing notification defaults remain active at runtime.
- Rebuilt **Settings** as a clear configuration workspace. Close to tray, Auto-hide sBot, and Auto-hide SRO client moved out of the sidebar into a Workspace behavior section. Save applies all preferences together; Cancel makes no changes.
- Added persistent **Auto-hide sBot** and **Auto-hide SRO client** preferences. Both default to On; they separately hide only verified sBot or related Silkroad windows after launch, without changing the manual visibility controls.
- Added **Show all bots** and **Hide all bots** below **Stop training all** and above the client visibility controls. They act only on verified running sBot main windows.
- Reconciled project documentation with the live Windows sBot integration; superseded simulation-only records.
- Added a durable maintenance rule: meaningful user-visible work updates runtime activity context, `docs/progress.md`, and this changelog.
- In-app Exit manager and normal window close now exit immediately without a warning dialog; Close to tray behavior is unchanged.

### Technical
- Verified the clientless update with 90 model/native fixture checks and 39 desktop UI checks. Published the development build directly to `C:\Users\juma\Desktop\sBot Manager`; no new ZIP release was created.
- Added a desktop UI assertion that Settings exposes no scrollbar and keeps Save within the dialog bounds; the isolated build passed 81 model/native and 37 desktop UI checks.
- Current verification evidence: 81 model/native checks and 36 desktop UI checks passed from an isolated build output because the normal app executable was in use. Scheduled-task registration was intentionally not invoked during verification.

## 2026-09-24

### Added
- Simulation-only WPF dashboard for four sBot accounts, individual and bulk lifecycle controls, bounded session activity log, and reset/help actions.
- Root `Launch Sample.cmd` builds and opens the preview.
- Dependency-free model smoke tests and Windows UI Automation smoke checks.
- Live sBot integration: path + PID verified launch, native Start/Stop training buttons, graceful/forced termination, hide/show window, opt-in recovery with retry budget, fail-closed behavior, local JSON settings with atomic saves. (Earlier session work, consolidated here.)
- `Clear log` button next to Copy log / Export in the activity panel.
- Double-click an activity entry to copy that entry to the clipboard (same format as Copy log).
- Native fixture verifies sBot's `Start Game!` control (`START`/`STOP`/`GAME` command log). Manager automatically clicks it after launching sBot.
- App icon: user-supplied PNG converted to a 7-size ICO (16–256), embedded in the exe, used as the main-window icon and tray NotifyIcon.
- Left sidebar redesigned: buttons only, grouped by divider lines — Training (Start/Stop all), Applications (Open bots / Terminate all), System (Close to tray toggle with On/Off state, Help / safety, Exit manager…). Brand wordmark, section labels, helper note and version moved into the app icon area and button tooltips.
- Windows notification settings: master on/off switch plus per-event toggles and editable messages (with `{Account}` placeholder) for retry budget used, close to tray, unexpected bot stop, unverifiable bot, and force-close needed. Stored in `settings.json`. Events fire from `Dashboard.Notify` and balloons are suppressed when the master switch or the event's rule is off or the message is blank.
- App window now starts maximized; sidebar icon sized 112 px; Settings dialog fits its content with no scrollbar; divider line below the app icon.
- Launch behavior: Manager automatically waits for and clicks sBot's Start Game control after launching each bot. It retries native clicks for up to 60 seconds while sBot finishes initializing; this runs in the background, so Launch all advances to the next configured bot without waiting for the control.

### Changed
- Bulk **Launch all** now starts eligible sBots, waits for their login-created Silkroad clients, then hides all top-level windows in each launched sBot process tree. Processes continue running; unrelated applications are never targeted.
- Added **Launch 1-1** directly below Launch all in the same sidebar group. It launches eligible clients sequentially with a 20-second spacing, hiding each client during its spacing window and making a final pass for late Silkroad windows.
- Added separate per-account Hide/Show bot and Hide/Show client controls. Client visibility is available only after a related Silkroad descendant window is verified; it never targets unrelated applications.
- Tray now has a refreshed **Live bots** submenu listing only session-launched running characters; tapping one restores its verified `SBotP` main window. Tray Exit has no confirmation and closes only session-launched sBots: normal close first, then automatic force-close. Pre-existing bots and Silkroad client processes are never directly targeted.
- Improved runtime clarity: card labels report verified game/training controls rather than unverified game state; the summary distinguishes session-launched from pre-existing running bots. Live bots includes Show all session bots. Activity log filters were removed to keep the full history visible.
- Added All-actions client controls: Show all clients and Hide all clients act on every verified related Silkroad client. They sit in their own divided section before the final Terminate all action.
- Bulk launch appears above bulk Start training; its confirmation is removed. Tray Exit exits immediately without a confirmation.
- Start training all, Stop training all, and Terminate all now run without confirmation dialogs. Terminate all tries normal close, then force-closes only failed sBots.
- Removed all seeded mock accounts: `SettingsStore.Defaults()` returns an empty roster; first run starts with the "+ Add account" card and `0/10` summary.
- Card grid fixes: `+` add card now uses a fresh instance per sync — fixes a WPF ItemContainerGenerator bug that silently dropped the add card after repeated saves.
- Sidebar version label bumped to `LOCAL WORKSPACE / 0.4`.

### Fixed
- Launching a real sBot failed with "The requested operation requires elevation" (Win32 740): sBot asks Windows for administrator rights. Launch now reports an actionable message and the manager must be run as administrator (right-click → Run as administrator) to open bots.
- Native control discovery against the real sBot: `WindowFor` returned the first top-level window of the process (usually a hidden `tooltips_class32` helper), so training, Start Game, hide/show, and WM_CLOSE all targeted the wrong window. It now prefers the visible, titled main window. A captured control dump of the live sBot (`SBotP v1.0.51`) confirmed the real hierarchy (`ID_PANEL1` → `Start Game!`, `ID_PANEL2` → `Start training`/`Stop training`) and that the buttons were present and enabled all along.

### Improved
- UI smoke script: first-run empty-state checks; deterministic file-picker selection (precise focus + retried Enter); built-in diagnostics on add-card failure; 76 model + 33 UI checks currently passing, including delayed native Start Game acceptance, all-client visibility actions, and no-confirmation Terminate all behavior.
- Account settings Browse now opens in the currently configured folder when it exists.

### Removed
- Per-account Start bot and Start game buttons; Manager now starts the game automatically after sBot launch.
- Temporary diagnostic `tests/probe-*.ps1` scripts (superseded by built-in ui-smoke diagnostics).
- Seed demo accounts and the `AddCard.Instance` singleton.

### Technical
- Build verified with zero warnings/errors on .NET 10.0.401 (winget-installed, hash verified).
- BotFixture now mimics the real sBot's window layout: a hidden, untitled topmost helper/tooltip window plus the titled main form — the native smoke tests prove `WindowFor` picks the real window and finds all controls through layers of panels.
- `ShowHideAsync` show/hide uses `ShowWindowAsync` with poll-verified hide; `Inspect` exposes `WindowVisible`.
- No live bot operations, specialist QA, publication, or full accessibility audit performed.
