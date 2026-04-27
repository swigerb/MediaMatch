# McManus — History

## Project Context
- **Project:** MediaMatch — modern open-source successor to FileBot
- **Based on:** FileBot v4.7.9 open-source (FB-Mod fork at github.com/barry-allen07/FB-Mod)
- **Tech Stack:** .NET 10 LTS, WinUI 3, Fluent 2, Velopack, OpenTelemetry, Serilog
- **User:** swigerb
- **UI Framework:** WinUI 3 with Fluent 2 design system

## Learnings

### 2026-04-27 — Phase 6: MVVM Architecture Implementation

- **CommunityToolkit.Mvvm 8.4.2 requires partial properties (not fields)** for WinRT AOT compatibility. Use `[ObservableProperty] public partial string Foo { get; set; }` instead of `[ObservableProperty] private string _foo;`. Error MVVMTK0045 fires otherwise.
- **DI container** set up in `App.xaml.cs` using `Microsoft.Extensions.DependencyInjection` (via `Microsoft.Extensions.Hosting` package). Static `App.GetService<T>()` accessor pattern.
- **Navigation** uses `NavigationView` + `Frame` in `MainWindow.xaml`. `NavigationService` wraps the Frame for ViewModel-driven navigation.
- **Build command**: `dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64` — must specify platform due to MSIX packaging requirements.
- **Key paths:**
  - ViewModels: `src/MediaMatch.App/ViewModels/`
  - Services: `src/MediaMatch.App/Services/`
  - Converters: `src/MediaMatch.App/Converters/`
  - Pages: `src/MediaMatch.App/Pages/`
- **x:Bind** used throughout for compiled bindings. `BoolToVisibilityConverter` needed for bool→Visibility conversion.
- **Mica backdrop** + `ThemeResource` used for dark/light theme — no custom colors.
- `HomeViewModel` is registered as singleton (preserves file list across navigations); `SettingsViewModel` and `AboutViewModel` are transient.

### 2026-04-27 — Team Consolidation Checkpoint

**Cross-team updates:**
- **From Fenster:** Phase 3 complete (providers, HTTP client, caching). ApiConfiguration available in Core for SettingsPage binding.
- **From Hockney:** Phase 5 complete (services, abstractions, 157 tests). IFileOrganizationService ready for HomePage integration.
- **From Keaton:** Phase plan consolidated to decisions.md. Critical path: Phase 3 → 5 → 6 integration. No blockers.

**Next:** Phase 5 services bound to HomeViewModel (file organization, media analysis). Phase 9 wizard (settings persistence) runs in parallel.

### 2026-04-27 — Phase 9: Configuration, Settings Persistence & Initialization

- **Settings architecture**: Interfaces (`ISettingsRepository`, `ISettingsEncryption`) in Core, implementations in Infrastructure. Both App and CLI share the same persistence layer.
- **AppSettings model** in `Core/Configuration/AppSettings.cs` — nested `ApiKeySettings`, `RenameSettings`, `OutputFolderSettings`. `FileOrganizationRules` provides per-media-type folder patterns.
- **SettingsRepository** reads/writes `%LOCALAPPDATA%/MediaMatch/settings.json` using `System.Text.Json`. Thread-safe via `SemaphoreSlim`. Atomic writes (write to .tmp, then `File.Move`). `FileShare.Read` for CLI coexistence.
- **SettingsEncryption** uses Windows DPAPI (`ProtectedData`) scoped to CurrentUser. Only API key fields encrypted; rest of JSON is human-readable. `ENC:` prefix distinguishes encrypted values. Requires `[SupportedOSPlatform("windows")]` since TFM is `net10.0`.
- **SettingsViewModel** updated: constructor now takes `ISettingsRepository` via DI. `LoadSettingsCommand` runs on page navigation. `SaveSettingsCommand` validates API key format (alphanumeric, hyphens, underscores only). Added OpenSubtitles and Anime fields.
- **First-run detection** in `App.xaml.cs`: checks `ISettingsRepository.SettingsFileExists()` in `OnLaunched`, navigates to Settings with `"first-run"` parameter. Welcome banner shown via `InfoBar` with `ShowWelcomeBanner` bound property.
- **DI wiring**: `AddMediaMatchInfrastructure()` in Infrastructure's `ServiceCollectionExtensions` now registers `ISettingsEncryption → SettingsEncryption` and `ISettingsRepository → SettingsRepository` as singletons. App.xaml.cs calls this method instead of manual registration.
- **Pre-existing build issue**: Serilog 5.0.3 not found on nuget.org — downgraded to 4.2.0. MatchingPipeline.cs has syntax errors unrelated to this phase.
- **DPAPI package**: Added `System.Security.Cryptography.ProtectedData 9.0.5` to Infrastructure.csproj.

