# WPF sample verification — 2026-09-24

> Historical record. The simulation-only sample scope below was superseded by the live Windows sBot integration. Current verification is recorded in `docs/progress.md` and `docs/CHANGELOG.md`: 76 model/native checks and 33 desktop UI checks passed.

## Provenance
- Scope: historical simulation-only four-account WPF sample, current source after button-contrast correction.
- No Git repository/revision exists.
- Built DLL: `src/SBotManager/bin/Debug/net10.0-windows/SBotManager.dll`.
- SHA256: `B78D6AC376266E3C54501DA1654064128C8494B46D2983A4E0D3E1EEC9609C22`.
- SDK: 10.0.401; Windows x64.
- Direct execution evidence, **not independent Kira QA**: specialist agent invocation unavailable.

## Checks actually run
1. `dotnet build src/SBotManager/SBotManager.csproj --nologo`: PASS, 0 warnings, 0 errors.
2. `dotnet run --project tests/SBotManager.SmokeTests/SBotManager.SmokeTests.csproj`: 16 checks PASS.
   - Configured accounts, initial state, individual start, duplicate rejection, restart, bulk lifecycle.
   - Pending-start cancellation, interrupted bulk sequence, reset during launch, simulated startup failure, bounded logs.
3. `powershell -NoProfile -ExecutionPolicy Bypass -File tests/ui-smoke.ps1`: 17 checks PASS.
   - Actual WPF buttons exercised using UI Automation.
   - Individual start/stop/restart, enable/disable transitions, bulk launch, interrupted bulk launch, reset.
   - Account control visibility checked after minimum-size request using Win32 SetWindowPos.
4. Captured `sample-preview.png` from the application window and read it for visual inspection.
   - Corrected light text on mint primary buttons and reran build/model/UI checks.
   - Activity section scrolls at the default window size; complete page remains reachable through vertical scrolling.
5. Final process responding, left open with simulated accounts in mixed running/stopped states.

## Harness issue resolved
- First attempt used UI Automation TransformPattern.Resize, which Windows rejected. Replaced the harness resize operation with Win32 SetWindowPos; this was not an application crash. Fresh UI runs passed.

## Not verified / not implemented
- Real bot launch/stop, actual executable paths, game telemetry, watchdog recovery, window hiding, credentials and persistence: intentionally absent.
- Full keyboard/screen-reader review, every DPI/theme, packaging for other machines, and installer/release readiness: not claimed.
- No actual publication, live bot manipulation, or independent specialist gate.
