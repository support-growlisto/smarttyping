# 03 — Architecture

## 1. รูปแบบ (Style)

**Clean Architecture** ที่มีเลเยอร์ซ้อนศูนย์กลางร่วมกันสี่ชั้น บวกกับ shared kernel
การพึ่งพา (dependencies) ชี้เข้าด้านในเท่านั้น Domain อยู่ตรงกลางและไม่พึ่งพาสิ่งใดเลย

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

## 2. โปรเจกต์และหน้าที่รับผิดชอบ

| Project                     | Depends on                         | Contains |
|-----------------------------|------------------------------------|----------|
| `SmartTyping.Domain`        | Shared                             | Entities (`Snippet`, `Category`, `UsageHistory`, `AppSetting`), value objects (`Trigger`), enums (`ConversionDirection`, `SettingKeys`), กฎของ domain แบบ pure |
| `SmartTyping.Application`   | Domain, Shared                     | Ports (`ISnippetRepository`, `IClipboardService`, `IKeyboardHook`, `ITextInjector`, `ITemplateEngine`, `IKeyboardLayoutConverter`, `IDateTimeProvider`), DTOs, application services (`SnippetExpansionService`, `LanguageConversionService`, `SettingsService`), `TemplateEngine`, `KeyboardLayoutConverter` |
| `SmartTyping.Infrastructure`| Application, Domain, Shared        | SQLite connection factory + schema init, Dapper repositories, `WindowsKeyboardHook`, `WindowsClipboardService`, `WindowsTextInjector`, การตั้งค่า Serilog |
| `SmartTyping.UI`            | Application, Infrastructure, Domain, Shared | WPF app, `App.xaml` DI composition root, views, viewmodels, tray icon, การเชื่อมต่อ hotkey |
| `SmartTyping.Shared`        | (none)                             | `Result`/`Result<T>`, `Guard`, helper แบบ cross-cutting ขนาดเล็ก |
| `SmartTyping.Tests`         | Application, Domain, Shared        | xUnit tests สำหรับ logic แบบ pure |

> หมายเหตุเกี่ยวกับ converter/template engine แบบ pure: ทั้งสองไม่มี I/O จึงอยู่ใน
> **Application** ในฐานะ concrete implementations ของ interface ที่นิยามไว้ใน Application วิธีนี้ทำให้
> ทั้งสองสามารถทำ unit test ได้ง่ายมากโดยไม่ต้องพึ่ง Infrastructure ส่วน input/output ที่เจาะจงกับ Windows
> (hooks, clipboard, injection) จะอยู่ใน **Infrastructure**

## 3. Key flows

### 3.1 การขยาย snippet (explicit action)
```
User triggers expansion for "/phone"
  → SnippetExpansionService.TryExpandAsync("/phone")
      → ISnippetRepository.FindByTriggerAsync   (SQLite/Dapper)
      → ITemplateEngine.Render(content)         (pure)
      → repository.IncrementUsageAsync + add UsageHistory
  → ITextInjector.InjectAsync(resolvedText)     (Infrastructure)
```

### 3.2 การแปลงภาษา (Ctrl+Shift+L)
```
Global hotkey fires
  → capture selection via IClipboardService (copy) OR last-word buffer
  → LanguageConversionService.Convert(text)
      → IKeyboardLayoutConverter.DetectDirection + Convert  (pure)
  → ITextInjector.InjectAsync(convertedText)
```

## 4. Cross-cutting concerns

- **DI**: `Microsoft.Extensions.DependencyInjection`; composition root เดียวคือ `SmartTyping.UI/App.xaml.cs`
- **Logging**: Serilog, ตั้งค่าใน Infrastructure, inject เข้ามาในรูปแบบ `ILogger`
- **Configuration**: การตั้งค่าแอปอยู่ใน SQLite (`AppSetting`) ไม่ใช่ appsettings.json ดังนั้นผู้ใช้จึงแก้ไขได้ภายในแอป
- **Threading**: hooks/hotkeys รันนอก UI thread; ViewModels ทำ marshal ผ่าน dispatcher
- **Error handling**: ขอบเขต I/O คืนค่า `Result`/`Result<T>` (Shared) แทนการ throw ข้ามเลเยอร์; infrastructure บันทึก log และกลืน (swallow) ความผิดพลาดระดับ OS

## 5. กลยุทธ์การทดสอบ (Testing strategy)

- Logic แบบ pure (converter, template engine, การจับคู่ snippet) → unit test ที่รวดเร็ว ไม่ต้องใช้ mock
- Application services → ทดสอบด้วย in-memory fakes ของ ports
- Infrastructure (Dapper, hooks) → บาง, ทดสอบด้วยตนเอง / ทำ integration test ทีหลัง; ไม่ใช่จุดโฟกัสของ MVP

## 6. Packaging

Framework-dependent build สำหรับการพัฒนา; มีแผนทำ self-contained single-file publish profile
สำหรับการ release (ดู `08_Release_Plan.md`) ไม่มี installer ใน MVP — ใช้ zip + shortcut

## 7. Decision records

ดู [`ADR/`](ADR/): clean architecture (001), แนวทาง keyboard hook (002), การออกแบบ snippet engine
(003), การเลื่อนระบบ plugin ออกไป (004)
