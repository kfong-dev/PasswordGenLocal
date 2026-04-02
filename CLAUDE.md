# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project Overview

**Password Generator — Local** (`PasswordGenLocal`) is a Windows-only (.NET 8 WinForms, x64) enterprise password generator. It uses Windows CSPRNG exclusively — no external APIs, no internet calls. Designed for LAN and air-gapped environments.

- Minimum target: Windows 10 Build 19045
- MIT Licensed
- No NuGet packages — BCL only (`System.Security.Cryptography`, `System.Windows.Forms`, `System.Text.Json`, `Microsoft.Win32`)

---

## Build & Run

### Build script (PowerShell)
```powershell
.\build.ps1
```
Scans `bin\` for existing `PGLv*.exe`, increments N by 1, publishes to `bin\PGLvN.exe`.

### Manual equivalent
```powershell
dotnet publish PasswordGenLocal.csproj -c Release -r win-x64 --self-contained false `
  -o bin\ -p:AssemblyName=PGLv1
```
`PublishSingleFile=true` is set in the csproj — the output is a single EXE (no separate DLL). The .NET 8 runtime must be installed on the target machine (`--self-contained false`).

### Run
```powershell
.\bin\PGLvN.exe
```

### Notes
- x64 Release only. Debug builds are not used.
- `DebugType=none`, `DebugSymbols=false` — no PDB output.
- `PublishSingleFile=true` + `SatelliteResourceLanguages=en` — single EXE output, no language subfolders.
- ProcessMitigation policies (DEP, ASLR, CFG, etc.) applied manually per build by the user.
- The Bash tool is broken in this environment (MSYS2 init failure). Use Read/Write/Edit/Grep/Glob tools for file operations; advise user to run builds manually via PowerShell.

---

## Architecture

Two-layer separation:

```
PasswordGenLocal.Core     — no WinForms dependency; crypto, storage, settings
PasswordGenLocal.UI       — WinForms forms, controls, dialogs
PasswordGenLocal          — Program.cs, AppLog.cs (root namespace)
```

---

## Key Files

### Core
| File | Purpose |
|------|---------|
| `Core/PasswordGenerator.cs` | `CharsetMode` enum, `Generate()`, `FriendlyName()`, `CharsetLabel()`, `ColumnLabel()` |
| `Core/StoreManager.cs` | LPGE binary format — `SaveStore`, `LoadStore`, `VerifyIntegrity` |
| `Core/AppSettings.cs` | `%appdata%\LPG\config.ini` — `Load()`, `Save()`, `ResetToDefaults()` |
| `Core/LpgeStore.cs` | Store model: `StoreName`, `Created`, `Modified`, `Entries`, `IsDirty`, `FilePath`, `CachedPassword` |
| `Core/StoreEntry.cs` | Entry model: `Guid`, `Label` (Username), `Note`, `Timestamp`, `Mode`, `Length`, `Password` |
| `Core/ClipboardTracker.cs` | `SetText()`, `ClearIfOurs()`, `ClearAll()` |
| `Core/EnterpriseDetector.cs` | `NetGetJoinInformation` P/Invoke + Entra registry — returns `JoinType` enum |

### UI
| File | Purpose |
|------|---------|
| `UI/Theme.cs` | Static colors, `Apply(bool dark)`, `StyleGrid()`, `StyleMenuStrip()`, `SetTitleBarDark()` (DWM) |
| `UI/SessionView.cs` | UserControl — DataGridView (Timestamp, Settings, Password); Delete key + context menu |
| `UI/StoreView.cs` | UserControl — header (Name, Created, Modified, dirty indicator) + DataGridView (Username, Note, Timestamp, Settings, Password) |
| `UI/MainForm.cs` | All menu handlers, generate logic, tab management, timers, theme application, delete confirm handler |
| `UI/MainForm.Designer.cs` | 960×540 layout: 278px sidebar (left), 1px separator, fill TabControl |
| `UI/Dialogs/PassphraseDialog.cs` | Enter or Create mode, entropy indicator, show/hide toggle |
| `UI/Dialogs/NewStoreDialog.cs` | Store name + password + confirm |
| `UI/Dialogs/ExportWarningDialog.cs` | Plaintext CSV warning, I Accept / suppress option |
| `UI/Dialogs/PreferencesDialog.cs` | 5-tab: General, Security, Store Settings, Session Settings, Global |
| `UI/Dialogs/AboutDialog.cs` | Logo placeholder, version, MIT license (scrollable RichTextBox), workstation status |

### Root
| File | Purpose |
|------|---------|
| `Program.cs` | STAThread, crash/thread exception handlers, `ApplicationConfiguration.Initialize()` |
| `AppLog.cs` | Appends to `rorg_log.csv` in exe directory — columns: timestamp, type, message, detail |

---

## LPGE Binary Store Format

```
Offset  Length  Field
0       4       Magic bytes: ASCII "LPGE" (0x4C 0x50 0x47 0x45)
4       2       Format version (LE uint16) — currently 1
6       1       Integrity algo: 0x01 = SHA3-384, 0x02 = SHA-512
7       32      PBKDF2 salt
39      12      AES-GCM nonce
51      4       Ciphertext length (LE int32)
55      N       AES-256-GCM ciphertext (encrypted UTF-8 JSON payload)
55+N    16      AES-GCM authentication tag
71+N    64      Integrity hash (covers all bytes above; see algo byte)
```

**Integrity hash detail:**
- SHA3-384 produces 48 bytes, zero-padded to 64
- SHA-512 produces 64 bytes
- The algo byte is peeked **before** the integrity check so cross-platform files load correctly

