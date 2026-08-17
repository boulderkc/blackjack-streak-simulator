# Architecture & Tooling Decisions

Rationale behind the technology and tooling choices for this project — captured while the reasoning is fresh, for anyone (including future me) wondering "why this and not the obvious alternative."

## Why Blazor, and Interactive Server to start

Blazor Web App template, Interactive Server render mode initially — chosen over WebAssembly for an easier learning curve while getting back up to speed. WebAssembly remains an option later if there's a concrete reason to switch (e.g. wanting the app to run without a live server connection).

## Why Azure Functions — and why standard Functions, not Durable Functions

Chosen deliberately to learn something new rather than defaulting to containers, and to get hands-on with a first-time technology (Azure Functions).

Durable Functions were considered (suggested by another AI tool) but deliberately not used for v1. Durable Functions solve a different problem than this project has: coordinating multi-step, long-running, stateful workflows — fan-out/fan-in across parallel work, checkpointing, retries across a multi-stage process. The batch simulation workload here — thousands to tens of thousands of blackjack hands, pure in-memory computation, no I/O per hand — is almost certainly a sub-second-to-low-seconds operation, not a long-running process. A standard HTTP-triggered Function that runs the simulation synchronously and writes the result to SQL is sufficient, and avoids stacking orchestrator/activity concepts, replay-determinism rules, and an extra storage-backed state machine on top of learning plain Azure Functions for the first time — consistent with the same "limit new-skill surface area per feature" reasoning applied to the SQL vs. Cosmos decision below.

Durable Functions remain a legitimate **v2 stretch goal** if there's ever a deliberate reason to showcase fan-out/fan-in orchestration (e.g., splitting a very large batch into parallel chunks) — but that would be an intentional addition once standard Functions are comfortable, not a default reached for up front.

Running on the Consumption plan, which includes a free monthly grant of executions/compute-time — expected to keep the Functions piece itself at or near $0 for a low-traffic personal demo.

## Why the engine isn't behind a swappable "local vs. remote" interface

Earlier planning considered giving the Blazor UI an abstraction (e.g. `ISimulationRunner`) that could be swapped between an in-process implementation and one that calls the Azure Function, so the UI code wouldn't need to change later. On reflection, this doesn't hold up: local execution returns a result immediately, while a batch job is inherently a different interaction shape (submit → check status/result) once real async orchestration is involved. Forcing both behind one interface either lies about the Function version being synchronous, or forces the local version to pretend it's async for no reason.

The actual resolution: the game engine is a **plain shared class library**, referenced directly by both the Blazor app (for step-through play, and any small/fast runs done locally) and the Azure Functions app (for larger bulk batches). Nobody swaps an implementation — there's one engine, used by two hosts. No interface abstraction is needed to make that work.

## Where session/game state lives

Three different kinds of state, three different homes — deliberately not unified into one:

