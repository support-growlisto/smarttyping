# 04 — Database

## 1. Engine

SQLite, accessed via Dapper. One file database per user:
`%LOCALAPPDATA%\SmartTyping\smarttyping.db`. WAL mode enabled for concurrency/durability.

The schema is created on first run by `DatabaseInitializer` (idempotent `CREATE TABLE IF NOT EXISTS`).

## 2. Schema

### 2.1 `categories`
| Column       | Type     | Notes                          |
|--------------|----------|--------------------------------|
| Id           | INTEGER  | PK, autoincrement              |
| Name         | TEXT     | NOT NULL, unique               |
| SortOrder    | INTEGER  | NOT NULL, default 0            |
| CreatedUtc   | TEXT     | ISO-8601 UTC                   |

### 2.2 `snippets`
| Column       | Type     | Notes                                            |
|--------------|----------|--------------------------------------------------|
| Id           | INTEGER  | PK, autoincrement                                |
| Trigger      | TEXT     | NOT NULL, unique (case-insensitive via NOCASE)   |
| Content      | TEXT     | NOT NULL                                         |
| CategoryId   | INTEGER  | NULL, FK → categories(Id) ON DELETE SET NULL     |
| IsEnabled    | INTEGER  | NOT NULL, default 1 (boolean 0/1)                |
| UsageCount   | INTEGER  | NOT NULL, default 0                              |
| CreatedUtc   | TEXT     | ISO-8601 UTC                                     |
| UpdatedUtc   | TEXT     | ISO-8601 UTC                                     |

Index: `UNIQUE INDEX ux_snippets_trigger ON snippets(Trigger COLLATE NOCASE)`.

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

## 3. DDL (authoritative reference)

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

## 4. Seed data

On first run the initializer inserts (idempotently):

- Settings: `SnippetExpansionEnabled=true`, `LanguageCorrectionEnabled=true`, `SchemaVersion=1`.
- A `General` category.
- Sample snippets: `/sig`, `/phone`, `/date` (`Today is {date}`) — so the feature is demonstrable immediately.

## 5. Conventions

- All timestamps stored as ISO-8601 UTC strings (`O` format) for portability.
- Booleans stored as `0/1` integers.
- Migrations: tracked by the `SchemaVersion` setting; the MVP only has version 1. Future
  schema changes bump the version and run ordered migration steps in `DatabaseInitializer`.

## 6. Backup / reset

The DB is a single file; users can copy it to back up. "Reset" = delete the file and restart.
Clearing usage history = `DELETE FROM usage_history`.
