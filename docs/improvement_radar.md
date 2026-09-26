# Improvement radar

## LIVE-01
- CATEGORY: Integration
- STATUS: Resolved
- WHAT: Replace simulated lifecycle with path-specific process control.
- WHERE: `Services/WindowsBotService.cs`.
- WHY: Real path/PID/start-time verification, controls, visibility, recovery, and termination are implemented.
- ACTION: Superseded by the current production integration.
- OWNER: Adam / implementation
- SCOPE: Completed
- URGENCY: N/A
- CONFIDENCE: High
- SOURCE: User baseline and brainstorming

## TELEMETRY-01
- CATEGORY: Research
- STATUS: Active
- WHAT: Investigate a supported sBot/Silkroad source for connection, farming, current XP, and required XP.
- WHERE: Supported sBot API/export/log integration only; no memory scanning or fabricated values.
- WHY: Process existence and a delivered Start Game click do not establish game health; XP bar is intentionally unavailable.
- ACTION: Public research found no documented, trustworthy sBot telemetry interface. Obtain a supported sBot API/export/log reference, or user authorization to inspect the installed sBot UI/log files, before integrating any data.
- OWNER: Adam
- SCOPE: Unknown
- URGENCY: Before promising live account status
- CONFIDENCE: High
- SOURCE: Baseline review; public research 2026-09-25
