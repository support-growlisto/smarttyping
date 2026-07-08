# ADR-003 — snippet engine และการ render template

- Status: Accepted
- Date: 2026-07-08

## บริบท

Snippet ขยาย trigger สั้น ๆ (เช่น `/phone`) ให้กลายเป็นข้อความที่เก็บไว้ซึ่งอาจมีตัวแปร
template (`{date}`, `{time}`, `{clipboard}`) เราต้องการการ lookup, การ render, การติดตามการใช้งาน และ
การเปิด/ปิด — โดยให้เรียบง่าย ปลอดภัย และทดสอบได้ ยังไม่มีพฤติกรรมแบบพิมพ์ไปขยายไปอัตโนมัติ

## การตัดสินใจ

1. **โมเดลของ trigger:** trigger คือ token สั้น ๆ ที่ไม่ซ้ำ (prefix ตั้งต้นคือ `/`) ความไม่ซ้ำและ
   การจับคู่เป็นแบบ **ไม่สนตัวพิมพ์เล็กใหญ่** (SQLite `COLLATE NOCASE` + การ lookup แบบ case-insensitive)
2. **การขยายแบบชัดแจ้ง:** `SnippetExpansionService.TryExpandAsync(trigger)` ทำ lookup →
   render → ติดตามการใช้งาน และคืนค่าผลลัพธ์ มันถูกเรียกเมื่อผู้ใช้กระทำ ไม่เคยเรียกอัตโนมัติ
3. **การ render เป็น port บริสุทธิ์แยกต่างหาก** `ITemplateEngine.Render(content)`:
   - แทนที่ token ที่รู้จัก `{date}`, `{time}`, `{clipboard}` (ไม่สนตัวพิมพ์เล็กใหญ่)
   - ปล่อย token ที่ **ไม่รู้จัก** ไว้เฉย ๆ (ไม่พัง ไม่สูญเสียข้อมูล) — รองรับตัวแปรในอนาคตแบบ forward-compatible
   - ขึ้นกับ `IDateTimeProvider` และ `IClipboardService` เพื่อให้ time/clipboard สามารถ inject และทดสอบได้
4. **การติดตามการใช้งาน:** เมื่อขยายสำเร็จ ให้เพิ่ม `UsageCount` และเพิ่มแถว `UsageHistory`
   ในเส้นทางเรียก repository เดียว
5. **snippet ที่ถูกปิดไว้จะไม่ขยายเด็ดขาด**

## ผลที่ตามมา

- **ข้อดี**: การ render เป็นฟังก์ชันบริสุทธิ์ที่ inject ได้ → unit-test ได้เต็มที่ด้วย clock ที่ตายตัวและ clipboard ปลอม
- **ข้อดี**: การปล่อยผ่าน token ที่ไม่รู้จัก หมายความว่าการเพิ่ม `{input:...}` ในภายหลังจะไม่ทำให้อะไรพัง
- **ข้อดี**: การแยกที่ชัดเจน — การ lookup/ติดตาม (service + repo) เทียบกับการ render (template engine)
- **ข้อเสีย**: การ parse template เป็นเพียงการสแกน token อย่างง่าย ไม่ใช่ภาษา expression เต็มรูปแบบ (โดยตั้งใจ)

## ทางเลือกที่พิจารณา

- **String.Format / interpolation** — เปราะเมื่อเนื้อหาของผู้ใช้มีวงเล็บปีกกา; ปฏิเสธ
- **ไลบรารี templating เต็มรูปแบบ (เช่น Handlebars/Scriban)** — เกินความจำเป็นสำหรับตัวแปรสามตัว; เพิ่ม
  dependency และเส้นโค้งการเรียนรู้ กลับมาพิจารณาใหม่เฉพาะเมื่อ template ซับซ้อนขึ้น
- **การ auto-expand แบบ inline เมื่อพิมพ์ trigger** — นั่นคือพฤติกรรมอัตโนมัติ เลื่อนออกไปตามข้อจำกัดของโปรเจกต์
