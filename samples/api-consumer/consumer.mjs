// M033 independent API consumer sample.
//
// Plain `fetch` against the real local host with a local-account Bearer token.
// No React is executed. The Bearer token is read from the environment into
// memory only and is never printed, logged, or included in output.
//
// Required environment:
//   LINGUADESK_API_BASE_URL  e.g. http://127.0.0.1:5123 (no trailing slash)
//   LINGUADESK_API_EMAIL      seeded verified local account email
//   LINGUADESK_API_PASSWORD  seeded verified local account password
//
// Every step asserts the expected HTTP status and wire shape and exits
// non-zero on the first unexpected response, so a clean run is the evidence.

import { randomFillSync } from "node:crypto";

const baseUrl = process.env.LINGUADESK_API_BASE_URL ?? "";
const email = process.env.LINGUADESK_API_EMAIL ?? "";
const password = process.env.LINGUADESK_API_PASSWORD ?? "";

if (!baseUrl || !email || !password) {
  console.error(
    "Missing LINGUADESK_API_BASE_URL, LINGUADESK_API_EMAIL or LINGUADESK_API_PASSWORD."
  );
  process.exit(2);
}

// Synthetic fixture text only. Never a secret, never user data.
const TRANSLATION_SOURCE = "Hello, the meeting starts at 14:30. Please go.";
const TRANSLATION_TARGET = "ro";
const EXPECTED_TRANSLATION = "Bun\u0103, \u00eent\u00e2lnirea \u00eencepe la 14:30. Te rog s\u0103 mergi.";
const REWRITE_SOURCE = "The quarterly report is ready for review today.";
const REWRITE_MODE = "simple";

const scalarCount = (text) => [...text].length;

// Minimal RFC 9562 UUIDv7: 48-bit unix-ms timestamp plus random bits.
function uuidv7() {
  const bytes = Buffer.alloc(16);
  const now = BigInt(Date.now());
  for (let shift = 40n; shift >= 0n; shift -= 8n) {
    bytes[Number(5n - shift / 8n)] = Number((now >> shift) & 0xffn);
  }
  randomFillSync(bytes, 6, 10);
  bytes[6] = (bytes[6] & 0x0f) | 0x70;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = bytes.toString("hex");
  return (
    `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-` +
    `${hex.slice(16, 20)}-${hex.slice(20)}`
  );
}

function fail(step, message, body) {
  console.error(`[FAIL] ${step}: ${message}`);
  if (body !== undefined) {
    console.error(JSON.stringify(body).slice(0, 2000));
  }
  process.exit(1);
}

function expect(step, actual, expected, body) {
  if (actual !== expected) {
    fail(step, `expected ${JSON.stringify(expected)} but saw ${JSON.stringify(actual)}`, body);
  }
}

async function call(step, method, path, { token = null, body = undefined } = {}) {
  const headers = { Accept: "application/json" };
  if (token !== null) {
    headers.Authorization = "Bearer [REDACTED]";
  }
  const init = { method, headers };
  if (body !== undefined) {
    init.headers = { ...headers, "Content-Type": "application/json" };
    init.body = JSON.stringify(body);
  }
  if (token !== null) {
    // Real credential goes on the wire only; logs keep the redacted placeholder.
    init.headers.Authorization = `Bearer ${token}`;
  }
  const response = await fetch(`${baseUrl}${path}`, init);
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    fail(step, `non-JSON response (status ${response.status})`, text.slice(0, 500));
  }
  return { status: response.status, json };
}

function snapshotLine(label, usage) {
  console.log(
    `  ${label}: day=${usage.day} consumed=${usage.consumedCharacters} ` +
      `reserved=${usage.reservedCharacters} available=${usage.availableCharacters} ` +
      `revision=${usage.revision} availability=${usage.availability}`
  );
}

