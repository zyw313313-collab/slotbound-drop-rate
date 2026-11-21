# Slotbound 实际爆率修复 Implementation Plan

> **For agentic workers:** Use subagent-driven development for independent review or inline execution for ordered live checks. Stop before public release if any validation gate fails.

**Goal:** Make the bilingual Windows UI control actual regular acquisition weights on the verified local Slotbound build, without changing game files or patching reports.

**Architecture:** Resolve and validate the single game-owned `unitGrades.probabilities` table, then read or update only its five named numeric fields. Keep the validated origin and original values only for the lifetime of the matching process/table. Replace the old report-string and byte-signature write paths rather than fixing their masks and re-enabling them.

**Tech Stack:** Existing .NET Framework WinForms and PowerShell build; existing LuaJIT data layout for the allowlisted game build only.

**Spec:** Chinese/English UI, real outcome and in-game tooltip verification, rollback, GitHub sync after validation. Supported target initially only Steam 26791687 with the exact tested game hashes. Do not claim compatibility with other builds; do not distribute game code or DLLs.

## Global constraints

- No writes for discovery or scan-only; no invisible CLI mutation. No injection, elevation workaround, game-file patch, or update to report text.
- If process identity, table ownership, fields, types, range, or readback are not conclusive, fail closed. Same-name candidates and displaced/reallocated table are not safe restore targets.
- Only normal grade rolls are in scope; scripted outcomes, stone pools, and ranked runs are out of scope for validation. Use a disposable local test run.

## Review focus

- An extra `probabilities` object from a stone pool must never be selected.
- Invalid weights and partial write failures must not report success.
- Restore must use captured originals and not guessed stock values.
- A process exit/relaunch or table replacement invalidates old handles and addresses.
- GitHub release evidence must identify the actual EXE and tested build.

## Tasks

- [ ] Resolve the live game-owned table read-only. Check upgrade initialization, tooltip and ordinary sampler references; compare the readback with `80 / 15 / 4.5 / 0.4 / 0.1` when upgrades match. Verify uniqueness after a clean restart and reject ambiguous candidates.
- [ ] In a disposable local test, compare baseline, all-Prismatic, all-Legendary and restored values. Verify the tooltip and log eligible awarded rarities from actual winning lines, excluding forced outcomes and empty spins. Do not use report header edits or successful API writes as the outcome measure.
- [ ] Update `src/SlotboundDropRate.cs` and `build.ps1` as needed. Keep bilingual controls, validate five percentages and a 100% sum, save original values for the matching table only, report verified readback precisely, and remove unsafe legacy paths. Exercise read-only/status/restore behavior using the existing self-test entry point and then repeat the live checks with the built EXE.
- [ ] Correct `README.md` and the two `docs/` landing pages. After an independent review and live verification, push the tested commit, upload the same verified EXE as the new release, and check the public asset and Pages links. Anything unverified stays local.
