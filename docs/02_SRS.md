# 02 — Software Requirements Specification (SRS)

## 1. วัตถุประสงค์

ข้อกำหนดโดยละเอียดที่ทดสอบได้สำหรับ SmartTyping Desktop MVP เป็นส่วนเสริมของ PRD
โดยเพิ่มรายละเอียดในระดับ interface และพฤติกรรม

## 2. บริบทของระบบ

แอปพลิเคชันเดสก์ท็อปสำหรับผู้ใช้คนเดียว อินพุต: keyboard events, clipboard, local SQLite DB
เอาต์พุต: ข้อความที่ถูกแทรกเข้าไปยังแอปพลิเคชันที่อยู่เบื้องหน้า, UI, local logs

```
[User keyboard] ─► [Keyboard hook] ─► [Application services] ─► [Text injection] ─► [Foreground app]
                                            │
                                       [SQLite DB] (snippets, settings, usage)
```

## 3. External interfaces

### 3.1 User interfaces
- หน้าต่างหลัก: รายการ snippet (กรองตาม category ได้, ค้นหาได้), แถบเครื่องมือ (เพิ่ม/แก้ไข/ลบ/เปิดใช้งาน)
- ไดอะล็อกเพิ่ม/แก้ไข snippet: trigger, เนื้อหา (หลายบรรทัด), category, สถานะเปิดใช้งาน
- หน้าตั้งค่า: ปุ่มสลับสำหรับการขยายข้อความ (expansion) และการแก้ไขภาษา
- ไอคอน System tray: แสดง/ซ่อนหน้าต่าง, สลับเปิดปิดฟีเจอร์, ออกจากโปรแกรม

### 3.2 Hardware/OS interfaces
- Low-level keyboard hook (`WH_KEYBOARD_LL`) ผ่าน Infrastructure เท่านั้น
- การลงทะเบียน global hotkey (`RegisterHotKey`) สำหรับ `Ctrl+Shift+L`
- อ่าน/เขียน clipboard; จำลอง keystrokes / paste สำหรับการแทรกข้อความ

### 3.3 Software interfaces
- SQLite ผ่าน Dapper
- Serilog file sink ภายใต้ `%LOCALAPPDATA%\SmartTyping\logs`

## 4. Functional requirements (รายละเอียด)

### 4.1 Snippet engine
- SRS-1 ระบบ SHALL จัดเก็บ snippet ด้วยฟิลด์: Id, Trigger, Content, CategoryId (nullable),
  IsEnabled, UsageCount, CreatedUtc, UpdatedUtc
- SRS-2 Triggers SHALL ต้องไม่ซ้ำกัน (case-insensitive) และต้องไม่ว่างเปล่า
- SRS-3 `SnippetExpansionService.TryExpand(trigger)` SHALL คืนค่าเนื้อหาที่ถูกแปลงแล้ว
  (ใส่ตัวแปร template แล้ว) สำหรับ snippet ที่เปิดใช้งานอยู่ หรือคืนผลลัพธ์แบบไม่พบข้อมูลในกรณีอื่น
- SRS-4 เมื่อการขยายสำเร็จ service SHALL เพิ่มค่า UsageCount และเพิ่มระเบียน UsageHistory

### 4.2 Template engine
- SRS-5 `ITemplateEngine.Render(content)` SHALL แทนที่ `{date}`, `{time}`, `{clipboard}`
  และคง token ที่ไม่รู้จักไว้ตามเดิม
- SRS-6 การจับคู่ token SHALL เป็นแบบ case-insensitive (`{Date}` == `{date}`)

### 4.3 Layout converter
- SRS-7 `IKeyboardLayoutConverter.Convert(text, direction)` SHALL แมปอักขระแต่ละตัวโดยใช้
  ตาราง Kedmanee↔QWERTY; อักขระที่ไม่มีในแมปจะผ่านไปตามเดิม
- SRS-8 `DetectDirection(text)` SHALL คืนทิศทางการแปลงที่น่าจะเป็นไปได้ โดยอิงจากอัตราส่วนของ
  อักขระไทยเทียบกับอักขระละติน
- SRS-9 การแปลง SHALL เป็น pure function (ไม่มี I/O, ไม่มี side effects) และผ่าน unit test อย่างครบถ้วน

### 4.4 Settings
- SRS-10 Settings SHALL เป็นแถวแบบ key/value (`AppSetting`) ที่โหลดตอนเริ่มต้นและถูก cache ไว้
- SRS-11 การเปลี่ยนปุ่มสลับ SHALL บันทึกทันที

### 4.5 Input & injection (Infrastructure)
- SRS-12 keyboard hook SHALL ถูกกำหนดนามธรรมไว้เบื้องหลัง `IKeyboardHook` (start/stop, key events)
- SRS-13 การแทรกข้อความ SHALL ถูกกำหนดนามธรรมไว้เบื้องหลัง `ITextInjector` พร้อม fallback แบบ clipboard-paste
- SRS-14 ตัวจัดการ hotkey SHALL รันการแปลงนอก UI thread และ marshal การอัปเดต UI กลับมา

## 5. Non-functional requirements

- SRS-15 เปิดใช้งาน nullable reference types; ไม่มีการระงับ (suppress) คำเตือน nullability ในเลเยอร์หลัก
- SRS-16 ไม่มีการเรียก Windows API นอก Infrastructure
- SRS-17 แอป SHALL เริ่มทำงานได้แม้ว่าจะไม่มี DB (ระบบจะ self-initialize)
- SRS-18 ข้อยกเว้น (exception) ทั้งหมดใน hooks/injection SHALL ถูกบันทึก log และ SHALL NOT ทำให้แอปล่ม

## 6. Data requirements

ดู [`04_Database.md`](04_Database.md) สำหรับ schema ข้อมูลผู้ใช้จะไม่ถูกส่งออกจากอุปกรณ์เลย

## 7. ข้อจำกัดและข้อสมมติ

- session ผู้ใช้เบื้องหน้าเพียงหนึ่งเดียว; ไม่รองรับการรันหลาย instance พร้อมกันใน MVP
- ผู้ใช้มีสิทธิ์ในการรัน low-level hooks (standard user เพียงพอบน Windows 10/11)
- บางแอปพลิเคชัน (elevated/secure desktops, ช่องรหัสผ่าน) อาจบล็อกการแทรกข้อความ — แอปจะลดระดับการทำงานลงอย่างนุ่มนวล (degrade gracefully)

## 8. Traceability (PRD → SRS)

| PRD | SRS            |
|-----|----------------|
| FR-1..FR-5 | SRS-1..SRS-4 |
| FR-6..FR-9 | SRS-7..SRS-9 |
| FR-10..FR-11 | SRS-5..SRS-6 |
| FR-12..FR-13 | SRS-10..SRS-11 |
