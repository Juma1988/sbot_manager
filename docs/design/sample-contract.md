# WPF sample design contract

**STATUS: Superseded historical sample contract.** Current live-product behavior and layout are recorded in `docs/project-memory.md`, `docs/progress.md`, and `docs/CHANGELOG.md`.

- Dark slate workspace, mint primary actions, amber demo banner.
- Persistent left rail: project identity, current Overview indicator, demo guide and reset.
- Main content: headline, explicit simulation warning, three summary cards, 2x2 account grid, recent activity.
- Account cards show name, textual status (not color alone), reference folder path, and start/stop/restart.
- All accounts initially stopped. Startup temporarily disables start; stop can interrupt it.
- Standard resizable Windows frame, 1080x760 minimum, vertical scrolling rather than clipped content.
- Keyboard-focus outlines and account-specific automation names on controls.
- No real game metrics, fake CPU charts, or implied live connection.
