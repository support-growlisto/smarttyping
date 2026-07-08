# 06 — แผนการทดสอบ

## 1. ขอบเขต

มุ่งเน้นการทดสอบ MVP ไปที่ **ตรรกะแกนกลางล้วนๆ (pure core logic)** ซึ่งเป็นจุดที่บั๊กสร้างความเสียหายมากที่สุดและการทดสอบมีต้นทุนต่ำที่สุด:
การแปลง keyboard layout, การค้นหา/ขยาย snippet และการแทนที่ตัวแปรใน template
ส่วนการเชื่อมต่อกับ Windows API (hooks, injection) จะตรวจสอบด้วยตนเองในช่วง MVP และทำระบบอัตโนมัติในภายหลัง

## 2. ระดับการทดสอบ

| ระดับ        | ทดสอบอะไร                                          | เครื่องมือ       |
|--------------|---------------------------------------------------|-----------------|
| Unit         | Converter, template engine, การจับคู่ snippet, ตรรกะ settings | xUnit |
| Component    | application services พร้อม fake ports แบบ in-memory | xUnit + fakes   |
| Integration  | Dapper repositories ทดสอบกับไฟล์ SQLite ชั่วคราว (ภายหลัง) | xUnit (เลื่อนออกไป) |
| Manual/E2E   | การแปลงและ injection ผ่าน hotkey ในแอปจริง         | เช็กลิสต์ทดสอบ  |

## 3. กรณีทดสอบระดับ unit (MVP — อัตโนมัติ)

### 3.1 ตัวแปลง keyboard layout
| ID    | Input           | Direction        | ผลลัพธ์ที่คาดหวัง    |
|-------|-----------------|------------------|---------------------|
| KC-1  | `l;ylfu`        | EnToThai         | `สวัสดี`            |
| KC-2  | `สวัสดี`        | ThaiToEn         | `l;ylfu`            |
| KC-3  | round-trip      | EN→TH→EN         | คงค่าเดิมไว้ครบถ้วน  |
| KC-4  | `hello 123`     | (ไม่มีการจับคู่)   | ตัวเลข/ช่องว่างไม่เปลี่ยน |
| KC-5  | auto-detect     | ข้อความส่วนใหญ่เป็นภาษาไทย | direction = ThaiToEn |

### 3.2 Template engine
| ID    | Content              | ผลลัพธ์ที่คาดหวัง                 |
|-------|----------------------|----------------------------------|
| TE-1  | `Today is {date}`    | `Today is <short date>`          |
| TE-2  | `Now: {time}`        | `Now: <short time>`              |
| TE-3  | `{clipboard}`        | ข้อความปัจจุบันใน clipboard        |
| TE-4  | `{unknown}`          | `{unknown}` (ไม่เปลี่ยนแปลง)      |
| TE-5  | `{Date}` (พิมพ์ผสมเล็กใหญ่)| ถูกแทนที่ (ไม่สนใจตัวพิมพ์เล็ก/ใหญ่)      |

### 3.3 การขยาย snippet
| ID    | สถานการณ์                                   | ผลลัพธ์ที่คาดหวัง                       |
|-------|--------------------------------------------|---------------------------------------|
| SE-1  | มี snippet ที่เปิดใช้งานอยู่                  | คืนค่าเนื้อหาที่ render แล้ว              |
| SE-2  | snippet ที่ปิดใช้งาน                          | ไม่มีการขยาย                          |
| SE-3  | trigger ที่ไม่รู้จัก                          | ไม่มีการจับคู่                          |
| SE-4  | trigger ไม่สนใจตัวพิมพ์เล็ก/ใหญ่ (`/Sig` เทียบ `/sig`)| จับคู่ได้                               |
| SE-5  | ขยายสำเร็จ                                  | usage count เพิ่มขึ้น, เพิ่ม history เข้าไป |

## 4. เช็กลิสต์ทดสอบด้วยตนเอง (hooks/injection)

- [ ] `Ctrl+Shift+L` แปลงข้อความที่เลือกใน Notepad, WordPad และช่องข้อความในเบราว์เซอร์
- [ ] การแปลงล้มเหลวอย่างนุ่มนวลเมื่อไม่สามารถจับข้อความที่เลือกได้
- [ ] Injection ใช้ fallback แบบ clipboard-paste เมื่อการพิมพ์โดยตรงล้มเหลว
- [ ] แอปไม่แครชหาก hook เกิด exception; มีการบันทึก error ไว้
- [ ] Tray สลับเปิด/ปิดฟีเจอร์ได้แบบเรียลไทม์

## 5. ความเป็นดีเทอร์มินิสติก (Determinism)

- การทดสอบที่ขึ้นกับเวลาจะฉีด `IDateTimeProvider` แบบ fake (นาฬิกาคงที่) — ไม่พึ่งพา `DateTime.Now`
- การทดสอบที่ขึ้นกับ clipboard ใช้ `IClipboardService` แบบ fake

## 6. เกณฑ์การผ่าน (MVP)

- automated unit tests ทั้งหมดผ่าน (`dotnet test` เป็นสีเขียว)
- เช็กลิสต์ทดสอบด้วยตนเองผ่านบนเครื่อง Windows 10 และ Windows 11
- ไม่มี unhandled exceptions ใน logs ระหว่างการทดสอบ smoke นาน 15 นาที

## 7. เป้าหมายความครอบคลุม (Coverage)

- Converter, template engine, การขยาย snippet: ครอบคลุม branch ประมาณ 100%
- Repositories/hooks: ไม่กำหนดเป็นเงื่อนไขบังคับสำหรับ MVP; เพิ่ม integration tests ก่อนถึง v1.0
