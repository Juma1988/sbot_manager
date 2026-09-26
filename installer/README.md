# Windows installer

This project uses **Inno Setup 6** to produce a self-contained x64 Windows installer.

## Build

1. Install Inno Setup 6 from its official site.
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-release.ps1 -Version 0.0.2
```

The script publishes the app with the .NET runtime included, creates the installer, and stores the installer, SHA-256 checksum, and installer definition under `releases\0.0.2\`.

## Distribute it

Do not distribute an installer before preserving that versioned release directory. For other people to download it, create a **GitHub Release** with a matching version tag (for example, `v0.0.2`) and upload the installer, optional portable ZIP, SHA-256 checksum, and release notes as release assets.

Use GitHub Releases for downloadable Windows builds. Do not use GitHub Packages: it is intended for developer dependencies, not application installers.
