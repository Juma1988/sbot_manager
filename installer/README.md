# Windows installer

This project uses **Inno Setup 6** to produce a self-contained x64 Windows installer.

## Build

1. Install Inno Setup 6 from its official site.
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-release.ps1 -Version 0.0.1
```

The script publishes the app with the .NET runtime included, creates the installer, and stores the installer, SHA-256 checksum, and installer definition under `releases\0.0.1\`.

Do not distribute an installer before preserving that versioned release directory.