- **Manual step-through, in-progress session** (current shoe, dealer/player hands, each seat's bankroll and bet, whose turn it is) — lives in a Scoped, per-circuit service in the Blazor app, wrapping an engine-provided session object. Created fresh per browser connection; calls the engine DLL directly, in-process — no reason to route per-hand decisions through a Function or persist them to SQL.
- **Bulk batch simulation, in-progress** — lives entirely inside the Azure Function's own execution for the duration of that one invocation. No per-hand UI, nothing intermediate persisted; the granular state is discarded once the run finishes.
- **Finished results** — one row per completed run, in Azure SQL, per the existing "not per-hand data in v1" decision below.

**Accepted v1 tradeoff:** because manual-play state is only Scoped/per-circuit, a browser refresh loses an in-progress session (new circuit = fresh instance, same behavior demonstrated with the practice bankroll-tracker service). This is intentional, not an oversight, for a solo-demo project. If refresh-survival ever becomes worth adding, the lightweight option is browser-side persistence (`localStorage` via JS interop, serialize/rehydrate the session, no login required) rather than standing up real user accounts + server-side intermediate storage just to solve this — that heavier version was considered and explicitly deferred as disproportionate to the benefit.

## Why Azure SQL, not Cosmos DB

Would have liked to use Cosmos DB to pick up NoSQL experience, but deliberately limited new-skill surface area for this project — Blazor, Azure Functions, and Azure OpenAI already represent three new things to learn at once. SQL is familiar territory, so it's a low-effort Azure-hosted addition rather than another learning curve. Persisting one row per simulation run (parameters, result, summary stats: max drawdown, longest streak, hands played) — not every individual hand, at least not in v1.

## Why Azure OpenAI

A lightweight AI feature: a plain-English summary/commentary on simulation results, rather than per-hand AI calls, to keep cost and complexity down.

## Why Azure DevOps + Azure App Configuration

Azure DevOps for the CI/CD pipeline. Azure App Configuration for runtime config (deck count, bankroll targets, etc.) rather than relying solely on pipeline variables.

## Why VS Code + C# Dev Kit, not Visual Studio or Rider

This project (Blazor + class library + Azure Functions) is a strong fit for VS Code's current tooling, with no WinForms/WebForms involved this time — also a chance to revisit VS Code after 5+ years away.

## Why Claude Code, not GitHub Copilot

Chosen to get comfortable with what's considered the leading agentic coding tool. The Blazor components and core game logic are being hand-written deliberately, to actually (re)learn Blazor — Claude Code is used for the parts not worth hand-learning: DevOps YAML, Azure Functions plumbing, code review/pairing. Claude.ai (chat) is used separately for higher-level design/architecture discussions.

## Why local dev + cloud deploy, not a cloud-hosted dev environment

Local development on a Windows 11 machine; Azure is used only for deployment/CI-CD, not as the coding environment — standard local-dev/cloud-deploy pattern, avoids Codespaces-style hourly cost/limits.

## On cost

This doesn't need to be free — it needs to be reasonable for a personal demo touched only by its owner and a handful of potential employers. Expectation, not yet verified:

- **Azure Functions (Consumption plan)** — likely $0, covered by the free monthly execution/compute grant at this traffic level.
- **Azure SQL, Azure OpenAI, App Service** — each has real (if modest) cost at low volume; not chasing free tiers here at the expense of a smoother build.
- **Budget/spending alerts** are planned, but deliberately deferred until something is actually deployed — not needed during local engine/UI development.

## Why player decisions are fixed basic strategy, not configurable or human-controlled

Considered letting the tracked seat play manually (hit/stand/double/split buttons) in step-through mode. Rejected: the project's actual question is how bet-sizing reshapes variance, not play skill — giving the human control over play decisions would confound that. Every seat, including the tracked one, always plays fixed basic strategy; the only lever exposed anywhere in the app is betting mode/config, never in-hand choices.

## Why splits and double-down are in scope for v1

Chosen over deferring to v2 for realism, accepting the added surface area (a seat can hold multiple hands per round after a split) up front rather than retrofitting it later.

## Why streak (Paroli) outcome is net profit per round, not per-hand

Splitting breaks the assumption that a round has one win/loss outcome — a split can produce hands with mixed results. Considered requiring all hands from a split to win for the streak to continue (stricter/more "pure" Paroli) and considered banning splits for the streak seat entirely (sidesteps the question). Settled on: sum profit/loss across every hand in the round; net positive continues the streak, net negative resets it, net zero (push) leaves it unchanged. Keeps the rule at the round level, matching how a human bettor would judge "did that bet pay off."

## Why manual step-through shows one full round per click, not one action at a time

Considered having the engine record a breadcrumb trail of the tracked seat's individual actions (hit/stand/split/double) and revealing them one click at a time, purely as a UI-level slideshow over an already-fully-computed round (no engine change required either way). Rejected as unnecessary: a round's final state (all hands, dealer's hand, outcome) is legible at a glance without narrating how it got there, and it keeps `RoundResult` and the step-through UI simpler.

## Build sequence / phases

**Phase 1 — local, working app**

1. Create GitHub repo
2. Install VS Code + C# Dev Kit + .NET SDK
3. Small standalone practice Blazor project to refresh skills
4. Install Claude Code CLI + VS Code extension, log in
5. Start CLAUDE.md early, capturing domain rules and conventions
6. Build core game engine (class library) by hand
7. Build Blazor UI shell by hand

**Phase 2 — cloud / infrastructure**

8. Layer in Azure Functions for batch simulation
9. Add Azure SQL persistence for run history
10. Add Azure OpenAI summary feature
11. Set up Azure DevOps pipeline + App Configuration
12. Deploy to Azure App Service, link on resume/LinkedIn

Rationale: the engine and Blazor UI are the parts most worth actually learning — build those first while focus is highest. Functions, SQL persistence, AI, and CI/CD are more mechanical/infrastructure-oriented, and natural candidates for delegating to Claude Code once the core app already works end-to-end locally.
