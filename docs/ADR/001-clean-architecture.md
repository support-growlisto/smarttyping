# ADR-001 — เลือกใช้ Clean Architecture

- Status: Accepted
- Date: 2026-07-08

## บริบท

SmartTyping ปะปนตรรกะบริสุทธิ์ (การแปลง layout, การ render template, การจับคู่ snippet)
เข้ากับส่วนที่ผูกกับ OS และทดสอบยาก (global keyboard hooks, clipboard, การฉีดข้อความ)
รวมถึง WPF UI เข้าด้วยกัน เราต้องการให้ตรรกะบริสุทธิ์สามารถ unit-test ได้อย่างง่ายดาย และให้รายละเอียดฝั่ง OS/UI
สามารถเปลี่ยนแทนได้โดยไม่ต้องแตะกฎทางธุรกิจ

## การตัดสินใจ

ใช้ **Clean Architecture** โดยให้ dependency ชี้เข้าด้านใน และมี project เหล่านี้:

- `SmartTyping.Domain` — entities, value objects, enums ไม่ขึ้นกับสิ่งใด (ยกเว้น `Shared`)
- `SmartTyping.Application` — use-case services, ports (interfaces), DTOs และตัว
  converter/template engine บริสุทธิ์ ขึ้นกับ Domain + Shared เท่านั้น
- `SmartTyping.Infrastructure` — implement ports: SQLite/Dapper, Windows hooks, clipboard, การฉีดข้อความ, logging
- `SmartTyping.UI` — WPF/MVVM และ DI composition root เพียงจุดเดียว
- `SmartTyping.Shared` — primitives แบบ cross-cutting (`Result`, guards) ไม่มี dependency ภายนอก

ตัว converter และ template engine บริสุทธิ์อยู่ใน Application (ไม่ใช่ Infrastructure) เพราะ
ไม่มี I/O — ทำให้มันปลอด dependency และทดสอบได้เร็ว

## ผลที่ตามมา

- **ข้อดี**: ตรรกะแกนกลางถูกทดสอบได้โดยไม่ต้องใช้ Windows APIs; Infrastructure เปลี่ยนแทนได้; ขอบเขตชัดเจน
- **ข้อดี**: DI composition root เป็นที่เดียวที่รู้จัก implementation จริง
- **ข้อเสีย**: มี project เยอะขึ้นและมีพิธีรีตองบ้าง (interfaces + DTOs) สำหรับแอปเดสก์ท็อปขนาดเท่านี้
- **การบรรเทา**: เก็บ `Shared` และ DTOs ให้น้อยที่สุด; อย่าเพิ่ม layer หรือ abstraction ที่เราไม่ได้ใช้

## ทางเลือกที่พิจารณา

- **WPF project เดียว (code-behind + services)** — เริ่มได้เร็วที่สุด แต่ตรรกะบริสุทธิ์จะ
  พันกันยุ่งกับ WPF และ Win32 ทำให้การทดสอบเจ็บปวด ปฏิเสธ
- **MVVM + โฟลเดอร์ services (ไม่มี layer projects)** — ดีขึ้น แต่ไม่มีอะไรบังคับทิศทางของ dependency
  ทำให้ Infrastructure รั่วเข้าสู่ตรรกะทางธุรกิจเมื่อเวลาผ่านไป ปฏิเสธเพราะเรื่องการดูแลรักษา
