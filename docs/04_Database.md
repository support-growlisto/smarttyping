# 04 — ฐานข้อมูล

## 1. เอนจิน

SQLite เข้าถึงผ่าน Dapper ใช้ฐานข้อมูลแบบไฟล์เดียวต่อผู้ใช้หนึ่งคน:
`%LOCALAPPDATA%\SmartTyping\smarttyping.db` เปิดใช้งานโหมด WAL เพื่อรองรับการทำงานพร้อมกันและความคงทนของข้อมูล

Schema จะถูกสร้างขึ้นในการรันครั้งแรกโดย `DatabaseInitializer` (ใช้ `CREATE TABLE IF NOT EXISTS` แบบ idempotent)

## 2. Schema

### 2.1 `categories`
| Column       | Type     | Notes                          |
|--------------|----------|--------------------------------|
| Id           | INTEGER  | PK, autoincrement              |
| Name         | TEXT     | NOT NULL, ไม่ซ้ำ                |
| SortOrder    | INTEGER  | NOT NULL, ค่าเริ่มต้น 0         |
| CreatedUtc   | TEXT     | ISO-8601 UTC                   |

### 2.2 `snippets`
| Column       | Type     | Notes                                            |
|--------------|----------|--------------------------------------------------|
| Id           | INTEGER  | PK, autoincrement                                |
| Trigger      | TEXT     | NOT NULL, ไม่ซ้ำ (ไม่แยกตัวพิมพ์ใหญ่-เล็กด้วย NOCASE) |
| Content      | TEXT     | NOT NULL                                         |
| CategoryId   | INTEGER  | NULL, FK → categories(Id) ON DELETE SET NULL     |
| IsEnabled    | INTEGER  | NOT NULL, ค่าเริ่มต้น 1 (boolean 0/1)            |
| UsageCount   | INTEGER  | NOT NULL, ค่าเริ่มต้น 0                          |
| CreatedUtc   | TEXT     | ISO-8601 UTC                                     |
| UpdatedUtc   | TEXT     | ISO-8601 UTC                                     |

Index: `UNIQUE INDEX ux_snippets_trigger ON snippets(Trigger COLLATE NOCASE)`

### 2.3 `usage_history`
| Column       | Type     | Notes                                   |
|--------------|----------|-----------------------------------------|
| Id           | INTEGER  | PK, autoincrement                       |
| SnippetId    | INTEGER  | NOT NULL, FK → snippets(Id) ON DELETE CASCADE |
| UsedUtc      | TEXT     | ISO-8601 UTC                            |

### 2.4 `app_settings`
| Column       | Type     | Notes                          |
|--------------|----------|--------------------------------|
| Key          | TEXT     | PK                             |
| Value        | TEXT     | NOT NULL                       |

## 3. DDL (เอกสารอ้างอิงหลัก)

```sql
PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS categories (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Name       TEXT    NOT NULL,
    SortOrder  INTEGER NOT NULL DEFAULT 0,
    CreatedUtc TEXT    NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_categories_name ON categories(Name COLLATE NOCASE);

CREATE TABLE IF NOT EXISTS snippets (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Trigger    TEXT    NOT NULL,
    Content    TEXT    NOT NULL,
    CategoryId INTEGER NULL REFERENCES categories(Id) ON DELETE SET NULL,
    IsEnabled  INTEGER NOT NULL DEFAULT 1,
    UsageCount INTEGER NOT NULL DEFAULT 0,
    CreatedUtc TEXT    NOT NULL,
    UpdatedUtc TEXT    NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_snippets_trigger ON snippets(Trigger COLLATE NOCASE);

CREATE TABLE IF NOT EXISTS usage_history (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    SnippetId INTEGER NOT NULL REFERENCES snippets(Id) ON DELETE CASCADE,
    UsedUtc   TEXT    NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_usage_snippet ON usage_history(SnippetId);

CREATE TABLE IF NOT EXISTS app_settings (
    Key   TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

## 4. ข้อมูลเริ่มต้น (Seed data)

ในการรันครั้งแรก ตัวเริ่มต้นระบบจะแทรกข้อมูลต่อไปนี้ (แบบ idempotent):

- Settings: `SnippetExpansionEnabled=true`, `LanguageCorrectionEnabled=true`, `SchemaVersion=1`
- category ชื่อ `General` หนึ่งรายการ
- snippet ตัวอย่าง: `/sig`, `/phone`, `/date` (`Today is {date}`) — เพื่อให้สาธิตฟีเจอร์ได้ทันที

## 5. ข้อกำหนดการใช้งาน (Conventions)

- timestamp ทั้งหมดจัดเก็บเป็นสตริง ISO-8601 UTC (รูปแบบ `O`) เพื่อความสามารถในการพกพาข้ามระบบ
- ค่า boolean จัดเก็บเป็นจำนวนเต็ม `0/1`
- Migration: ติดตามด้วยค่าตั้ง `SchemaVersion` โดย MVP มีเพียงเวอร์ชัน 1 เท่านั้น การเปลี่ยนแปลง
  schema ในอนาคตจะเพิ่มหมายเลขเวอร์ชันและรันขั้นตอน migration ตามลำดับใน `DatabaseInitializer`

## 6. การสำรองข้อมูล / รีเซ็ต

ฐานข้อมูลเป็นไฟล์เดียว ผู้ใช้สามารถคัดลอกเพื่อสำรองข้อมูลได้ "รีเซ็ต" = ลบไฟล์แล้วเริ่มโปรแกรมใหม่
การล้างประวัติการใช้งาน = `DELETE FROM usage_history`
