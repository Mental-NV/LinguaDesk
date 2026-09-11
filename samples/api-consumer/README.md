# M033 — Independent API consumer sample

A small standalone consumer that proves the public API is usable without
the SPA and without React: plain `fetch` with a local-account Bearer token,
covering sign-in → translation → status recovery → rewrite → usage →
duplicate/conflict recovery → classified failures in one deterministic run.

- `consumer.mjs` — the sample. Imports nothing but `node:crypto`; request
  and response shapes follow the committed generated client at
  `frontend/src/api/generated/linguadesk-api.d.ts` (shapes are not
  redefined as a second client implementation).
- `run.sh` — starts an isolated local `Smoke` host (deterministic fake
  providers, seeded local accounts, temporary storage on a loopback
  OS-assigned port), runs the sample, then tears the host down.
- `package.json` — local metadata only; the sample has no dependencies.

## Prerequisites

- .NET SDK 10.0.302 (`dotnet --version`)
- Node v24.20.0 (`node --version`)

## Run

```sh
bash samples/api-consumer/run.sh
```

The script builds the API, migrates an isolated temporary database, seeds
one verified and one unverified local account in the `Smoke` environment
only, waits for `/health/live` and `/health/ready`, then runs the sample.
Credentials travel by environment variable; the sample redacts the Bearer
token from every line it prints and uses synthetic fixture text only.

To point the sample at an already-running compatible local host instead:

```sh
LINGUADESK_API_BASE_URL=http://127.0.0.1:PORT \
LINGUADESK_API_EMAIL=<verified-email> \
LINGUADESK_API_PASSWORD=<password> \
node samples/api-consumer/consumer.mjs
```

Never point the sample at a shared or production deployment.

## Expected output

Ten numbered steps ending with:

```text
CONSUMER RUN COMPLETE: translation, rewrite, recovery and classified failures demonstrated.
```

The run exits non-zero on the first unexpected status, shape, charge delta
or error category, so a clean exit is the demonstration. Usage assertions
are deltas over a pre-run baseline read, so the seeded M028 usage baseline
on the verified account does not affect the result.
