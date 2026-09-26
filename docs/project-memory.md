# Project memory

## Purpose and approved direction
- Display name: **i1988 - sBot manager**. Internal `SBotManager` namespaces, local-data path, and Scheduled Task identifier remain unchanged for compatibility with existing settings and startup registration.
- Public-preview version: **0.0.1**. Global About & Support offers Ko-fi (`https://ko-fi.com/i1988`), InstaPay Egypt, and support email; payment links open externally and no payment data is stored.
- A standalone portable release carries `portable.flag`; when present beside `SBotManager.exe`, the app creates and uses `data/settings.json` beside the executable. Normal installed copies still use `%LocalAppData%\SBotManager`.
- Ibrahim wants a Windows manager for GreatestSRO sBot accounts (up to 10 configured entries). The default and current local roster are empty; users add their own accounts.
- Approved stack: C# + .NET 10 + WPF. The product controls real, path-verified Windows sBot processes; it is no longer a simulation-only sample.
- Manager launches sBot, automatically sends Start Game after control readiness, controls training, manages verified bot/client window visibility, and supports normal/forced termination.
- Original supplied batch is preserved at the root in `sbot-farm-manager-baseline.md`.

## Architecture
- .NET 10 WPF app: `src/SBotManager`.
- Dependency-free lifecycle models and bounded in-memory/daily persisted activity log under `Models` and `Services/ActivityStore.cs`.
- WPF bindings for account state and summaries; thin UI event handlers.
- Windows process/window integration: `Services/WindowsBotService.cs`; all actions revalidate executable path, PID, and start time.
- Local persistence: `%LocalAppData%\SBotManager\settings.json`; activity logs under `%LocalAppData%\SBotManager\Logs\` (14-day retention, daily 2 MB rotation). No credentials are stored.
- Console model/native fixture smoke tests: `tests/SBotManager.SmokeTests`; desktop UI automation: `tests/ui-smoke.ps1`.

## Approved behavior and conventions
- Launch all and Launch 1-1 start eligible sBots. Start Game retries in the background for up to 60 seconds, so a late/unresponsive sBot UI does not block the next launch.
- Show all / Hide all bots affect only verified running sBot main windows; Show all / Hide all clients affect only verified related Silkroad client windows. Terminate all is the final destructive bulk action.
- The XP bar remains unavailable until a supported sBot/Silkroad telemetry source is verified; no XP value is fabricated.
- In-app Exit manager, normal window close, and tray Exit have no warning dialog. **Terminate session bots when exiting** controls whether they close only bots launched in the current Manager session (normal close, then force fallback); when Off, Manager instead restores only those session-launched sBot windows before exiting. Pre-existing bots and all Silkroad client-window visibility are always untouched. The preference defaults Off for safe upgrade behavior.
- Bulk Start training, Stop training, and Terminate run without confirmation. Terminate attempts a normal close, then force-closes only failures.
- The per-account training action sends native training commands only; it deliberately keeps sBot open. After a command it polls sBot's verified Start/Stop control for up to two seconds, then reads **Stop training** when active or **Start training** after stopping. **Terminate** is the process-ending action.
- Each account card's bottom action reads **Launch** while its sBot is stopped and switches to **Terminate** while it is verified running. Terminate follows the existing normal-close then force fallback; training remains a separate, non-terminating action.
- Double-clicking an account card's initial-avatar toggles its persisted bulk eligibility. Disabled accounts are visibly dimmed/struck through and excluded from user-initiated bulk launch, training, visibility, clientless, and termination controls, but all individual controls remain available.
- The character workspace remains scrollable by wheel/touchpad/keyboard, but its scrollbar is visually hidden. The **LIVE PROCESS CONTROLS** label is intentionally removed.
- **Switch to clientless after game entry** is an opt-in global preference. For Manager-launched bots it enables sBot's verified native automatic-clientless checkbox before Start Game without changing sBot's configured delay; saving the preference also applies it to verified running accounts. **Go clientless all** is a separate immediate command for verified running bots and appears directly above **Terminate all**.
- User-selected development-build destination: `C:\Users\juma\Desktop\sBot Manager`. Development builds update this extracted portable folder directly; ZIP packaging is reserved for explicit standalone/release requests.
- The account-card badge next to the gear reads **TRAINING** when sBot exposes Stop training, **READY** when it exposes Start training, and **RUNNING** only when no training state is verifiable. Each card also shows the latest crash/recovery event for the current Manager session; detailed history is persisted in the activity log.
- Windows notification Settings expose a master switch and per-event switches for recovery retry limit, unexpected stop, unverifiable bot, and force-termination requirement. Existing default messages are preserved.
- Settings is the single home for persistent behavior: Close to tray, **Auto-hide sBot**, **Auto-hide SRO client**, Windows startup, launch mode, automatic clientless-after-game-entry, exit behavior, and notifications. The auto-hide settings default to On; they hide only verified windows after launch and do not affect manual show/hide controls. Saving automatic clientless explicitly synchronizes the verified native setting on running bots in both directions; Manager launches also set the requested native state before Start Game.
- Settings uses compact, accessible switches for every persistent preference: a slate track with explicit OFF text, a mint track with explicit ON text, and a white thumb that slides right/left over 180 ms when enabled/disabled. Hover, press, and keyboard-focus feedback are included. This includes **Start Farm Manager with Windows**, its dependent bot-launch option, and the **Launch 1-1** (default) / **Launch all** mode selection. Enabling Manager startup creates the per-user `SBotManager Startup` Scheduled Task at logon with highest privileges, so it can operate administrator-only sBot. Bot auto-launch occurs only when that task passes `--windows-startup`, never on a normal manual Manager launch.
- Settings derives its height from its current content, has no internal scrollbar, and cannot be manually stretched beyond that content.
- Settings keeps its sections intact in two columns: Workspace behavior and Windows startup on the left; Exit behavior and Windows notifications on the right.
- Meaningful work must update: (1) the runtime activity log when a user-visible action/failure occurs, (2) `docs/progress.md` with current state and verification, and (3) `docs/CHANGELOG.md` with completed product/technical changes. Update this memory when durable behavior, architecture, constraints, or decisions change.

## Constraints
- A running process is not proof a character is connected or farming.
- A sent Start Game command is not proof that Silkroad connected; no supported game/EXP telemetry is integrated.
- Real client pairing is limited to verified windows in the launched sBot process tree.
- Source control: Git repository on `main`, tracking `https://github.com/Juma1988/sbot_manager.git`.
- Verification is developer/model/UI evidence, not an independent release QA gate.
- Activity-log mutations are serialized so concurrent lifecycle tasks cannot race its bounded in-memory collection.
