# Release-readiness assessment — 2026-09-26

## Candidate assessed
- Source base revision: `a7cb067e23496ccdcf19bf406640f338db4fc28b` (`Restore normal launch when clientless is off`).
- Candidate state: **not committed**; tracked source, test, and documentation changes are present in the working tree.
- Preflight publish output: self-contained `win-x64` executable built with version `0.0.1`; this is superseded by the configured `0.0.2` release version.
- Executable SHA-256: `8C503F4E4462318CACE80F2E39AA91BA112D0026C52F6DAF77DE98E1DF783511`.

## Kira verification
- `dotnet build src/SBotManager/SBotManager.csproj -warnaserror`: PASS, zero warnings/errors.
- Model/native smoke suite: PASS, 97 checks.
- Desktop UI automation suite: PASS, 40 checks.
- The model suite covers the recent normal-launch behavior: Start Game proceeds when automatic clientless is disabled even if the native clientless control cannot be synchronized.

## Rei release gate
**BLOCKED for publication.**

**Problem countdown: 6 remaining** — 4 ☠️ MUST blockers and 2 ⚠️ warnings.

| Countdown | Status | Requirement | Fix | Estimated effort* |
| --- | --- | --- | --- | --- |
| **✅ Complete** | ☠️ **MUST** | Version `0.0.1` collided with the existing `v0.0.1` portable artifact. | Configured application, installer, and documentation version `0.0.2`. Build metadata confirms file version `0.0.2.0`. | Completed |
| **6 → 5** | ☠️ **MUST** | The candidate has no committed/tagged source revision, so it cannot be reproduced from Git. | Review the changes, commit them, and create the matching release tag. | Small — 15–30 min |
| **5 → 4** | ☠️ **MUST** | Inno Setup (`iscc.exe`) is unavailable; no installer can be produced or validated. | Install Inno Setup 6, build the versioned installer, and preserve its checksum under `releases/<version>/`. | Small — 30–60 min |
| **4 → 3** | ☠️ **MUST** | The newly generated self-contained output has not received final artifact-based QA. | Run the UI smoke suite and critical manual checks against the exact packaged artifact. | Medium — 1–2 hr |
| **3 → 2** | ☠️ **MUST** | No clean-machine install, launch, upgrade, and rollback validation exists for this candidate. | Test the final installer on a clean Windows environment and record the result. | Medium — 1–3 hr |
| **2 → 1** | ⚠️ **WARNING** | No code-signing configuration was found. Unsigned public installers will trigger Windows reputation/security warnings. | Obtain/configure a code-signing certificate and sign the installer. | Medium–Large — certificate lead time varies; 1–2 hr after it is available |
| **1 → 0** | ⚠️ **WARNING** | No live regression with a real configured sBot/Silkroad installation is recorded. | Exercise launch, Start Game, training, window visibility, and exit behavior with a real local setup. | Medium — 1–2 hr, dependent on access to a test account |

\*Effort estimates are hands-on work ranges, not promised elapsed-time estimates. Some work can overlap once the final artifact exists.

## Hygiene
- `TrainingStart_Click` and `Names()` in `MainWindow.xaml.cs` have no references. They are non-blocking dead-code candidates; do not remove them during this release assessment.

## Required path to READY
1. Commit the approved candidate and create a new version/tag.
2. Install/configure Inno Setup, build the versioned installer, preserve it and its checksum under `releases/<version>/`, and configure code signing.
3. Validate install, launch, upgrade/rollback, and critical workflows on a clean Windows machine using that exact artifact.
4. Rerun Kira QA against the final artifact, then rerun this Rei preflight.
