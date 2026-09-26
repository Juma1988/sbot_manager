# Rollback plan

## Like you are five

Before giving anyone a new app, keep the previous good installer in a labeled box. If the new app has a problem, give people the old installer from the box.

## Project rule

Every released version has a permanent folder:

```text
releases/
  0.0.1/
    i1988-sBot-manager-setup-0.0.1.exe
    SHA256.txt
    i1988-sBot-manager.iss
```

## v0.0.1 portable artifact

- File: `releases/i1988-sBot-manager-0.0.1-win-x64.zip`
- Rollback copy: `releases/0.0.1/i1988-sBot-manager-0.0.1-win-x64.zip`
- SHA-256: `FC64B50449C1E16BA17CA9A52B7FB5F1C5CC3A6DA6B739E454E8EDB9D9BCA121`
- Distribution: self-contained Windows x64 portable ZIP; no installer or .NET runtime install required.

## v0.0.1 standalone portable artifact

- File: `releases/i1988-sBot-manager-0.0.1-win-x64-standalone.zip`
- Rollback copy: `releases/0.0.1/i1988-sBot-manager-0.0.1-win-x64-standalone.zip`
- SHA-256: `BECC03971ECBEF9468A3319F9451B335955747B7721D6498A0D4DF8341A9B39A`
- Distribution: one self-contained EXE plus `portable.flag` and README. On first launch, the EXE creates `data/settings.json` beside itself; settings travel with the extracted folder.

## Roll back a bad release

1. Stop sharing the faulty installer.
2. Tell users which version is safe to reinstall.
3. Give users the previous installer from `releases/<previous-version>/`.
4. Users install it over the newer version; the stable Inno Setup AppId makes it an upgrade/downgrade of the same app.
5. Verify that the app opens and settings remain available. Never delete `%LocalAppData%\SBotManager` during rollback.

## Code signing

Code signing is a certificate Windows uses to identify the publisher and detect modified installers. Before public distribution, buy or obtain a Windows code-signing certificate from a trusted certificate authority, then configure Inno Setup/signing tooling to sign the generated installer. Never put the certificate password or private key in this repository.
