# Project Rules & Context — Blackjack Streak Simulator

Operational rules and domain context for AI-assisted work in this repo. Keep this terse and current — it's read before every task, not prose for humans.

## Domain rules (blackjack simulation)

- Standard Vegas rules apply unless noted otherwise.
  - Dealer hits soft 17.
  - *(add further table rules here as they're settled — e.g. blackjack payout, split/double-down limits)*
- 5-player table.
- Configurable shoe size (e.g. 5 decks).
- Configurable starting bankroll (X) and win-goal target (Y).
  - Bankroll reaching 0 = run fails.
  - Bankroll reaching Y = run succeeds.
- Betting modes:
  - **Streak (Paroli-style) mode** — double the bet through a win streak of N hands (N configurable); any loss resets to base bet.
  - **Flat mode** — same bet every hand; used as the comparison baseline.
  - Seats other than the tracked player always use standard/flat betting.
- Two run modes:
  - Manual step-through (one hand at a time) — nice-to-have, not required for v1.
  - Bulk simulation (thousands/tens of thousands of hands, results only, no per-hand UI).

## Coding conventions

*(to be filled in as decided — naming, project/folder structure, test conventions, etc.)*

## Architecture notes

- Core game engine lives in its own class library project, independent of the Blazor project — must stay unit-testable in isolation.
- That engine library is referenced **directly** by both the Blazor app and the Azure Functions app. There is no interface/abstraction swapping between "local" and "remote" execution — the Function is just another host running the same engine code, not a separate implementation of it. Don't introduce a swappable-execution interface for this; it isn't needed.
- Batch simulation runs on a standard (non-Durable) Azure Function, HTTP-triggered. Durable Functions were deliberately not used for v1 — the workload (thousands of in-memory hands, no external I/O per hand) is fast enough to run synchronously within a single function invocation, and skipping Durable Functions avoids stacking orchestrator/activity concepts and replay-determinism rules on top of learning plain Azure Functions for the first time. See `docs/DECISIONS.md` for the full reasoning; if a future need for fan-out/fan-in or long-running orchestration actually arises, that's a deliberate v2 decision, not a default.
- Manual step-through session state (shoe, hands, each seat's bankroll/bet, whose turn it is) lives in a Scoped, per-circuit service in the Blazor app — not Azure SQL, not Functions. It's ephemeral by design: a browser refresh tears down the circuit and loses the in-progress session. That's an accepted v1 tradeoff, not an oversight. A `localStorage`-based refresh-survival option (via JS interop) was discussed as a possible future nice-to-have — explicitly out of scope for v1.

## Tech stack (quick reference)

- C# / .NET (latest), Blazor Web App, Interactive Server render mode (to start).
- Azure Functions — standard HTTP-triggered function, Consumption plan (not Durable Functions) — for batch simulation runs.
- Azure SQL Database for run-history persistence — one row per run (parameters, result, summary stats); not per-hand data in v1.
- Azure OpenAI / Azure AI Foundry for a plain-English summary of simulation results.
- Azure DevOps for CI/CD; Azure App Configuration for runtime config.

See `docs/DECISIONS.md` for the reasoning behind each of these choices.

## Working agreement

- The core game engine and Blazor UI components are being hand-written by the project owner, deliberately, to (re)learn Blazor and reinforce fundamentals — prefer explaining/pointing over generating full replacements for these, unless explicitly asked to just implement something.
- Infrastructure/plumbing pieces (Azure Functions scaffolding, DevOps YAML, SQL schema/migration plumbing) are fair game to delegate more fully.
