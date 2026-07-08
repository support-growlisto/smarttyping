# 08 — แผนการปล่อยเวอร์ชัน (Release Plan)

## 1. การกำหนดเวอร์ชัน (Versioning)

ใช้ Semantic Versioning (`MAJOR.MINOR.PATCH`) โดย MVP ตั้งเป้าที่ **0.1.0** เวอร์ชันถูกกำหนดไว้ใน
`Directory.Build.props` และแสดงผลในหน้า About/tooltip ของ tray

## 2. ช่องทางการปล่อยเวอร์ชัน (Release channels)

| ช่องทาง | วัตถุประสงค์                       | รอบการปล่อย        |
|---------|----------------------------------|------------------|
| dev     | บิลด์ในเครื่อง, ตรวจสอบ PR         | ต่อเนื่อง          |
| preview | ผู้ทดสอบภายใน                     | ต่อ milestone     |
| stable  | ผู้ใช้งานปลายทาง                   | เมื่อ milestone GA |

## 3. การบิลด์และเผยแพร่ (Build & publish)

แบบ framework-dependent (dev):
```powershell
dotnet build -c Release
```

แบบ self-contained single-file (release candidate):
```powershell
dotnet publish src/SmartTyping.UI -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```
ผลลัพธ์: `src/SmartTyping.UI/bin/Release/net9.0-windows/win-x64/publish/`

## 4. การแพ็กเกจ (Packaging) (MVP)

- ส่งมอบไฟล์เดียว `SmartTyping.exe` จาก publish profile (จะบีบอัดเป็น zip ก็ได้)
- **winget**: portable manifests อยู่ใน `packaging/winget/` — กรอก release URL + SHA-256 แล้ว
  `winget validate` / submit เพื่อให้ผู้ใช้สามารถ `winget install SmartTyping.SmartTyping` ได้ ดู README ในโฟลเดอร์นั้น
- **ตัวติดตั้ง (Inno Setup)**: `packaging/installer/SmartTyping.iss` + `scripts/build-installer.ps1`
  สร้างตัวติดตั้งแบบ per-user `SmartTyping-Setup-<version>.exe` (Start Menu / shortcut บน Desktop แบบเลือกได้,
  ตัวถอนการติดตั้ง) ต้องใช้ Inno Setup 6 (`iscc`) ส่วน MSIX/Store ถูกเลื่อนออกไป (ต้องใช้ signing cert)

## 4a. การอัปเดต (Updates)

- **การตรวจสอบอัปเดตในแอป** (opt-in, ปิดไว้เป็นค่าเริ่มต้น — เป็นฟีเจอร์เดียวของแอปที่ใช้เครือข่าย) เมื่อเปิดใช้งาน
  มันจะสอบถามที่ GitHub endpoint "latest release" ตอนเริ่มโปรแกรมและผ่าน Settings -> "Check now"
  โดยเปรียบเทียบเวอร์ชันด้วย `UpdateComparer` แบบ pure หากมีเวอร์ชันใหม่กว่า จะเสนอให้เปิดหน้าดาวน์โหลด
- ตั้งค่า `WindowsUpdateChecker.LatestReleaseUrl` ให้ชี้ไปยัง repository จริงก่อนปล่อยเวอร์ชัน

## 5. รายการตรวจสอบก่อนปล่อยเวอร์ชัน (Pre-release checklist)

- [ ] `dotnet test` ผ่านทั้งหมด (green)
- [ ] รายการตรวจสอบ hook/injection แบบ manual (ดู Test Plan §4) ผ่านทั้งบน Win10 + Win11
- [ ] เพิ่มเวอร์ชันใน `Directory.Build.props`
- [ ] อัปเดต `CHANGELOG.md` แล้ว; ย้าย `[Unreleased]` ไปอยู่ใต้เวอร์ชันใหม่ + วันที่
- [ ] ทดสอบ smoke test บนเครื่องใหม่ (ไม่มี dev tools): DB เริ่มต้นตัวเองได้, แอปทำงานได้
- [ ] logs ไม่มี unhandled exception ใด ๆ ระหว่างการ smoke test

## 6. ขั้นตอนการปล่อยเวอร์ชัน (Release steps)

1. ติด tag: `git tag v0.1.0 && git push --tags`
2. บิลด์ publish artifact แบบ self-contained
3. บีบอัดเป็น zip และแนบไปกับหน้า release พร้อมข้อความส่วนตัดตอนจาก `CHANGELOG`
4. ทดสอบ smoke-test กับ artifact บน VM ที่สะอาด

## 7. การย้อนกลับ (Rollback)

Artifacts นั้น immutable ต่อแต่ละ tag หากต้องการย้อนกลับ ให้ re-publish zip ของ tag ก่อนหน้า ข้อมูลผู้ใช้
(SQLite DB) เข้ากันได้ทั้งแบบ forward/backward ภายในสาย MINOR เดียวกัน; การเพิ่มเวอร์ชัน schema เป็นแบบเพิ่มเข้ามา (additive)

## 8. Milestones

| เวอร์ชัน | เนื้อหา                                                        |
|---------|--------------------------------------------------------------|
| 0.1.0   | MVP: snippets + templates + การแปลง layout + settings         |
| 0.2.0   | hotkeys ปรับแต่งได้, start-with-Windows, ปรับปรุง tray          |
| 0.3.0   | integration tests ของ repository, import/export snippets      |
| 1.0.0   | injection ที่แข็งแกร่งขึ้น, ตัวติดตั้ง, auto-detect แบบเลือกได้ (opt-in) |
