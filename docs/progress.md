# Current progress

- Goal: live C#/.NET 10 WPF sBot farm manager for up to 10 configured accounts.
- Current development build: `C:\Users\juma\Desktop\sBot Manager\SBotManager.exe` (self-contained, portable; published 2026-09-26). `portable.flag` and the local `data/` folder are present.
- Current runtime: The updated development build is running from the selected development-build folder.
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
- Character Settings now accepts pasted or typed local absolute `.exe` paths, alongside Browse selection.
- Removed the redundant aggregate counts and default lifecycle notice; per-card badges beside the gear provide account state, while warnings remain visible only when needed.

## Verification

- Current source: `dotnet build` passed with zero warnings/errors after the automatic-clientless fix.
- Model/native fixture smoke checks: **95 passed**, including automatic-clientless enable/disable synchronization.
- Desktop UI smoke checks: **40 passed**, including removal of the redundant workspace summary.

## Remaining verification / limitations

- After Windows restart, optionally verify the configured `SBotManager Startup` scheduled task behavior on the real machine; automated checks did not register it.
- XP/game-health telemetry remains unavailable until a supported sBot API/export/log source is verified. No memory scanning or fabricated values.
- The existing v0.0.1 ZIP is an earlier standalone artifact; it is not the current development build and was not repackaged in this session.