const run = async () => {
  // 1. Sign in with the seeded verified local account (token stays in memory).
  const signIn = await call("sign-in", "POST", "/api/accounts/bearer-sign-in", {
    body: { email, password },
  });
  expect("sign-in", signIn.status, 200, signIn.json);
  expect("sign-in", signIn.json.verificationStatus, "verified", signIn.json);
  const token = signIn.json.accessToken;
  if (typeof token !== "string" || token.length < 20) {
    fail("sign-in", "missing access token");
  }
  console.log(
    `[1] sign-in ok: verificationStatus=verified expiresIn=${signIn.json.expiresIn} tokenType=${signIn.json.tokenType}`
  );

  // Baseline usage before this run submits anything.
  const baseline = await call("usage-baseline", "GET", "/api/usage", { token });
  expect("usage-baseline", baseline.status, 200, baseline.json);
  const consumedBaseline = baseline.json.consumedCharacters;
  snapshotLine("baseline usage", baseline.json);

  // 2. Translation submit (AC-001).
  const translationId = uuidv7();
  const translationBody = {
    operationId: translationId,
    family: "translation",
    source: TRANSLATION_SOURCE,
    target: TRANSLATION_TARGET,
  };
  const translation = await call("translation-submit", "POST", "/api/operations", {
    token,
    body: translationBody,
  });
  expect("translation-submit", translation.status, 201, translation.json);
  expect("translation-submit", translation.json.family, "translation", translation.json);
  expect("translation-submit", translation.json.status, "succeeded", translation.json);
  expect("translation-submit", translation.json.translatedText, EXPECTED_TRANSLATION, {
    length: translation.json.translatedText?.length,
  });
  expect("translation-submit", translation.json.characterCount, scalarCount(TRANSLATION_SOURCE));
  const chargeDay = translation.json.admissionDay;
  console.log(
    `[2] translation ok: chars=${translation.json.characterCount} chargeDay=${chargeDay}`
  );
  console.log(`  translatedText: ${translation.json.translatedText}`);
  snapshotLine("post-translation usage", translation.json.usage);

  // 3. Status re-read returns terminal metadata without result text (AC-003).
  const translationStatus = await call(
    "translation-status",
    "GET",
    `/api/operations/${translationId}`,
    { token }
  );
  expect("translation-status", translationStatus.status, 200, translationStatus.json);
  expect("translation-status", translationStatus.json.status, "succeeded", translationStatus.json);
  expect("translation-status", translationStatus.json.outputAvailable, false, translationStatus.json);
  console.log(
    `[3] translation status ok: status=succeeded outputAvailable=false chargeDay=${translationStatus.json.admissionDay}`
  );

  // 4. Rewrite submit with exactly one mode (AC-002).
  const rewriteId = uuidv7();
  const rewriteBody = {
    operationId: rewriteId,
    family: "rewriting",
    source: REWRITE_SOURCE,
    mode: REWRITE_MODE,
  };
  const rewrite = await call("rewrite-submit", "POST", "/api/operations", {
    token,
    body: rewriteBody,
  });
  expect("rewrite-submit", rewrite.status, 201, rewrite.json);
  expect("rewrite-submit", rewrite.json.family, "rewriting", rewrite.json);
  expect("rewrite-submit", rewrite.json.status, "succeeded", rewrite.json);
  expect("rewrite-submit", rewrite.json.characterCount, scalarCount(REWRITE_SOURCE));
  if (typeof rewrite.json.rewrittenText !== "string" || rewrite.json.rewrittenText.length === 0) {
    fail("rewrite-submit", "missing rewritten text", { length: rewrite.json.rewrittenText?.length });
  }
  console.log(`[4] rewrite ok: mode=${REWRITE_MODE} chars=${rewrite.json.characterCount} chargeDay=${rewrite.json.admissionDay}`);
  console.log(`  rewrittenText: ${rewrite.json.rewrittenText}`);
  snapshotLine("post-rewrite usage", rewrite.json.usage);

  // 5. Status re-read for the rewrite.
  const rewriteStatus = await call("rewrite-status", "GET", `/api/operations/${rewriteId}`, {
    token,
  });
  expect("rewrite-status", rewriteStatus.status, 200, rewriteStatus.json);
  expect("rewrite-status", rewriteStatus.json.status, "succeeded", rewriteStatus.json);
  expect("rewrite-status", rewriteStatus.json.outputAvailable, false, rewriteStatus.json);
  console.log("[5] rewrite status ok: status=succeeded outputAvailable=false");

  // 6. Settled usage snapshot covers both charges as deltas over baseline.
  const usage = await call("usage", "GET", "/api/usage", { token });
  expect("usage", usage.status, 200, usage.json);
  const expectedConsumed =
    consumedBaseline + scalarCount(TRANSLATION_SOURCE) + scalarCount(REWRITE_SOURCE);
  expect("usage", usage.json.consumedCharacters, expectedConsumed, usage.json);
  snapshotLine("settled usage", usage.json);
  console.log(
    `[6] usage ok: both charges settled (consumed delta=${usage.json.consumedCharacters - consumedBaseline})`
  );

  // 7. Duplicate same-identity/same-payload replays the original outcome
  //    with no new charge or dispatch (AC-003).
  const duplicate = await call("duplicate-submit", "POST", "/api/operations", {
    token,
    body: translationBody,
  });
  expect("duplicate-submit", duplicate.status, 200, duplicate.json);
  expect("duplicate-submit", duplicate.json.status, "succeeded", duplicate.json);
  expect("duplicate-submit", duplicate.json.outputAvailable, false, duplicate.json);
  const afterDuplicate = await call("usage-after-duplicate", "GET", "/api/usage", { token });
  expect("usage-after-duplicate", afterDuplicate.json.consumedCharacters, expectedConsumed);
  console.log(
    "[7] duplicate ok: same identity/payload replayed outcome metadata (outputAvailable=false), no new charge"
  );

  // 8. Same identity with a changed payload conflicts (AC-003).
  const conflict = await call("conflict-submit", "POST", "/api/operations", {
    token,
    body: { ...translationBody, source: `${TRANSLATION_SOURCE} Really.` },
  });
  expect("conflict-submit", conflict.status, 409, conflict.json);
  expect("conflict-submit", conflict.json.category, "identityConflict", conflict.json);
  console.log(
    `[8] conflict ok: status=409 category=identityConflict detail="${conflict.json.detail}"`
  );

  // 9. Invalid input is rejected with its category and zero charge (AC-004).
  const invalid = await call("invalid-submit", "POST", "/api/operations", {
    token,
    body: { operationId: uuidv7(), family: "translation", source: "", target: TRANSLATION_TARGET },
  });
  expect("invalid-submit", invalid.status, 422, invalid.json);
  expect("invalid-submit", invalid.json.category, "inputEligibility", invalid.json);
  const afterInvalid = await call("usage-after-invalid", "GET", "/api/usage", { token });
  expect("usage-after-invalid", afterInvalid.json.consumedCharacters, expectedConsumed);
  console.log(
    `[9] invalid-input ok: status=422 category=inputEligibility, no charge (consumed=${afterInvalid.json.consumedCharacters})`
  );

  // 10. Unauthenticated usage read is rejected distinctly from success (AC-004).
  const anonymous = await call("anonymous-usage", "GET", "/api/usage");
  expect("anonymous-usage", anonymous.status, 401, anonymous.json);
  const anonymousCategory = anonymous.json?.category ?? "(no body category)";
  console.log(`[10] unauthenticated ok: status=401 category=${anonymousCategory}`);

  console.log("CONSUMER RUN COMPLETE: translation, rewrite, recovery and classified failures demonstrated.");
};

await run();
