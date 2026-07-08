# SmartTyping Desktop

แอปเพิ่มประสิทธิภาพการพิมพ์บน Windows ที่รวม **การแก้ภาษาไทย/อังกฤษ**
เข้ากับ **การขยายข้อความและเทมเพลต**

พิมพ์คำสั่งสั้นๆ เช่น `/phone` แล้วมันขยายเป็นข้อความที่ตั้งไว้ · พิมพ์ไทยผิดเลย์เอาต์
(ได้อักษรอังกฤษมั่วๆ) แก้ได้ด้วยคีย์ลัดปุ่มเดียว — **ทำงานในเครื่องทั้งหมด ข้อมูลเป็นส่วนตัว**

---

## ภาพรวม

SmartTyping ช่วยให้ผู้ใช้สองภาษา (ไทย/อังกฤษ) พิมพ์เร็วขึ้นและแก้ข้อผิดพลาดที่เจอบ่อย
โดยไม่ต้องออกจากแอปที่ใช้อยู่:

- **ขยาย snippet** — คำสั่งสั้นขยายเป็นข้อความยาว มีหมวดหมู่ เปิด/ปิดได้ นับจำนวนการใช้
- **แปลงภาษา** — แปลงข้อความที่พิมพ์ผิดเลย์เอาต์ ไทย Kedmanee ↔ อังกฤษ QWERTY
  (เช่น `l;ylfu` → `สวัสดี`)
- **ตัวแปรเทมเพลต** — แทรกค่าอัตโนมัติ เช่น `{date}`, `{time}`, `{clipboard}`, `{cursor}`,
  และ `{input:ป้าย}` (ถามค่าตอนขยาย)

ออกแบบให้ **ปลอดภัยและชัดเจน**: ไม่มีการแทนที่ข้อความอัตโนมัติขณะพิมพ์ —
การแก้ภาษาและขยาย snippet เกิดจากการกดคีย์ลัด/ปุ่มเท่านั้น

---

## ฟีเจอร์

**การพิมพ์ (คีย์ลัด — ทุกอย่างเป็นการกดเอง ไม่มีอัตโนมัติ)**
- แปลงภาษา — แปลงข้อความที่เลือก หรือ **คำล่าสุด** ถ้าไม่ได้เลือก
- ขยาย snippet ที่คำสั่งที่เลือก
- **Quick-picker** — ค้นหา snippet แบบ **fuzzy** แล้วแทรกที่เคอร์เซอร์
- เพิ่ม snippet จากข้อความที่เลือก

**Snippet & เทมเพลต**
- CRUD, หมวดหมู่, เปิด/ปิด, นับการใช้, ปุ่มทดลองขยาย (Preview)
- ตัวแปร: `{date}` / `{date:yyyy-MM-dd}` / `{date+7}` / `{time}` / `{clipboard}` / `{cursor}` / `{input:ป้าย}`
- **นำเข้า/ส่งออก** เป็นไฟล์ JSON (backup / แชร์)

**ผู้ใช้ & ระบบ**
- **ตั้งคีย์ลัดเองได้ทั้ง 4 ตัว**, จัดการหมวดหมู่, หน้าสถิติการใช้งาน
- **Dark mode** (ตามระบบ/สว่าง/มืด), **UI ไทย/อังกฤษ** (default ไทย)
- เปิดพร้อม Windows, เปิดตอนแรกมี onboarding, system tray
- อัปเดต: **ตรวจหาอัปเดตได้** (opt-in, ปิดเป็น default — เป็น network feature เดียวของแอป)

**ความปลอดภัย**
- ข้ามช่องรหัสผ่านที่ตรวจพบ, รักษา clipboard เดิม (รวมรูป/ไฟล์), กันเปิดซ้ำ (single instance),
  ดักข้อผิดพลาดไม่ให้แอปแครช

> สิ่งที่ **ยังไม่ทำ**: แก้อัตโนมัติขณะพิมพ์, AI, cloud sync + login (ต้องมี backend), ระบบ plugin
> ดู [`docs/09_Roadmap.md`](docs/09_Roadmap.md)

---

## Tech stack

| ด้าน | เลือกใช้ |
|------|----------|
| Runtime | .NET 9 |
| ภาษา | C# (เปิด nullable) |
| UI | WPF + MVVM |
| ฐานข้อมูล | SQLite |
| Data access | Dapper |
| Logging | Serilog |
| Testing | xUnit |
| สถาปัตยกรรม | Clean Architecture |

---

## โครงสร้างโปรเจกต์

