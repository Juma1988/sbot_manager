# Release-readiness assessment — 2026-09-25

## Candidate
- Artifact: `SBotManager.exe` from isolated Release publish output.
- SHA-256: `FCE2BDD3C9A7CD7465D37EA87D278E7A2FAF0E4696E77662E2D1C61B6B9FB172`
- Source revision: no Git repository/revision is available.
- Framework: .NET 10, Windows WPF, framework-dependent publish.

## Evidence run
- `dotnet publish -c Release`: PASS, zero warnings/errors.
- Model/native smoke suite: PASS, 81 checks.
- Desktop UI smoke suite against this release artifact: PASS, 37 checks.
- Dependencies: no direct or transitive NuGet packages.

## Hygiene review
- No packaging, MSIX/installer, code-signing, explicit app-version, or rollback artifact configuration was found.
- Harmless dead-code candidates: `TrainingStart_Click` and `Names()` in `MainWindow.xaml.cs` have no current references. They are not release blockers and were not removed during this assessment.

## Kira assessment
**CONDITIONAL PASS for local technical beta use.** Core automated lifecycle, native-control, and desktop UI checks pass against the pinned artifact.

### Verification gaps
- Current two-column Settings layout, notification controls, exit-window restoration, and recovery-history presentation lack focused runtime UI assertions.
- No live regression against a real configured sBot/Silkroad installation was run for this candidate.
- No full accessibility, DPI/text-scaling, or keyboard-only review was run.
- XP/connection telemetry remains intentionally unavailable pending a trustworthy supported source.

## Rei assessment
**BLOCKED for public publication.** Required publication readiness is incomplete:
1. Choose release target/channel and intended audience.
2. Set an explicit application version/build number.
3. Produce a distribution method (installer/MSIX or documented framework-dependent zip) and validate it on a clean Windows machine.
4. Configure/verify code signing if distributing beyond trusted local users.
5. Add focused verification for the newest Settings and exit behavior, then rerun QA on the final artifact.
6. Document a known-good artifact and rollback procedure.
