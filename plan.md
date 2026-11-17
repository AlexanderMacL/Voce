# Voce — invoice generator for a freelance singer

A portable Windows 11 desktop app (WinUI 3) that keeps employers, the user's
own details and every invoice in a local SQLite database, and produces PDF
invoices in a standard layout.

## Decisions (agreed)

| Topic | Decision |
|---|---|
| Reporting period | UK tax year, 6 April – 5 April, by **invoice issue date**. Dashboard defaults to the current tax year. |
| Invoice lines | Any number of lines per invoice. Each line = editable **reason** text + **fee** (fee may be blank). |
| Default first line | User-settable template per profile, e.g. `{NAME}\t{VOICE}\t{FEE}`. See *Line template* below. |
| VAT / UTR | No VAT. Optional UTR shown on the invoice when filled in. |
| Invoice number | `yyyy-MM-dd-NNN` (issue date + per-day sequence), e.g. `2026-11-17-001`. Deleted invoices leave a gap — numbers are never reused. |
| User details | Several profiles (address + bank details). One is the default; each invoice picks one. |
| PDF file name | Pattern setting, default `Invoice-{EMPLOYER}-{NAME}`. If that file already belongs to another invoice, ` ({NUMBER})` is appended so nothing is overwritten. |
| Amending | On saving changes to an existing invoice the user is asked: **Overwrite** the invoice (same number) or **Save as new** (new number, original kept). PDF regenerated. |
| Payment | Mark paid with a paid date; mark unpaid again. |
| Deployment | Windows 11, portable self-contained folder containing `Voce.exe`. Database at `Data\voce.db` next to the exe; PDFs default to `Invoices\` next to the exe (changeable). |
| Existing data | A placeholder dataset is seeded when a new database is created. Real data will be imported separately straight into `voce.db` (schema below). |
| Export | CSV export of all invoices in a chosen tax year. |

## Architecture

```
src/Voce.Core        net8.0 class library — models, SQLite storage, numbering,
                     templates, PDF (QuestPDF), CSV, dashboard maths. Cross-platform, unit-tested.
src/Voce.App         WinUI 3 (Windows App SDK, unpackaged, self-contained) UI shell.
tests/Voce.Core.Tests  xUnit tests for Voce.Core.
.github/workflows    Windows CI: test core, publish portable app as a zip artifact.
```

Libraries: Microsoft.Data.Sqlite (plain SQL, no ORM so the schema is easy to
populate from outside), QuestPDF (Community licence), CommunityToolkit-free
code-behind UI to keep the shell small.

## Database schema (`Data/voce.db`)

Money is stored as integer **pence**; dates as `TEXT` `yyyy-MM-dd`.

```sql
profiles(id, label, singer_name, voice_part, address, email, phone, utr,
         bank_name, account_name, sort_code, account_number,
         payment_terms_days, first_line_template, is_default)
employers(id, name, contact_name, address, email, notes, is_archived)
events(id, employer_id → employers, name, default_fee_pence NULL, is_archived)
invoices(id, number UNIQUE, issue_date, due_date, event_date NULL,
         employer_id NULL → employers, profile_id NULL → profiles,
         bill_to_name, bill_to_address,
         from_singer_name, from_voice_part, from_address, from_email, from_phone, from_utr,
         from_bank_name, from_account_name, from_sort_code, from_account_number,
         notes, paid_date NULL, pdf_path NULL, created_at, updated_at)
invoice_lines(id, invoice_id → invoices (cascade), position, description, fee_pence NULL)
number_sequences(date PRIMARY KEY, last_value)     -- guarantees gaps are never refilled
settings(key PRIMARY KEY, value)                   -- pdf_folder, file_name_pattern
```

Invoices store a **snapshot** of the bill-to and the user's details, so later
edits to an employer or profile never silently change an issued invoice.
Amending an invoice re-takes the snapshot from the currently selected records.

The full DDL lives in `src/Voce.Core/Data/Schema.sql`.

## Line template

Tokens: `{NAME}` singer name, `{VOICE}` voice part, `{EMPLOYER}`, `{EVENT}`
event name, `{DATE}` event date (`d MMMM yyyy`), `{FEE}` event default fee.

`{FEE}` is not printed into the reason text — it means *"put the event's
default fee in this line's fee column"*. Tab characters split the reason into
columns on the PDF, so `{NAME}\t{VOICE}\t{FEE}` for Evensong at Church A gives
a line reading `Jane Smith | Soprano | £86.00`.

## Screens

1. **Dashboard** — tax-year picker (or custom range); invoice count, total
   invoiced, paid, outstanding, overdue; list of unpaid invoices; *Export CSV*.
2. **Invoices** — searchable list; New, Edit, Open PDF, Regenerate PDF,
   Mark paid / unpaid, Delete (confirm; optionally delete the PDF file).
3. **Invoice editor** — profile, employer, event, dates, bill-to (editable),
   lines (reason + fee), notes, live total; Save & generate PDF.
4. **Employers** — employers and their events/default fees; archive or delete.
5. **My details** — profiles; set default; line template.
6. **Settings** — PDF folder, file-name pattern, database location.

## Phases

1. ✅ Repo structure, schema, core services, unit tests, Windows CI.
2. ✅ Employers and My details screens (code written; first Windows run pending).
3. ✅ Invoice editor and PDF generation.
4. ✅ Invoice list actions: amend (overwrite or new number), regenerate, mark paid/unpaid, delete.
5. ✅ Dashboard and CSV export.
6. ☐ After first use on Windows: fix UI issues, import real data into `voce.db`, add validation and keyboard shortcuts.

## Notes

- The editor shows tab separators as ` | ` because a text box can't take a Tab key press. A literal `|` therefore can't appear in a line's reason.
- "Portable exe" means a self-contained folder: `Voce.exe` plus the runtime files beside it. Copying the folder moves the app and its data together.
