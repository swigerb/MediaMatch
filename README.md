# MediaMatch
## Modern open-source media file organizer

MediaMatch is a modern successor to FileBot, built with .NET 10, WinUI 3, and Fluent 2 design.

### Features (Planned)
- **Smart Matching** — Heuristic media file matching using multiple similarity metrics
- **Expression Engine** — Flexible naming templates with `{n}/{s00e00}` syntax
- **Multiple Providers** — TMDb, TVDB, AniDB, TVMaze, OpenSubtitles integration
- **Subtitle Support** — Search, download, and auto-match subtitles
- **CLI & GUI** — Both WinUI 3 desktop app and Spectre.Console CLI
- **Automation** — Folder watching and post-processing pipelines

### Tech Stack
- .NET 10 LTS
- WinUI 3 + Windows App SDK 1.8
- Fluent 2 Design
- Serilog + OpenTelemetry
- Velopack installer

### Building
```bash
dotnet build MediaMatch.sln
dotnet test
```

### License
GPL v3 — see [LICENSE](LICENSE) for details.
