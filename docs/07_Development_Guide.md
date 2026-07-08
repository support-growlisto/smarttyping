# 07 — คู่มือการพัฒนา

## 1. สิ่งที่ต้องมีก่อน

- Windows 10/11
- .NET 9 SDK (`dotnet --version` → 9.x)
- IDE สักตัว: Visual Studio 2022 (17.12+), JetBrains Rider หรือ VS Code + C# Dev Kit

## 2. เริ่มต้นใช้งาน

```powershell
git clone <repo> smarttyping
cd smarttyping
dotnet restore
dotnet build
dotnet run --project src/SmartTyping.UI
```

รันการทดสอบ:

```powershell
dotnet test
```

## 3. โครงสร้าง solution

ดูที่ [`03_Architecture.md`](03_Architecture.md) กฎเหล็ก:

- **Domain** อ้างอิงได้เฉพาะ Shared เท่านั้น ไม่มี SQLite, ไม่มี WPF, ไม่มี Windows API
- **Application** นิยาม interfaces (ports) และ pure services ห้ามอ้างอิง Infrastructure เด็ดขาด
- **Infrastructure** implement ports ของ Application ทุกส่วนที่เป็น P/Invoke อยู่ที่นี่
- **UI** เป็น composition root เพียงจุดเดียว (เดินสาย DI ใน `App.xaml.cs`)
- **Shared** เก็บ primitives เล็กๆ ที่ใช้ร่วมกันข้ามส่วน โดยไม่มี dependency ภายนอก

## 4. มาตรฐานการเขียนโค้ด

- เปิด nullable reference types **on**; แก้ warnings อย่า suppress
- ใช้ file-scoped namespaces; วาง `using` ไว้นอก namespace
- ใช้ `async`/`await` สำหรับงาน I/O ทั้งหมด (DB, clipboard, injection) ส่วน pure mapping ยังคงเป็น synchronous
- ใช้ constructor injection ทุกที่; ไม่มี service locator, ไม่ `new` infrastructure ในเลเยอร์ UI/App
- ห้ามมี static business logic ยกเว้นตารางค่าคงที่ล้วนๆ (keyboard maps) และ pure helpers
- ตั้งชื่อให้มีความหมาย; ใส่ XML doc-comments เฉพาะจุดที่พฤติกรรมไม่ชัดเจน
- เขียน method ให้เล็ก; เลือกใช้ `Result<T>` แทนการโยน exception ข้ามขอบเขตเลเยอร์

## 5. การเพิ่มฟีเจอร์ (สูตรทำ)

1. สร้างโมเดลใน **Domain** (entity/value object/enum) ถ้ามันเป็นแนวคิดหนึ่ง
2. นิยาม **port(s)** และ **DTOs** ใน Application; เขียน service โดยใช้ ports เหล่านั้น
3. เขียน **unit tests** ให้ service ด้วย fakes แบบ in-memory
4. Implement port(s) ใน **Infrastructure**
5. เดินสาย DI + UI ใน **UI**
6. อัปเดต `CHANGELOG.md` และเอกสารที่เกี่ยวข้อง

## 6. การทำงานกับฐานข้อมูล

- การเปลี่ยนแปลง schema ให้ใส่ไว้ใน `DatabaseInitializer` พร้อมกับ bump เวอร์ชัน (`SchemaVersion`)
- ห้ามแก้ไข DB ที่ ship ไปแล้วด้วยมือ; ให้ถือว่าเป็นข้อมูลของผู้ใช้
- DB สำหรับ dev บนเครื่องอยู่ที่ `%LOCALAPPDATA%\SmartTyping\smarttyping.db` ลบทิ้งเพื่อรีเซ็ต

## 7. โค้ดรับ input ของ Windows

- Hooks/injection ถูกแยกไว้ใน Infrastructure หลัง `IKeyboardHook`, `ITextInjector`,
  `IClipboardService` ห้ามเรียก `user32.dll` จากที่อื่น
- ให้ log-and-continue เสมอเมื่อเกิด OS faults; อย่าปล่อยให้ hook exception ทำให้แอปแครช

## 8. ข้อตกลงการ commit (Conventional Commits)

```
feat: add snippet usage counter
fix: correct Kedmanee mapping for sara-am
docs: expand database schema notes
refactor: extract template token parser
test: add converter round-trip cases
chore: bump Serilog to x.y.z
```

ชื่อ branch: `feat/<slug>`, `fix/<slug>`, `docs/<slug>`

## 9. นิยามของคำว่าเสร็จ (Definition of Done)

- โค้ดคอมไพล์ผ่านโดยไม่มี warning ใหม่; nullable สะอาด
- เพิ่ม/อัปเดต unit tests แล้วและผ่านเป็นสีเขียว
- อัปเดตเอกสาร/CHANGELOG แล้ว
- ทำ manual smoke test สำหรับทุกอย่างที่แตะต้อง hooks/injection

## 10. การแก้ปัญหา (Troubleshooting)

| อาการ                                | สาเหตุที่เป็นไปได้ / วิธีแก้                          |
|--------------------------------------|-----------------------------------------------------|
| กด hotkey แล้วไม่มีอะไรเกิดขึ้น        | แอปอื่นครอบครอง `Ctrl+Shift+L` อยู่; ตรวจสอบ logs   |
| Injection ไม่แทรกอะไรเลย              | เป้าหมายบล็อก synthetic input; ผลลัพธ์อยู่ใน clipboard แล้ว — วางเอง |
| DB เกิด error ตอนเริ่มโปรแกรม          | ไฟล์เสียหาย/ถูกล็อก; ปิด instance อื่นหรือลบ DB เพื่อรีเซ็ต |
| ภาษาไทยแสดงเป็นกล่องสี่เหลี่ยม         | ขาด font fallback ภาษาไทย; ตรวจสอบว่าระบบรองรับภาษาไทย |
