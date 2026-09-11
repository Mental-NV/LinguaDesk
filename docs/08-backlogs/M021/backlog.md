# M021 — Reserve one logical operation
Status: done; all selected ACs passed with deterministic policy + file-backed SQLite/HTTP evidence and zero provider dispatches (see [tasks](tasks.md#completion-record))
Milestone: [M021 — Reserve one logical operation](../../07-roadmap.md#44-shared-allowance-cost-and-recovery-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-021 | Concurrent clients share user/global character capacity: one verified account's identical submission identity is admitted once (duplicates observe, never re-dispatch), a changed payload conflicts, and admission atomically enforces both daily allowances | Must | M003 Done (file-backed SQLite, migrations, WAL/single-writer); M005 Done (`unicode-scalar-v1`, catalog limits, identity validity constants, generated-contract baseline); M007 Done (durable verified accounts, `VerifiedAccount` policy) | implemented | [spec](spec.md) |

Acceptance summary: an authenticated verified client submits one logical operation with a UUIDv7 identity; the server validates, fingerprint-matches and atomically reserves the full scalar count against the user (20,000) and global (2,000,000) UTC-day ledgers in a single transaction; concurrent identical claims produce one reservation with zero provider dispatches, changed payloads get 409, expired identities 410, exhausted capacity 429; reservations and ledger state survive restart; detailed ACs are in spec.md.
Human steps: None required.