```
smarttyping/
├── README.md
├── CHANGELOG.md
├── Directory.Build.props        # ตั้งค่า MSBuild ร่วม (nullable, langversion, version)
├── SmartTyping.sln
├── docs/                        # charter, PRD, SRS, architecture, DB, UI/UX, test plan, ADRs
├── src/
│   ├── SmartTyping.Domain/          # entity, value object, enum — ไม่พึ่งใคร
│   ├── SmartTyping.Application/      # use case, interface (port), DTO, service
│   ├── SmartTyping.Infrastructure/  # SQLite+Dapper, Windows hook, clipboard, injection, logging
│   ├── SmartTyping.UI/              # WPF, MVVM, views, viewmodels, tray, DI composition root
│   └── SmartTyping.Shared/          # ของใช้ร่วม (Result, guards) — ไม่พึ่ง external
├── tests/                       # unit + integration tests
├── packaging/                   # winget manifests, Inno Setup installer
├── assets/                      # ไอคอน/รูป
└── scripts/                     # build / packaging helpers
```

ทิศทาง dependency (Clean Architecture):

```
UI ──► Application ──► Domain
Infrastructure ──► Application ──► Domain
Shared ◄── (ทุกชั้นอ้าง Shared ได้)
```

Domain ไม่พึ่งใคร · Application พึ่งแค่ Domain (+ Shared) · Infrastructure และ UI พึ่งเข้าด้านใน
ไม่มีใครพึ่ง Infrastructure หรือ UI ออกด้านนอก

---

## สิ่งที่ต้องมี

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

ตรวจว่ามี SDK:

```powershell
dotnet --version   # ควรได้ 9.x
```

---

## วิธี build

จาก root ของ repo:

```powershell
dotnet restore
dotnet build -c Debug
```

Build แบบ Release:

```powershell
dotnet build -c Release
```

## วิธีรัน

```powershell
dotnet run --project src/SmartTyping.UI
```

แอปจะเปิดหน้าต่างหลัก (ตัวจัดการ snippet) + ไอคอนใน system tray · ตอนรันครั้งแรก
ฐานข้อมูล SQLite จะถูกสร้างที่ `%LOCALAPPDATA%\SmartTyping\smarttyping.db`
พร้อมค่าเริ่มต้นและ snippet ตัวอย่าง

**คีย์ลัด (default — ปรับได้ในหน้า Settings):**

- **Ctrl + Shift + L** — แปลงภาษา ไทย ↔ อังกฤษ (ข้อความที่เลือก หรือ **คำล่าสุด** ถ้าไม่ได้เลือก)
- **Ctrl + Shift + E** — ขยาย snippet ที่คำสั่งที่เลือก (เช่น เลือก `/phone` แล้วกด)
- **Ctrl + Shift + Space** — เปิด **quick-picker** ค้นหา snippet แล้วแทรกที่เคอร์เซอร์
- **Ctrl + Shift + N** — **เพิ่ม snippet จากข้อความที่เลือก**

## วิธี publish (ไฟล์เดียว self-contained)

```powershell
dotnet publish src/SmartTyping.UI -c Release /p:PublishProfile=win-x64
# → src/SmartTyping.UI/bin/Release/net9.0-windows/win-x64/publish/SmartTyping.exe
```

## วิธีสร้าง installer (Inno Setup)

```powershell
pwsh scripts/build-installer.ps1 -Version 0.1.0
# ต้องมี Inno Setup 6 (winget install JRSoftware.InnoSetup)
# → dist/SmartTyping-Setup-0.1.0.exe
```

## วิธีทดสอบ

```powershell
dotnet test
```

---

## กฎการพัฒนา

- **เปิด nullable reference types** ทุกที่ อย่าปิด
- **ใช้ async ตามความเหมาะสม** — I/O (DB, clipboard) เป็น async · การแมปที่เป็น CPU ล้วนเป็น sync
- **Dependency injection** — ห้าม `new` infrastructure ในชั้น application/UI · ลงทะเบียนที่ composition root (UI)
- **ไม่มี business logic แบบ static** ยกเว้นค่าคงที่/ตารางแมปที่ pure (เช่นตารางเลย์เอาต์คีย์บอร์ด)
- **ใช้ interface สำหรับงาน infrastructure** — ทุกขอบเขต I/O เป็น port ประกาศใน Application ทำจริงใน Infrastructure
- **Windows API อยู่ใน Infrastructure เท่านั้น** — ไม่มี P/Invoke `user32.dll` ใน Domain/Application/UI
- **ชื่อสื่อความหมาย** · ใส่ XML comment เฉพาะที่พฤติกรรมไม่ชัด
- **เขียนเทสต์ให้ logic หลัก** — converter, snippet matching, template ต้องมีเทสต์
- **Conventional Commits** — `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`
- **อย่า commit ข้อมูลผู้ใช้** — `*.db`, `logs/`, `bin/obj` ถูก git-ignore ไว้แล้ว

---

## License

[MIT](LICENSE) — เลือกเป็น default แบบอนุญาตกว้าง เปลี่ยนได้ถ้าโปรเจกต์ต้องการเงื่อนไขอื่น
