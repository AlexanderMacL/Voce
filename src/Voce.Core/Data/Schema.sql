PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS profiles (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    label               TEXT    NOT NULL,
    singer_name         TEXT    NOT NULL DEFAULT '',
    voice_part          TEXT    NOT NULL DEFAULT '',
    address             TEXT    NOT NULL DEFAULT '',
    email               TEXT    NOT NULL DEFAULT '',
    phone               TEXT    NOT NULL DEFAULT '',
    utr                 TEXT    NOT NULL DEFAULT '',
    bank_name           TEXT    NOT NULL DEFAULT '',
    account_name        TEXT    NOT NULL DEFAULT '',
    sort_code           TEXT    NOT NULL DEFAULT '',
    account_number      TEXT    NOT NULL DEFAULT '',
    payment_terms_days  INTEGER NOT NULL DEFAULT 30,
    first_line_template TEXT    NOT NULL DEFAULT '{NAME}	{VOICE}	{FEE}',
    is_default          INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS employers (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    name         TEXT    NOT NULL,
    contact_name TEXT    NOT NULL DEFAULT '',
    address      TEXT    NOT NULL DEFAULT '',
    email        TEXT    NOT NULL DEFAULT '',
    notes        TEXT    NOT NULL DEFAULT '',
    is_archived  INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS events (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    employer_id       INTEGER NOT NULL REFERENCES employers(id) ON DELETE CASCADE,
    name              TEXT    NOT NULL,
    default_fee_pence INTEGER NULL,
    is_archived       INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS invoices (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    number              TEXT    NOT NULL UNIQUE,
    issue_date          TEXT    NOT NULL,
    due_date            TEXT    NOT NULL,
    event_date          TEXT    NULL,
    employer_id         INTEGER NULL REFERENCES employers(id) ON DELETE SET NULL,
    profile_id          INTEGER NULL REFERENCES profiles(id) ON DELETE SET NULL,
    bill_to_name        TEXT    NOT NULL DEFAULT '',
    bill_to_address     TEXT    NOT NULL DEFAULT '',
    from_singer_name    TEXT    NOT NULL DEFAULT '',
    from_voice_part     TEXT    NOT NULL DEFAULT '',
    from_address        TEXT    NOT NULL DEFAULT '',
    from_email          TEXT    NOT NULL DEFAULT '',
    from_phone          TEXT    NOT NULL DEFAULT '',
    from_utr            TEXT    NOT NULL DEFAULT '',
    from_bank_name      TEXT    NOT NULL DEFAULT '',
    from_account_name   TEXT    NOT NULL DEFAULT '',
    from_sort_code      TEXT    NOT NULL DEFAULT '',
    from_account_number TEXT    NOT NULL DEFAULT '',
    notes               TEXT    NOT NULL DEFAULT '',
    paid_date           TEXT    NULL,
    pdf_path            TEXT    NULL,
    created_at          TEXT    NOT NULL,
    updated_at          TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_invoices_issue_date ON invoices(issue_date);

CREATE TABLE IF NOT EXISTS invoice_lines (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    invoice_id  INTEGER NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    position    INTEGER NOT NULL,
    description TEXT    NOT NULL DEFAULT '',
    fee_pence   INTEGER NULL
);

CREATE TABLE IF NOT EXISTS number_sequences (
    date       TEXT    PRIMARY KEY,
    last_value INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS settings (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
