# MediaMatch

> A modern, open-source media file organizer — the successor to FileBot.

MediaMatch automatically renames and organizes your TV shows, movies, anime, and subtitle files using online metadata from TMDb, TVDb, and more. Built with .NET 10 and WinUI 3, it offers both a polished desktop GUI and a powerful CLI.

---

## Features

- **Smart Matching** — Heuristic matching engine using edit-distance, Jaccard similarity, and bigram overlap to accurately identify media files
- **Expression Engine** — Flexible naming templates (`{n} - {s00e00} - {t}`) with a full Scriban-based expression language
- **Multiple Metadata Providers** — TMDb movies/TV, TVDb series, with provider fallback and 0.85 confidence threshold
- **Batch Rename with Undo** — Process hundreds of files in parallel with progress tracking, cancellation, and one-click undo
- **Subtitle Support** — Detect and organize `.srt`, `.sub`, `.idx`, `.ssa`, `.ass` subtitle files alongside video
- **Desktop + CLI** — Full WinUI 3 desktop app with Fluent 2 design, plus a Spectre.Console CLI for automation
- **Settings Persistence** — Encrypted API key storage (Windows DPAPI), human-readable JSON config at `%LOCALAPPDATA%/MediaMatch/`
- **Dark & Light Themes** — Follows Windows system theme via Mica backdrop and `ThemeResource` bindings
- **Observability** — Structured logging via Serilog, OpenTelemetry tracing ready

## Screenshots

> *Screenshots will be added after the v0.1.0 release.*

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 (1809+) or Windows 11
- Windows App SDK 1.8+

### Build & Run

```powershell
# Clone the repository
git clone https://github.com/swigerb/MediaMatch.git
cd MediaMatch

# Build (must specify platform)
dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64

# Run tests
dotnet test
```

### Configuration

On first launch, MediaMatch opens the Settings page to configure API keys:

| Provider | Key Required | Get Key |
|----------|-------------|---------|
| TMDb | Yes | [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api) |
| TVDb | Yes | [thetvdb.com/dashboard/account/apikey](https://thetvdb.com/dashboard/account/apikey) |
| OpenSubtitles | Optional | [opensubtitles.com](https://www.opensubtitles.com/) |

Settings are stored at `%LOCALAPPDATA%\MediaMatch\settings.json`. API keys are encrypted with Windows DPAPI.

## How It Differs from FileBot

| | MediaMatch | FileBot |
|--|-----------|---------|
| **License** | GPL v3 — free and open source | Commercial ($6/year) |
| **Platform** | .NET 10, WinUI 3, Windows | Java, Swing, cross-platform |
| **UI** | Fluent 2 design, Mica backdrop | Legacy Swing UI |
| **Matching** | Configurable multi-metric engine | Fixed algorithms |
| **Undo** | Built-in undo journal | Manual rollback |
| **CLI** | Spectre.Console with rich output | Basic CLI |
| **Updates** | Velopack auto-update | Manual download |

MediaMatch is inspired by [FileBot](https://www.filebot.net/) and the open-source [FB-Mod fork](https://github.com/barry-allen07/FB-Mod) (FileBot v4.7.9). It is a clean-room rewrite — no FileBot code is used.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Add folder |
| `Ctrl+A` | Select all files |
| `Delete` | Remove selected files |
| `Ctrl+Z` | Undo last rename |
| `F5` | Refresh / re-scan folder |

## Architecture

```
MediaMatch.Core         — Domain models, interfaces, enums, expression contracts
MediaMatch.Application  — Services (rename pipeline, batch ops, undo, matching)
MediaMatch.Infrastructure — Providers (TMDb, TVDb), HTTP client, caching, persistence
MediaMatch.App          — WinUI 3 desktop application (MVVM, pages, navigation)
MediaMatch.CLI          — Spectre.Console command-line interface
```

Clean architecture: dependencies flow inward (`App/CLI → Application → Core`; `Infrastructure → Core`).

## Tech Stack

- **.NET 10 LTS** — Latest long-term support runtime
- **WinUI 3 + Windows App SDK 1.8** — Native Windows UI
- **CommunityToolkit.Mvvm 8.4** — Source-generated MVVM with partial properties
- **Serilog** — Structured logging to file and console
- **OpenTelemetry** — Distributed tracing and metrics
- **Scriban** — Expression template engine
- **Velopack** — Auto-update and installer framework

## Contributing

Contributions are welcome! Please open an issue first to discuss what you'd like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes and add tests
4. Run `dotnet build` and `dotnet test`
5. Submit a pull request

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.
