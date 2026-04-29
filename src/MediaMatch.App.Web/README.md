# MediaMatch.App.Web — Uno Platform WebAssembly Head

> **Status: Sprint 6 — Scaffolded, ready for WASM build testing.**

This project is the browser-based head for MediaMatch, using [Uno Platform](https://platform.uno/) 6.x compiled to WebAssembly. It runs the same WinUI 3 XAML as the Windows / macOS / Linux heads, hosted in any modern browser.

## Prerequisites

1. **.NET 10 SDK**
2. **wasm-tools workload**:
   ```bash
   dotnet workload install wasm-tools
   ```
3. A modern browser (Chrome, Edge, Firefox, Safari)

## Building

```bash
# From repo root:
dotnet build src/MediaMatch.App.Web/MediaMatch.App.Web.csproj -c Release
```

## Running locally

```bash
dotnet run --project src/MediaMatch.App.Web
```

The `Uno.Wasm.Bootstrap.DevServer` package starts a Kestrel-backed dev server (typically `http://localhost:5000`) that serves the bundled WASM payload with hot-reload friendly headers.

> **Note:** This project is **not** included in `MediaMatch.slnx` because the `wasm-tools` workload is not installed by default on Windows dev machines and would break the desktop build.

## Architecture

This project has its own:

- `App.xaml.cs` — Composition root with DI (mirrors Linux/macOS but omits `Infrastructure.Unix` and the Serilog file sink).
- `MainWindow.xaml` — NavigationView shell.
- Pages: `HomePage`, `SettingsPage`, `HistoryPage`, `AboutPage`.
- Controls: `EpisodesPanel`, `SubtitlePanel`, `SfvPanel`, `FilterPanel`, `ListPanel`.
- Dialogs: `LogViewer`, `PresetEditor`, `PresetManager`, `ExpressionEditor`, `MediaInfoInspector`, `MatchSelection`, `Conflict`, `KeyboardShortcuts`.
- Services: `BrowserSettingsEncryption` — Base64 obfuscation in lieu of DPAPI / AES (browser sandbox cannot store a stable key).

Shared layers (project references):

- `MediaMatch.Core` — Domain models, interfaces.
- `MediaMatch.Application` — Services, pipeline orchestration.
- `MediaMatch.Infrastructure` — HTTP clients, providers, caching.

> **No reference** to `MediaMatch.Infrastructure.Unix` — that project's POSIX hard links and AES file encryption do not apply in a browser.

## Feature subset available in the browser

| Area | Browser support |
|------|----------------|
| Metadata search preview (TMDb / TVDb / etc.) | ✅ Full |
| Expression editor | ✅ Full |
| Settings (API keys, theme) | ✅ Full (persisted via Mono WASM virtual FS) |
| About page | ✅ Full |
| File upload (read-only) | ⚠️ Via `Windows.Storage.Pickers.FileOpenPicker` only |
| Match / Rename pipeline | ❌ Requires desktop file system access |
| Hard links / ReFS clone | ❌ Not available |
| MediaInfo (`ffprobe`) inspector | ❌ Native binary, desktop only |
| Shell extension | ❌ Windows-only |

A persistent banner on the Home page communicates these limits to end-users.

## Differences from the desktop heads

| Concern | Desktop | Browser |
|---------|---------|---------|
| Rendering | WinUI 3 / Skia native | Uno WebAssembly |
| Encryption | DPAPI (Windows) / AES key file (Unix) | Base64 obfuscation only |
| Logging | Serilog Console + File | Serilog Console (browser dev tools) |
| Settings persistence | `%LOCALAPPDATA%\MediaMatch\settings.json` | Mono WASM virtual FS (per-origin) |
| File system access | Full | `FileOpenPicker` (read-only) |

## Deployment to GitHub Pages

After `dotnet publish -c Release`, the static WASM payload appears under
`bin/Release/net10.0/dist/`. Push the contents of that folder to the
`gh-pages` branch (or any static host):

```bash
dotnet publish src/MediaMatch.App.Web -c Release
# Then upload bin/Release/net10.0/dist/ to your static host
```

Because the app uses `<base href="./" />`, it runs correctly from a
sub-path such as `https://<user>.github.io/MediaMatch/`.
