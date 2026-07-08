# 03 — Architecture

## 1. Style

**Clean Architecture** with four concentric layers plus a shared kernel. Dependencies
point inward only. The Domain is the center and depends on nothing.

```
        ┌───────────────────────────────────────────┐
        │                   UI (WPF)                 │  composition root, views, viewmodels, tray
        │   ┌───────────────────────────────────┐    │
        │   │          Application              │    │  use cases, ports (interfaces), DTOs
        │   │   ┌───────────────────────────┐   │    │
        │   │   │          Domain           │   │    │  entities, value objects, enums
        │   │   └───────────────────────────┘   │    │
        │   └───────────────────────────────────┘    │
        └───────────────────────────────────────────┘
                 ▲                         ▲
        Infrastructure (implements Application ports: SQLite/Dapper, hooks, clipboard, injection, logging)

        Shared: Result<T>, guards, pure primitives — referenced by all layers.
```

## 2. Projects & responsibilities

| Project                     | Depends on                         | Contains |
|-----------------------------|------------------------------------|----------|
| `SmartTyping.Domain`        | Shared                             | Entities (`Snippet`, `Category`, `UsageHistory`, `AppSetting`), value objects (`Trigger`), enums (`ConversionDirection`, `SettingKeys`), pure domain rules. |
| `SmartTyping.Application`   | Domain, Shared                     | Ports (`ISnippetRepository`, `IClipboardService`, `IKeyboardHook`, `ITextInjector`, `ITemplateEngine`, `IKeyboardLayoutConverter`, `IDateTimeProvider`), DTOs, application services (`SnippetExpansionService`, `LanguageConversionService`, `SettingsService`), `TemplateEngine`, `KeyboardLayoutConverter`. |
| `SmartTyping.Infrastructure`| Application, Domain, Shared        | SQLite connection factory + schema init, Dapper repositories, `WindowsKeyboardHook`, `WindowsClipboardService`, `WindowsTextInjector`, Serilog setup. |
| `SmartTyping.UI`            | Application, Infrastructure, Domain, Shared | WPF app, `App.xaml` DI composition root, views, viewmodels, tray icon, hotkey wiring. |
| `SmartTyping.Shared`        | (none)                             | `Result`/`Result<T>`, `Guard`, small cross-cutting helpers. |
| `SmartTyping.Tests`         | Application, Domain, Shared        | xUnit tests for pure logic. |

> Note on the pure converter/template engine: they contain no I/O, so they live in
> **Application** as concrete implementations of Application-defined interfaces. This keeps
> them trivially unit-testable without Infrastructure. Windows-specific input/output
> (hooks, clipboard, injection) stays in **Infrastructure**.

## 3. Key flows

### 3.1 Snippet expansion (explicit action)
```
User triggers expansion for "/phone"
  → SnippetExpansionService.TryExpandAsync("/phone")
      → ISnippetRepository.FindByTriggerAsync   (SQLite/Dapper)
      → ITemplateEngine.Render(content)         (pure)
      → repository.IncrementUsageAsync + add UsageHistory
  → ITextInjector.InjectAsync(resolvedText)     (Infrastructure)
```

### 3.2 Language conversion (Ctrl+Shift+L)
```
Global hotkey fires
  → capture selection via IClipboardService (copy) OR last-word buffer
  → LanguageConversionService.Convert(text)
      → IKeyboardLayoutConverter.DetectDirection + Convert  (pure)
  → ITextInjector.InjectAsync(convertedText)
```

## 4. Cross-cutting concerns

- **DI**: `Microsoft.Extensions.DependencyInjection`; the only composition root is `SmartTyping.UI/App.xaml.cs`.
- **Logging**: Serilog, configured in Infrastructure, injected as `ILogger`.
- **Configuration**: app settings live in SQLite (`AppSetting`), not appsettings.json, so users edit them in-app.
- **Threading**: hooks/hotkeys run off the UI thread; ViewModels marshal via the dispatcher.
- **Error handling**: I/O boundaries return `Result`/`Result<T>` (Shared) rather than throwing across layers; infrastructure logs and swallows OS-level faults.

## 5. Testing strategy

- Pure logic (converter, template engine, snippet matching) → fast unit tests, no mocks needed.
- Application services → tests with in-memory fakes of the ports.
- Infrastructure (Dapper, hooks) → thin, exercised manually / integration-tested later; not the MVP focus.

## 6. Packaging

Framework-dependent build for dev; a self-contained single-file publish profile is planned
for release (see `08_Release_Plan.md`). No installer in the MVP — zip + shortcut.

## 7. Decision records

See [`ADR/`](ADR/): clean architecture (001), keyboard hook approach (002), snippet engine
design (003), plugin system deferral (004).
