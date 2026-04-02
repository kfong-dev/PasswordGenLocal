# Password Generator — Local

A Windows-only password generator built as a redesigned spin-off of RORG Password Generator. Focuses exclusively on local CSPRNG generation with session and persistent store capabilities.

Work in progress.

---

## How it differs from RORG

- **CSPRNG only** — no external API dependency, no hybrid mode. Entirely local and offline.
- **Redesigned UI** — cleaner layout, rebuilt from scratch with a different visual approach
- **Session passwords** — generated passwords available for the duration of the session
- **Password stores** — create and manage encrypted `.bin` store files for persistent password storage
- **Environment detection** — automatically detects whether the host machine is Domain-Joined, Entra-Joined (Azure AD), or in a Workgroup, and surfaces this in the UI

---

## Features

**Generation**
- Windows 10+ CSPRNG via `System.Security.Cryptography.RandomNumberGenerator`
- Rejection sampling — no modular bias in charset mapping
- Multiple character set profiles

**Store capabilities**
- Create named password stores saved as `.bin` files
- Session view of generated passwords without persistence
- Store management UI

**Environment awareness**
- Detects join state at startup: Domain, Entra/Azure AD, or Workgroup
- Relevant for environments where password policy or store location may differ

---

## Requirements

- Windows 10 x64 or later (Windows CSPRNG APIs are the hard dependency)
- .NET 8.0

---

## Status

Active development. Core generation and store functionality in progress.

---

## Security notes

All generation is local — no network calls, no telemetry. The `.bin` store format is currently being designed with encryption in mind. Do not use current store files for production secrets until the encryption implementation is finalised and documented.