### 2026-04-27 — Phase 11: Batch Operations & Undo + Phase 14: Polish & Documentation

- **BatchOperationService** (`Application/Services/BatchOperationService.cs`) — processes files in parallel via configurable chunk-based concurrency (default 4). Delegates to `IFileOrganizationService.OrganizeAsync()` per file. Reports progress via `IProgress<BatchProgress>`. Cancellation marks remaining items as Skipped.
- **UndoService** (`Application/Services/UndoService.cs`) — rolling journal of up to 100 `UndoEntry` records stored at `%LOCALAPPDATA%/MediaMatch/undo.json`. Atomic write (`.tmp` + `File.Move`). Thread-safe via `SemaphoreSlim`. Uses `IFileSystem` abstraction from Hockney's Phase 5 for testability.
- **Core models**: `BatchJob.cs` (Id, Files, Status, Progress, timestamps), `UndoEntry.cs` (record: OriginalPath, NewPath, Timestamp, MediaType). Core interfaces: `IBatchOperationService`, `IUndoService`.
- **BatchProgressViewModel** — exposes TotalFiles, CompletedFiles, FailedFiles, CurrentFile, ProgressPercent, ProgressText for UI binding.
- **HomeViewModel updated**: Constructor now accepts `IBatchOperationService`, `IUndoService`, `ILogger` via DI. New commands: `SelectAll`, `CancelBatch`, `UndoLast`, `Refresh`. `ApplyRenames` now uses batch service with progress reporting and records undo entries on success. Error handling wraps all async operations.
- **HomePage.xaml updated**: Added batch progress bar (ProgressBar + text), Cancel button (visible during batch), Undo Last and Refresh toolbar buttons, Select All button. Added `Page.KeyboardAccelerators` for Ctrl+O, Ctrl+A, Delete, Ctrl+Z, F5.
- **HomePage.xaml.cs updated**: `OnKeyboardAcceleratorInvoked` override dispatches to ViewModel commands.
- **DI registration** in `App.xaml.cs`: `IFileSystem`, `IBatchOperationService`, `IUndoService` registered as singletons. `HomeViewModel` factory uses `sp.GetRequiredService` for all three dependencies.
- **README.md** — Complete rewrite: project description, features list, quick start guide, FileBot comparison table, keyboard shortcuts, architecture diagram, tech stack, contributing guide.
- **CHANGELOG.md** — v0.1.0 entry covering all implemented features across all phases.
- **Theme support verified**: All new UI elements use `{ThemeResource}` bindings — no hardcoded colors. Progress bar, status text, and toolbar all respect dark/light theme.
- **Error handling polish**: All ViewModel async commands wrapped in try/catch with user-friendly StatusMessage updates. No stack traces exposed to UI. Logger captures full exception details.

### 2026-04-27 — Cross-Agent Impact: Fenster Phase 8 & Hockney Phase 10

**From Fenster (Phase 8 — CLI Commands):**
- Full CLI layer now available for settings integration. `ConfigCommand` can read/write from `ISettingsRepository` once wired.
- DI bridge pattern (`TypeRegistrar`/`TypeResolver`) proven with Spectre.Console.Cli — reusable pattern for other CLI integrations.
- 4 pre-existing build issues fixed; codebase compiles cleanly.

**From Hockney (Phase 10 — Observability):**
- Serilog + OpenTelemetry fully instrumented across Application layer. SettingsViewModel now logs state changes via `ILogger<T>` with optional pattern.
- First-run detection and settings persistence paths are traced via activity spans; all failures logged structurally.
- 159 tests pass without modification — optional logger pattern ensures backward compatibility for all existing tests.
