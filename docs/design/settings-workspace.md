# Settings workspace

## Status
Active — 2026-09-25.

## Purpose
Settings is the single, intentional home for workspace behavior preferences. The sidebar links to it but does not expose mutable preference toggles.

## Structure
1. The two-column layout keeps sections intact: **Workspace behavior** and **Windows startup** on the left; **Exit behavior** and **Windows notifications** on the right.
2. Workspace behavior contains Close to tray, Auto-hide sBot, and Auto-hide SRO client. Each has a concise consequence statement and a right-aligned compact switch: slate + OFF when disabled, mint + ON when enabled, with a sliding white thumb.
3. Windows startup contains Manager at sign-in and its dependent launch-configured-bots option.
4. Save and Cancel remain visible below the complete form.

## Interaction and accessibility
- All controls have explicit automation names/IDs and keyboard focus behavior supplied by the shared WPF styles.
- Settings changes are saved atomically together; Cancel changes nothing.
- The form uses readable labels, text explanations, and non-color state cues.
- The dialog's height is determined by its complete content and has no internal scrollbar or surplus resize space.
- The bot startup option is disabled until Manager Windows startup is enabled.

## Notifications
Settings includes a Windows-notification master switch plus per-event switches for recovery retry limit, unexpected bot stop, unverifiable bot, and force-termination requirement. Existing default messages remain in effect.