**Key derivation:** PBKDF2-SHA512, 210,000 iterations, 32-byte output

**Atomic save:** write to `.tmp` → `File.Replace()` with no backup (avoids `.bak` accumulation on shares)

**Forward compatibility:**
- Files created on Win10 (SHA-512) load correctly on Win11 and vice versa
- Files created with SHA3-384 on Win11 will fail gracefully on Win10 with a clear error message
- JSON payload fields: unknown fields are silently ignored on deserialise (System.Text.Json default)

**JSON payload entry fields:** `id`, `label`, `note`, `ts`, `mode`, `length`, `pwd`

---

## Password Generation

- CSPRNG: `RandomNumberGenerator` — no `System.Random` for password bytes
- Rejection sampling: no modular bias. `maxValid = (256 / charsetLen) * charsetLen`
- Length 65–127 is blocked by design (invalid range enforced in UI and on generate)

### Charset modes
| Mode | Sidebar Label | Column Label | Characters |
|------|---------------|--------------|------------|
| `Web (0)` | Web/OAuth | Web (N characters) | Alphanumeric + `!#$%&()*+-./:;=?@_~` |
| `Nix (1)` | NIX/POSIX | POSIX (N characters) | Alphanumeric + `!#%+,-./:=@_~` |
| `Azure (2)` | Azure AD/Kerberos | Kerberos (AD) (N characters) | Full 95 printable ASCII (0x20–0x7E) |

`CharsetLabel()` is used in the sidebar radio buttons. `ColumnLabel(mode, length)` is used in the grid "Settings" column and CSV export.

---

## Configuration

**Path:** `%appdata%\LPG\config.ini`

Key=value format, `#` comments stripped. Keys (case-insensitive):

```
theme                        # 0=Follow System, 1=Light, 2=Dark
defaultLength                # 7–128 (65–127 invalid range)
defaultMode                  # 0=Web, 1=Nix, 2=Azure
defaultCount                 # 1–128
trackAndClearGlobal          # true/false
trackAndClearEverSet         # true/false — first-exit prompt guard
clipboardTimeoutSeconds      # 0=disabled
askOnGenerate                # true/false — first-generate store prompt
csvExportWarningAccepted     # true/false
rememberStorePassword        # true/false — cache store password in process memory per session
storeCheckIntervalSeconds    # 1–60 — dirty-indicator refresh rate (default 5)
deleteConfirmGlobal          # true/false — show confirm dialog before deleting rows
```

Note: `editableStoreWarningAccepted` is a legacy key that is silently ignored if present in old config files.

---

## Theming

- Three modes: Follow System (registry `AppsUseLightTheme`), Light, Dark
- DWM title bar: `DwmSetWindowAttribute` attr 20 (Win11), attr 19 fallback (Win10)
- All colors defined as static properties on `Theme` — both light and dark values
- `Theme.Apply(bool dark)` sets `Theme.IsDark`; all color properties react immediately
- Apply theme to new windows: call `ApplyTheme()` in the form's `Load` event

---

## Tab & Store Behaviour

- **Session tab** always present (`tabSession`), never closeable
- **Store tabs** open dynamically via `AddStoreTab(LpgeStore)`, tracked in `_storeTabs: Dictionary<TabPage, StoreView>`
- Generation is **context-sensitive**: active tab determines output target (session vs store)
- Entries can be deleted (Delete key or right-click context menu); delete confirmation is configurable
- `StoreView.IsDirty` → tab title gets ` *` suffix + amber dirty indicator in the header; checked on close and app exit
- `_suppressStorePromptSession`: suppresses first-generate store prompt for the session only
- `_suppressDeleteConfirmSession`: suppresses delete-row confirmation for the session only
- `LpgeStore.CachedPassword`: runtime-only; populated when `RememberStorePassword` is enabled; cleared on tab close and app exit

---

## Sidebar Layout (top → bottom)

1. PASSWORD LENGTH — `nudLength`
2. NUMBER OF PASSWORDS — `nudCount` (formerly "Batch Count")
3. CHARACTER SET — `radWeb`, `radNix`, `radAzure`
4. Generate button
5. Log textbox (fills remaining space)

---

## Status Bar Layout (left → right)

`UTC HH:mm:ss.ff`  `|`  `Local HH:mm:ss.ff`  [spring]  `AES-256-GCM | <algo>`

Clock timer fires every 100 ms (centisecond display). Encryption label is set once on `OnLoad` from `StoreManager.HashAlgorithmLabel`.

---

## Non-obvious Details

- `_ran` (System.Random) in MainForm is seeded from CSPRNG every 60s but never used for password bytes — vestigial, intentional
- `ClipboardTracker.WroteToClipboard` flag: only clear clipboard on exit if this session wrote to it (`ClearIfOurs`); `ClearAll` always clears regardless
- `EnterpriseDetector.Detect()` calls `NetGetJoinInformation` twice in the Workgroup/Standalone path — minor inefficiency, not a bug
- `AboutDialog` has `DetectUrls = false` on the RichTextBox and no `Process.Start` calls — zero internet capability
- `StoreEntry.Label` is the backing field for the "Username" display column — the field name was kept for JSON compatibility; only the UI column header changed
- `StoreEntry.Note` is sanitised on edit to printable ASCII (0x20–0x7E), max 95 characters

---

## Refer to SPEC.md for feature requests

Process iteratively in order — complete each numbered item fully before proceeding.
If context is near exhaustion, stop at the last fully completed item and report it.
If there is a conflict with a feature request, stop and ask the user for direction.
