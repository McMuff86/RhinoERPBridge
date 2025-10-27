## Improvements backlog

### 1) Material group names (M_MATGRUPPE_ID)
- Current: Codes are mapped to human-readable names using an embedded JSON file `EmbeddedResources/material-groups.json` and `Services/MaterialGroupService`.
- Rationale: Decouple UI from DB lookups and allow quick edits without DB schema knowledge.
- Next steps (optional):
  - Switch to a SQL LEFT JOIN on the official lookup table when available (provide table/column names).
  - Add a small editor to manage mappings in the UI, persisting back to JSON.

### 2) Show all columns in ERP Search
- Current: Typed view shows a curated subset (SKU, Name, Category, Price, Stock).
- Planned: Add a "Show all columns" checkbox to display `TOP 100 *` for the mapped table with the same search filter. Columns are generated dynamically based on the result set.
- Notes:
  - Uses the same DB Settings and ODBC connection path.
  - Keeps the existing typed view as default for clarity and performance.
  - Optional enhancements: paging, sorting, column chooser, export to CSV.

### 3) Robustness / UX
- Auto-refresh repository when DB Settings are saved (no panel reopen necessary).
- Multi-line error messages already added to avoid oversized panels.
- Debounced search (e.g., 250ms) and cancellation tokens for long queries (future).

### 4) Packaging
- Keep Yak packaging metadata in sync (manifest version vs assembly version).

### 5) Security
- Password stored via DPAPI (CurrentUser). Consider enterprise secret storage if needed.
