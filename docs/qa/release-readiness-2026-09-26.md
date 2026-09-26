# Release-readiness assessment — 2026-09-26

## Candidate assessed
- Source revision: `e68ed0b3c6e2ffd94f4fc0e1524e3230aa84034f` (`Prepare 0.0.2 release candidate`).
- Source tag: annotated `v0.0.2`, verified to point to that exact revision.
- Distribution target: private, trusted Discord sharing—not public store/web publication.
- Portable artifact: `releases/i1988-sBot-manager-0.0.2-win-x64-standalone.zip`.
- SHA-256: `70E230835FD7E1DE99155D86A18832ABE6BCB473893A55547DB7B6544C99EED7`.

## Kira verification
- `dotnet build src/SBotManager/SBotManager.csproj -warnaserror`: PASS, zero warnings/errors.
- Model/native smoke suite: PASS, 97 checks.
- Desktop UI automation suite: PASS, 40 checks.
- The model suite covers the recent normal-launch behavior: Start Game proceeds when automatic clientless is disabled even if the native clientless control cannot be synchronized.
- The exact extracted `v0.0.2` portable artifact passed the desktop UI automation suite: 40 checks.
- Artifact version metadata: file version `0.0.2.0`; product version `0.0.2+e68ed0b3c6e2ffd94f4fc0e1524e3230aa84034f`.
- The preserved SHA-256 file matches the ZIP byte-for-byte.

## Rei release gate
**CONDITIONAL PASS for private trusted Discord sharing.** No installer or code signing is required for this portable ZIP channel.

**Problem countdown: 2 recommended checks remaining** — neither blocks a limited trusted beta, but both should be completed before wider sharing.

| Countdown | Status | Requirement | Fix | Estimated effort* |
| --- | --- | --- | --- | --- |
| **✅ Complete** | ☠️ **MUST** | A unique release version was required. | Configured version `0.0.2`; build metadata confirms file version `0.0.2.0`. | Completed |
| **✅ Complete** | ☠️ **MUST** | Reproducible release source was required. | Committed `e68ed0b` and annotated tag `v0.0.2`. | Completed |
| **✅ Complete** | ☠️ **MUST** | A shareable artifact and checksum were required. | Built and preserved the self-contained portable ZIP and matching SHA-256 checksum. | Completed |
| **✅ Complete** | ☠️ **MUST** | Final artifact QA was required. | Extracted the exact ZIP and passed 40 desktop UI checks. | Completed |
| **2 → 1** | ⚠️ **WARNING** | No clean-Windows-machine launch check is recorded for this portable ZIP. | Extract the ZIP on another/clean Windows machine and confirm launch and settings creation. | Small — 15–30 min |
| **1 → 0** | ⚠️ **WARNING** | No live regression with a real configured sBot/Silkroad installation is recorded. | Exercise launch, Start Game, training, window visibility, and exit behavior with a real local setup. | Medium — 1–2 hr, dependent on access to a test account |

\*Effort estimates are hands-on work ranges, not promised elapsed-time estimates. Some work can overlap once the final artifact exists.

## Hygiene
- `TrainingStart_Click` and `Names()` in `MainWindow.xaml.cs` have no references. They are non-blocking dead-code candidates; do not remove them during this release assessment.

## Required path to READY
1. Complete the two remaining recommended checks before expanding beyond trusted Discord friends.
2. For eventual public distribution, add installer and code-signing work, then rerun release readiness for that new channel.
