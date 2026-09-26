# i1988 — sBot manager

A Windows desktop manager for coordinating up to ten configured sBot accounts. It is built with **C#**, **.NET 10**, and **WPF**, with local-only settings and no stored credentials.

> **Safety first:** this app verifies the configured executable path and the live process before acting. A sent sBot command is not proof that a character connected, entered game, or is farming. EXP/game telemetry is intentionally unavailable until a supported source exists.

## Highlights

- **Individual control:** launch or terminate one bot; start/stop training; show or hide its verified sBot and related Silkroad windows.
- **Bulk control:** launch all, launch one-by-one with configurable spacing, training, visibility, clientless, and termination commands.
- **Choose bulk participants:** double-click a character's two-letter avatar to include or exclude it from bulk actions. Disabled cards are dimmed, struck through, and labeled **DISABLED**, while individual controls remain available.
- **Safer lifecycle handling:** termination tries a normal close before a force fallback; recovery is opt-in and bounded.
- **Automatic Start Game:** after launch, the app waits up to 60 seconds for the verified native control without delaying the next bulk launch.
- **Clientless options:** optionally enable sBot's native automatic clientless setting after game entry, or send **Go clientless all** to verified running bulk-enabled bots.
- **Local activity history:** user-visible actions and failures are recorded locally with daily rotation and retention.
- **Portable development build:** a `portable.flag` beside the executable stores data beside the app instead of in `%LocalAppData%`.

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) for development
- A locally installed sBot executable for each configured account

Run the Manager as administrator when the installed sBot instance requires elevation.

## Run from source

```powershell
dotnet run --project src/SBotManager/SBotManager.csproj
```

Add accounts from the UI, selecting each local sBot executable. The manager never launches an executable merely because it was selected.

## How controls work

| Control | Behavior |
| --- | --- |
| **Launch / Terminate** | The bottom card action is **Launch** when stopped and **Terminate** when verified running. Terminate attempts normal close, then force fallback only when needed. |
| **Start / Stop training** | Sends only the verified native training command. Stopping training does **not** close sBot. |
| **Avatar double-click** | Includes/excludes that character from all user-initiated bulk actions. It does not disable individual controls or stop a running bot. |
| **Launch 1-1** | Starts eligible accounts sequentially. Long-press it to set a persistent 1–300 second spacing. |
| **Show / Hide** | Affects only verified sBot windows or verified related Silkroad client windows. |
| **Terminate all** | Targets only bulk-enabled accounts. Each bot receives a normal close first, then a force fallback where required. |

## Settings and local data

Settings cover close-to-tray behavior, automatic hiding, Windows startup, startup launch mode, clientless behavior, session termination on exit, and notifications.

| Installation | Settings location |
| --- | --- |
| Normal run | `%LocalAppData%\SBotManager\settings.json` |
| Portable build (`portable.flag`) | `data\settings.json` beside `SBotManager.exe` |

Activity logs are retained locally for 14 days with a 2 MB daily rotation. No account credentials are stored by this application.

## Verify

Run the full local verification suite:

```powershell
dotnet build src/SBotManager/SBotManager.csproj
dotnet run --project tests/SBotManager.SmokeTests/SBotManager.SmokeTests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File tests/ui-smoke.ps1
```

The model/native smoke tests exercise lifecycle, persistence, safety, recovery, and native-control boundaries. The UI smoke test uses Windows UI Automation for key desktop workflows.

## Project layout

```text
src/SBotManager/              WPF application
src/SBotManager/Models/       UI state and lifecycle rules
src/SBotManager/Services/     Windows process, persistence, logs, startup integration
tests/SBotManager.SmokeTests/ Dependency-free model and native fixture checks
tests/ui-smoke.ps1            Desktop UI Automation checks
installer/                    Standalone build/installer tooling
docs/                         Project decisions, QA state, and change history
```

## Limitations

- A running sBot process is not proof that a character is connected or farming.
- Start Game delivery is not proof of a successful game connection.
- EXP/game-health telemetry is not implemented; the UI does not fabricate it.
- Related Silkroad windows are identified only through verified windows in the launched sBot process tree.

## Support

Use **About & support** inside the app for the current support options, or email [i.juma1988@gmail.com](mailto:i.juma1988@gmail.com).

The original supplied launch-script baseline is retained in [sbot-farm-manager-baseline.md](sbot-farm-manager-baseline.md).
