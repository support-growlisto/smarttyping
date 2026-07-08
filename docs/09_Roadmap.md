# 09 — Roadmap

มุมมองที่มีชีวิตของทิศทางที่ SmartTyping Desktop กำลังมุ่งไป ลำดับเรียงตามความสำคัญ ไม่ใช่วันที่ตายตัว

## ตอนนี้ — MVP (v0.1)

- [x] โครงสร้าง Clean Architecture + เอกสาร
- [ ] Snippet CRUD, หมวดหมู่, เปิด/ปิดใช้งาน, นับจำนวนการใช้งาน
- [ ] การขยาย snippet (ด้วยการสั่งงานอย่างชัดเจน) พร้อมตัวแปรเทมเพลต `{date}`, `{time}`, `{clipboard}`
- [ ] การแปลง Thai Kedmanee ↔ English QWERTY ผ่าน `Ctrl+Shift+L`
- [ ] การตั้งค่าแบบ toggle (การขยาย, การแก้ไขภาษา) บันทึกใน SQLite
- [ ] Unit tests สำหรับตัวแปลง, template engine, การค้นหา snippet
- [ ] เชลล์ของ system tray

## ถัดไป (v0.2)

- ~~Configurable hotkeys (rebind the conversion/expansion keys)~~ — **done** (v0.1): hotkeys ทั้งสี่ปุ่ม rebind ได้ใน Settings
- ~~Start with Windows (registry/Startup entry, user opt-in)~~ — **done** (v0.1)
- ~~User-input placeholders in templates (`{input:Label}` prompts a small dialog)~~ — **done** (v0.1)
- ~~Import/export snippets (JSON)~~ — **done** (v0.1): การจัดการความขัดแย้งแบบ skip/overwrite, การสร้างหมวดหมู่ขึ้นใหม่
- ~~Preview / test-expand and richer template variables (`{date:fmt}`, `{date+N}`, `{cursor}`)~~ — **done** (v0.1)
- ~~Convert the *last typed word* without a selection~~ — **done** (v0.1): เลือกคำก่อนหน้าให้อัตโนมัติ
- ~~Snippet quick-picker / command palette (fuzzy search + insert)~~ — **done** (v0.1): Ctrl+Shift+Space
- การตรวจจับทิศทางที่ดีขึ้น และตัวเลือกแบบ manual "แปลงเป็นภาษาไทย / เป็นภาษาอังกฤษ"

## ที่เพิ่งส่งมอบไป (v0.1, เกินกว่า MVP เดิม)

- Dark mode (ตามระบบ + แบบ manual), localization ภาษาไทย/อังกฤษ
- placeholders `{input:Label}`, `{cursor}`, รูปแบบและ offset ของวันที่/เวลา
- Quick-picker (fuzzy search), convert-last-word, add-snippet-from-selection
- hotkeys ปรับแต่งได้, การจัดการหมวดหมู่, สถิติการใช้งาน
- Import/Export, onboarding, start-with-Windows, single-instance, การป้องกัน secure-field

## ภายหลัง (v0.3–v1.0)

- integration tests ของ repository + การทำให้ migration framework แข็งแกร่งขึ้น
- การ inject ข้อความที่เชื่อถือได้มากขึ้นในแอปหลากหลายมากขึ้น; ขยายการตรวจจับ secure-field ให้ครอบคลุมเกินกว่ากล่องรหัสผ่าน `Edit`
  แบบเนทีฟ (ที่ข้ามไปแล้ว) ไปยัง browsers/Electron/UWP ผ่าน UI Automation `IsPassword`
- การปรับปรุงการค้นหา snippet (fuzzy, ล่าสุด, รายการโปรด)
- การขยายอัตโนมัติขณะพิมพ์แบบ **opt-in** ที่เลือกได้ (ยังคงไม่ทำลายข้อมูลโดยไม่มีการยืนยันแบบ inline)
- ~~Installer (Inno Setup), auto-update~~ — **done** (v0.1): ตัวติดตั้งแบบ per-user + การตรวจสอบอัปเดตแบบ opt-in ส่วน MSIX/Store ยังคงไว้ทีหลัง
- Cloud sync + accounts — เลื่อนออกไป (ต้องใช้ hosted backend + OAuth); ดู non-goals ในเอกสาร charter

## เลื่อนออกไป — อยู่นอกขอบเขตอย่างชัดเจนจนกว่าจะถึงเวลา

สิ่งเหล่านี้ตั้งใจ **ไม่** สร้างในตอนนี้ (ดูข้อจำกัดใน charter):

- **การแก้ไขอัตโนมัติขณะพิมพ์** — MVP ใช้ hotkey/manual เท่านั้น
- **การแก้ไข/แนะนำโดยใช้ AI** — ไม่มี AI ใน MVP
- **Cloud sync / multi-device** — เป็นแบบ local-first เท่านั้น
- **ระบบ plugin** — ดู ADR-004; ให้ core เรียบง่ายก่อน
- **macOS / Linux** — รองรับเฉพาะ Windows ในตอนนี้

## ข้อจำกัดเชิงแนวทาง (ยึดถือต่อไป)

- อย่าทำการแทนที่แบบทำลายข้อมูลทั่วทั้งระบบโดยไม่มีการสั่งงานจากผู้ใช้อย่างชัดเจน
- เก็บทุกอย่างไว้ในเครื่องและเป็นส่วนตัวโดยค่าเริ่มต้น
- ให้ตรรกะหลักเป็น pure และมีการทดสอบ; เก็บโค้ดที่เจาะจงกับ OS ไว้หลัง interfaces
