<div align="center">
  <img src="src/SBotManager/Assets/app-icon.png" width="140" alt="i1988 sBot manager icon">

  # i1988 — sBot manager

  **A focused Windows control center for your local sBot accounts.**

  Launch, hide, train, recover, and manage up to ten configured sBot instances from one local-first workspace.

  `Windows 10 / 11` &nbsp;·&nbsp; `.NET 10` &nbsp;·&nbsp; `WPF` &nbsp;·&nbsp; `Local settings only`
</div>

---

## Why this exists

Running several sBot accounts should not mean hunting through windows or guessing what a bulk action will affect. **i1988 — sBot manager** keeps the important controls in one desktop app while deliberately staying conservative around game state:

- verifies the configured executable path and the live sBot process before acting;
- launches accounts all at once or one by one with a saved delay;
- automatically sends sBot’s native **Start Game** command when its verified control becomes available;
- lets you hide/show verified sBot and related Silkroad client windows independently;
- records local activity so failures and actions are visible later.

> [!IMPORTANT]
> A successful command is **not proof** that a character connected, entered the game, trained, or farmed. The Manager never invents EXP or game-health telemetry.

## Highlights

| Control | What it does |
| --- | --- |
| **Launch all / Launch 1-1** | Starts eligible accounts together or sequentially. Long-press **Launch 1-1** to set a persistent 1–300 second spacing. |
| **Automatic Start Game** | Waits up to 60 seconds for sBot’s verified native control and starts the game without delaying other bulk launches. |
| **Per-account control** | Launch or terminate one bot; start/stop training; show or hide that bot and its related client windows. |
| **Bulk eligibility** | Double-click an avatar to exclude that character from bulk actions while keeping individual controls available. |
| **Clientless options** | Optionally enable sBot’s native automatic-clientless setting after game entry, or use **Go clientless all** as a separate immediate command. |
| **Safe lifecycle handling** | Termination tries normal close before a force fallback; optional recovery is bounded to prevent loops. |
| **Windows startup** | Start Manager at logon and optionally launch enabled bots using your chosen launch mode. |
| **Local activity log** | Review, copy, export, or clear locally stored action and error history. |

## Get running

### Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) for development
- A local sBot executable for every account you configure

Run the Manager **as administrator** if the relevant sBot installation requires elevation.

### Run from source

```powershell
git clone https://github.com/Juma1988/sbot_manager.git
cd sbot_manager
dotnet run --project src/SBotManager/SBotManager.csproj
```

Then use **Add account** and select (or paste) the local absolute path to that account’s sBot `.exe` file. Network paths, web URLs, and relative paths are rejected intentionally because process verification requires a local executable.

### Build a standalone Windows installer

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php).
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-release.ps1 -Version 0.0.2
```

The script creates a self-contained x64 installer and stores the installer definition and SHA-256 checksum in `releases\0.0.2\`. See [installer/README.md](installer/README.md) for the release-artifact details.

### Share a version with other people

Use a **[GitHub Release](https://docs.github.com/repositories/releasing-projects-on-github/managing-releases-in-a-repository)**, not GitHub Packages. Create a version tag such as `v0.0.2`, upload the installer and/or portable ZIP as release assets, include the matching SHA-256 checksum, and add concise release notes. GitHub Packages is for developer dependencies such as NuGet or Docker packages, not downloadable Windows applications.

## Daily workflow

1. **Configure accounts** — add up to ten local sBot executables.
2. **Choose bulk participants** — double-click an account’s two-letter avatar to include or exclude it from bulk actions.
3. **Launch** — use **Launch all** or **Launch 1-1**. Each successful Manager launch queues Automatic Start Game in the background.
4. **Keep the desktop tidy** — enable auto-hide in Settings, or use the show/hide controls whenever you need a window back.
5. **Review activity** — use the Activity log for command results, warnings, and recovery events.

## Settings & local data

The Manager stores local settings only—**never account credentials**.

| Installation | Settings location |
| --- | --- |
| Standard run | `%LocalAppData%\SBotManager\settings.json` |
| Portable build (`portable.flag` beside the app) | `data\settings.json` beside `SBotManager.exe` |

Available settings cover close-to-tray behavior, auto-hide behavior, Windows startup, startup launch mode, clientless-after-game-entry, session termination on exit, and notifications. Activity logs rotate daily, are capped at 2 MB per day, and are retained for 14 days.

## Safety boundaries

This project intentionally does **not**:

- store account credentials;
- click unknown screen coordinates;
- act on a process it cannot verify against the configured executable;
- fabricate connection, training, EXP, or farming status;
- scan memory for game telemetry.

Related Silkroad windows are identified only when they can be verified in the launched sBot process tree.

## Verify changes

Run the full local verification suite before distributing a build:

```powershell
dotnet build src/SBotManager/SBotManager.csproj
dotnet run --project tests/SBotManager.SmokeTests/SBotManager.SmokeTests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File tests/ui-smoke.ps1
```

The model/native fixture suite covers lifecycle, persistence, recovery, and native-control boundaries. The UI smoke suite uses Windows UI Automation for desktop workflows.

## Project map

```text
src/SBotManager/              WPF application
src/SBotManager/Models/       UI state and lifecycle rules
src/SBotManager/Services/     Windows process, persistence, logs, startup integration
installer/                    Self-contained build and installer tooling
```

## Support

Use **About & support** in the app or email [i.juma1988@gmail.com](mailto:i.juma1988@gmail.com).

---

<div align="center">
  Built for a cleaner local sBot workflow—not a fake telemetry dashboard.
</div>
