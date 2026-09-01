# Project Rules & Context — Blackjack Streak Simulator

Operational rules and domain context for AI-assisted work in this repo. Keep this terse and current — it's read before every task, not prose for humans.

## Terminology

- **Round** — one hand dealt and settled (`RoundEngine.PlayRound`).
- **Run** — one full playthrough of the tracked seat from `InitialBankroll` to bust or goal, made up of many Rounds (`SimulationResult`, produced by `SimulationRunner.RunSimulation` or a manual "Finish Automatically" session).
- **Batch** — many independent Runs using the same config, aggregated into `BatchSimulationResult`. Don't use "trial" or other synonyms for Run — three tiers, three names, no overlap.

## Domain rules (blackjack simulation)

- Standard Vegas rules apply unless noted otherwise.
  - Dealer hits soft 17.
  - Double down allowed on any two cards (no hard-9/10/11 restriction).
  - Double after split (DAS) is allowed.
  - Resplitting is allowed up to 4 hands total (3 resplits) per seat per round. Split Aces are the exception: never resplit, receive exactly one card each, no further hitting.
- Configurable seat count (5-player table by default).
- Configurable shoe size (e.g. 5 decks).
- Configurable starting bankroll (X), base bet, and win-goal target (Y) for the tracked seat.
  - Bankroll reaching 0 = run fails.
  - Bankroll reaching Y = run succeeds.
- Betting modes:
  - **Streak (Paroli-style) mode** — double the bet through a win streak of N hands (N configurable); any loss resets to base bet.
  - **Flat mode** — same bet every hand; used as the comparison baseline.
  - Seats other than the tracked player always use standard/flat betting, with a fixed low base bet and a fixed large bankroll — no configuration surface, and never meaningfully at risk of busting.
- Two run modes:
  - Manual step-through (one hand at a time) — nice-to-have, not required for v1.
  - Bulk simulation (thousands/tens of thousands of hands, results only, no per-hand UI).
- All seats, including the tracked player, always play fixed basic strategy (hit/stand/double/split) — no human-controlled play decisions anywhere in the app, including step-through mode.
- Splits and double-down are in scope for v1; a Seat can hold multiple Hands for a round.
- Splitting requires matching **rank**, not just matching value — e.g. two Jacks can split; a King and a Jack cannot, despite both being worth 10.
- Streak (Paroli) win/reset/push is determined by **net profit across all of a seat's hands in the round** — covers splits; net positive continues the streak, net negative resets it, net zero (push) leaves it unchanged.
- Manual step-through mode is spectator-only: each click plays and reveals one full round (all seats' final hands, dealer's hand, outcome) via the same engine call bulk mode uses — not a per-action reveal, not human-controlled play.
- Reported results track streak-length *frequency* (a count of how many streaks concluded at each length, 1 through the configured max) rather than a single "longest streak" number — the highest populated bucket already gives you longest streak for free.
- Reaching the configured max streak length (e.g. 4) triggers an automatic reset to base bet — a "full" streak completing is a deliberate reset (classic Paroli "cash out"), the same as a loss-triggered reset, just not caused by a loss. A streak can never exceed the configured max in length.

## Coding conventions

*(to be filled in as decided — naming, project/folder structure, test conventions, etc.)*

## Architecture notes

- Core game engine lives in its own class library project, independent of the Blazor project — must stay unit-testable in isolation.
- That engine library is referenced **directly** by both the Blazor app and the Azure Functions app. There is no interface/abstraction swapping between "local" and "remote" execution — the Function is just another host running the same engine code, not a separate implementation of it. Don't introduce a swappable-execution interface for this; it isn't needed.
- Result persistence (writing a finished `SimulationResult` to Azure SQL) lives in its own shared project, `BlackjackStreakSimulator.Data`, referenced directly by both the Blazor app and the Azure Functions app — same reasoning as the engine, no swappable interface, just two hosts calling the same code. Manual mode calls it directly when a session finishes (whether by clicking through to the end or via "Finish Automatically"); batch mode calls it after `SimulationRunner.RunSimulation` returns. Deliberately **not** a second Azure Function reached over HTTP from Blazor — that would be an unnecessary network hop for a simple database write, when Blazor is already server-side .NET with direct SQL access. This also means manual and batch runs end up cataloged in the same run history uniformly, from one shared write path.
- Batch simulation runs on a standard (non-Durable) Azure Function, HTTP-triggered, deliberately kept out of the Blazor process — a large batch run shouldn't compete for CPU/threads with other users' live Blazor Server connections, which is the real (not just learning-motivated) reason this is a separate compute tier rather than another page calling `SimulationRunner` in-process. Durable Functions were deliberately not used for v1 — the workload (thousands of in-memory hands, no external I/O per hand) is fast enough to run synchronously within a single function invocation, and skipping Durable Functions avoids stacking orchestrator/activity concepts and replay-determinism rules on top of learning plain Azure Functions for the first time. See `docs/DECISIONS.md` for the full reasoning; if a future need for fan-out/fan-in or long-running orchestration actually arises, that's a deliberate v2 decision, not a default.
- Manual step-through session state (shoe, hands, each seat's bankroll/bet, whose turn it is) lives in a Scoped, per-circuit service in the Blazor app — not Azure SQL, not Functions. It's ephemeral by design: a browser refresh tears down the circuit and loses the in-progress session. That's an accepted v1 tradeoff, not an oversight. A `localStorage`-based refresh-survival option (via JS interop) was discussed as a possible future nice-to-have — explicitly out of scope for v1.
- Engine internal shape: `Card`, `Shoe`, `Hand` (one hand's cards + its own bet; a `Seat` holds a list of these, >1 after a split), `Seat` (persists bankroll/streak across the session).
- Dealer play is a fixed method, not an interface — same reasoning as the local/remote engine decision above: don't abstract what's never actually swapped.
- `IBettingStrategy` (Streak vs. Flat) is the only pluggable piece in the engine; only the tracked seat's strategy is ever swapped, everyone else always flat-bets.
- `RoundEngine.PlayRound(dealerHand, shoe, seats)` is the single orchestration entry point — plays one full round (deal, every seat's basic-strategy turn including splits, dealer's turn, settlement) and returns `void`. `Hand`/`Seat` are reference types the caller already holds, so mutating them in place is all that's needed — no result object to build or return. Both hosts call this same method; the only difference between manual step-through and bulk simulation is how many times, and how fast, it's called.
- `SimulationConfig`/`SimulationResult` are shared shapes reused across manual session, bulk run, and later the Function request/response — not duplicated per host.
- Step-through and results display use actual visual card/table rendering, not plain text — this was left unspecified early on. Cards are drawn with **HTML/CSS, not image assets** (not even AI-generated ones, which was the original plan) — chosen for better scaling across screen sizes and because it's more idiomatically web-native than embedding static card images. See `docs/DECISIONS.md`.

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
