# Current progress

- Goal: prepare the current WPF sBot manager for trusted Discord sharing as a portable ZIP.
- Current development build: `C:\Users\juma\Desktop\sBot Manager\SBotManager.exe` (self-contained, portable; deployed 2026-09-26). It is version `0.0.2.0`; `portable.flag` and the local `data/` folder are present.
- Current runtime: The selected development-build folder includes the automatic-clientless launch correction, Activity-log scrollbar styling, compact character-card grid, and verified lifecycle-status labels.
- Source control: verified project source and documentation are pushed to `https://github.com/Juma1988/sbot_manager` on `main`.

## Current behavior

- Windows startup launch mode defaults to **Launch 1-1** and can be changed to **Launch all** in Settings.
- Long-press **Launch 1-1** to set its persistent 1–300 second spacing; normal click remains sequential launch.
- Training cards refresh from sBot's verified Start/Stop control after a command, so the action changes between **Start training** and **Stop training** without terminating sBot.
- Settings offers opt-in automatic clientless-after-game-entry. It enables sBot's native checkbox before Start Game while preserving each bot's native delay. **Go clientless all** is the separate immediate action above **Terminate all**.
- Each account card's bottom action reads **Launch** for a stopped bot and **Terminate** for a verified running bot. Terminate uses normal close with force fallback; training controls remain separate.
- Double-click an account avatar to exclude/include it in bulk actions. Disabled accounts are visibly dimmed, struck through, and labeled **DISABLED**, but retain individual controls.
- The character workspace scrolls normally without showing a scrollbar; the **LIVE PROCESS CONTROLS** label has been removed.
- Saving automatic clientless now synchronizes its native sBot setting both on and off for running bots; future Manager launches synchronize it before Start Game.
- With automatic clientless On, Start Game uses an explicit native setting and blocks on a failed set or readable mismatch. With it Off, Launch leaves the native clientless setting untouched and starts the game normally.
- Character Settings now accepts pasted or typed local absolute `.exe` paths, alongside Browse selection.
- Removed the redundant aggregate counts and default lifecycle notice; per-card badges beside the gear provide account state, while warnings remain visible only when needed.
- The Activity-log scrollbar uses the app’s dark track, muted-blue thumb, mint hover state, and visible keyboard-focus border instead of the Windows-default scrollbar.
- Character cards retain a compact two-column grid with matching horizontal/vertical spacing and show red **STOPPED**, launch/game-command phases, client-window verification, and training-control state separately. They do not fabricate login, character, EXP, or farming status.

## Verification

- Current source: `dotnet build src/SBotManager/SBotManager.csproj -warnaserror` passed with zero warnings/errors after the clientless launch correction.
- Model/native fixture smoke checks: **97 passed**, including the automatic-clientless safety block and normal Start Game when clientless synchronization is unavailable but disabled.
- Desktop UI smoke checks: **40 passed**, including removal of the redundant workspace summary.
- Latest card/status verification: build passed with zero warnings; **100 model/native** and **42 desktop UI** checks passed. The deployed development executable checksum matches the verified self-contained build.
- Release preflight on 2026-09-26: **CONDITIONAL PASS** for trusted Discord sharing. The exact `v0.0.2` portable ZIP passed 40 desktop UI checks after extraction; its SHA-256 is `70E230835FD7E1DE99155D86A18832ABE6BCB473893A55547DB7B6544C99EED7`.

## Remaining verification / limitations

- After Windows restart, optionally verify the configured `SBotManager Startup` scheduled task behavior on the real machine; automated checks did not register it.
- XP/game-health telemetry remains unavailable until a supported sBot API/export/log source is verified. No memory scanning or fabricated values.
- The existing v0.0.1 ZIP is an earlier standalone artifact; it is not the current development build and was not repackaged in this session.
- The final automatic-clientless launch correction is deployed. Next action: reopen `C:\Users\juma\Desktop\sBot Manager\SBotManager.exe` and verify a Windows-startup launch starts each sBot's client.
- Release version: **0.0.2** is configured across application, installer, and release documentation; build metadata verifies `0.0.2.0`.
- Remaining recommended checks: launch the ZIP on a clean/second Windows machine and perform a live sBot/Silkroad workflow check. These do not block limited trusted Discord sharing. Details: `docs/qa/release-readiness-2026-09-26.md`.
