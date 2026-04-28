# Copilot Instructions — MediaMatch

## Build, Test, and Lint

```powershell
# Build the GUI app (requires -p:Platform=x64)
dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64

# Build everything else (CLI, Core, Application, Infrastructure)
dotnet build

# Run all tests
dotnet test

# Run a single test project
dotnet test tests\MediaMatch.Application.Tests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~MetadataProviderChainTests.SearchAsync_ReturnsFirstSuccessfulResult"
```

The App project targets `net10.0-windows10.0.26100.0` and requires the Windows App SDK. All other projects target plain `net10.0`.

## Architecture

Clean Architecture with dependencies flowing inward:

```
App / CLI ──► Application ──► Core ◄── Infrastructure
```

- **Core** — Domain models, interfaces (`IMovieProvider`, `IEpisodeProvider`, `IMusicProvider`, `ILlmProvider`, `ISubtitleProvider`, `ILocalMetadataProvider`), enums, expression contracts. Zero external dependencies.
- **Application** — Services, pipeline orchestration (`MatchingPipeline`, `MetadataProviderChain`), batch operations, undo journal. Depends only on Core.
- **Infrastructure** — Provider implementations (TMDb, TVDb, AniDB, MusicBrainz, OpenSubtitles, LLM), HTTP client (`MediaMatchHttpClient`), caching (`MetadataCache`), persistence (`SettingsRepository`), `MediaInfoExtractor`. Depends on Core.
- **App** — WinUI 3 desktop UI with MVVM (CommunityToolkit.Mvvm source generators). Composition root in `App.xaml.cs`.
- **CLI** — Spectre.Console commands. Composition root in `Program.cs`.

## Key Conventions

### C# Style

- File-scoped namespaces (enforced as warning)
- Private fields use `_camelCase` prefix
- `var` preferred everywhere
- Nullable reference types enabled, warnings-as-errors
- Expression-bodied members for single-line methods/properties

### Provider Pattern

All metadata providers follow this shape:

1. Constructor injects `MediaMatchHttpClient`, config/API key, `MetadataCache`, `ILogger<T>`
2. Expose a `Name` property for logging/display
3. Use `MetadataCache` for response caching
4. Map API-specific DTOs (private nested classes) to Core domain models
5. Handle errors by logging and returning null/empty — never throw from search methods
6. LLM providers use raw `HttpClient` + `System.Text.Json` (no SDK), with `ILlmProvider` interface and `IsAvailable` config check

### HTTP and Serialization

- `MediaMatchHttpClient` wraps `HttpClient` with retry logic. Use `maxRetries: 0` in tests.
- JSON deserialization uses `System.Text.Json` with `PropertyNameCaseInsensitive = true` but **no** snake_case naming policy. Test JSON must use camelCase keys to match PascalCase DTO properties.

### MVVM (App Layer)

- ViewModels extend `ViewModelBase` (which extends `ObservableObject`)
- Use `[ObservableProperty]` and `[RelayCommand]` source generators from CommunityToolkit.Mvvm
- Navigation via `NavigationService` mapping string keys to page types

### Test Conventions

- xUnit with `[Fact]` and `[Theory]`, FluentAssertions (`.Should()`), Moq
- Test class naming: `{ClassName}Tests`, sealed classes
- Test method naming: `Method_Scenario_ExpectedResult`
- Mock `HttpMessageHandler` via `Moq.Protected()` (since `MediaMatchHttpClient` is sealed)
- Use real `MemoryCache` and `MediaMatchHttpClient` with mocked handler in provider tests
- `NfoMetadataProvider`/`XmlMetadataProvider` implement both `IMovieProvider` and `IEpisodeProvider` — cast to the specific interface to avoid CS0121 ambiguity

### DI Registration

Both composition roots (`App.xaml.cs`, `Program.cs`) use manual `ServiceCollection` registration — no auto-scanning. Services are mostly singletons.

### Settings and Secrets

- Settings stored at `%LOCALAPPDATA%\MediaMatch\settings.json`
- API keys encrypted with Windows DPAPI via `ISettingsEncryption`
- `ISettingsRepository` in Core, implementation in `Infrastructure/Persistence`
- Never read `.env` files or commit secrets

### Error Handling

Provider and pipeline code catches exceptions, logs warnings, and continues with fallbacks rather than failing fast. This is intentional — a single provider failure should not block the entire matching pipeline.

### Logging

- Services use `ILogger<T>` with optional parameter (`ILogger<T>? logger = null`) and `NullLogger<T>` fallback
- Serilog is the backend, wired only in App/CLI composition roots — never reference Serilog in Application or Core layers

### P/Invoke

Infrastructure uses `LibraryImport` (not `DllImport`) with `AllowUnsafeBlocks = true` and partial classes for source generation (see `ReFsCloneHandler`, `HardLinkHandler`).
