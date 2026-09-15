# Contributing to Prononce for Windows 🇫🇷

Thank you for your interest in contributing to Prononce! We welcome bug reports, improvements, vocabulary expansions, and feature proposals.

---

## Code of Conduct

Please be respectful, constructive, and collaborative in all discussions, issues, and pull requests.

---

## Development Setup

### Prerequisites

- **Windows 10 (Build 19041+) or Windows 11**
- **.NET 8.0 SDK** (or later): [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (with *.NET desktop development* workload) or **VS Code** with C# Dev Kit.

### Clone and Build Locally

```powershell
# 1. Clone your fork
git clone https://github.com/<your-username>/prononce_ws.git
cd prononce_ws

# 2. Restore dependencies and build
dotnet build Prononce.sln

# 3. Run the application
dotnet run --project src/Prononce/Prononce.csproj
```

### Packaging Locally

To test single-file packaging:
```powershell
.\scripts\build.ps1
```
The compiled executable will be located in `dist/Prononce.exe`.

---

## Architectural Principles to Follow

When submitting code changes, please keep these core design principles in mind:

1. **Zero Idle CPU**: Keep the application purely message/event-driven. Never add background polling loops.
2. **Never Clobber Clipboard**: Any synthetic clipboard operation must completely back up and restore previous clipboard formats in memory.
3. **Never Steal Keyboard Focus**: The floating HUD must always preserve `WS_EX_NOACTIVATE` so that learners never lose their cursor while reading or writing.
4. **100% Offline & Private**: Do not introduce dependencies that send user text or telemetry to external cloud servers. All speech and dictionary lookups must work fully offline.

---

## Submitting Pull Requests

1. Create a feature branch: `git checkout -b feature/my-new-feature`
2. Commit your changes with clear, descriptive commit messages.
3. Verify that the solution builds cleanly: `dotnet build Prononce.sln -c Release`
4. Push your branch to GitHub and open a Pull Request against `master`.
