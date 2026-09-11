# Architecture & Tooling Decisions

Rationale behind the technology and tooling choices for this project — captured while the reasoning is fresh, for anyone (including future me) wondering "why this and not the obvious alternative."

## Why Blazor, and Interactive Server to start

Blazor Web App template, Interactive Server render mode initially — chosen over WebAssembly for an easier learning curve while getting back up to speed. WebAssembly remains an option later if there's a concrete reason to switch (e.g. wanting the app to run without a live server connection).

## Why Azure Functions — and why standard Functions, not Durable Functions

Chosen deliberately to learn something new rather than defaulting to containers, and to get hands-on with a first-time technology (Azure Functions).

That said, it's not purely an invented excuse to touch the technology — there's a real architectural reason a batch run belongs outside the Blazor process, not just inside it as another page calling `SimulationRunner` directly. Blazor Server keeps a live SignalR connection per user, including anyone else using step-through mode at the same time; a batch run of a large number of hands would compete with those connections for the same process's CPU/threads, which is exactly the kind of thing you offload to a separately-scaling compute tier instead. An HTTP-triggered Function is one of the standard, textbook shapes for that: submit a compute job, let it run somewhere else, get a result back — not a contrived example.

Durable Functions were considered (suggested by another AI tool) but deliberately not used for v1. Durable Functions solve a different problem than this project has: coordinating multi-step, long-running, stateful workflows — fan-out/fan-in across parallel work, checkpointing, retries across a multi-stage process. The batch simulation workload here — thousands to tens of thousands of blackjack hands, pure in-memory computation, no I/O per hand — is almost certainly a sub-second-to-low-seconds operation, not a long-running process. A standard HTTP-triggered Function that runs the simulation synchronously and writes the result to SQL is sufficient, and avoids stacking orchestrator/activity concepts, replay-determinism rules, and an extra storage-backed state machine on top of learning plain Azure Functions for the first time — consistent with the same "limit new-skill surface area per feature" reasoning applied to the SQL vs. Cosmos decision below.

Worth being honest about the ceiling this assumption rests on: HTTP-triggered Functions on the Consumption plan have a hard execution-time limit (check current Azure docs for the exact figure — it's changed over time). A request for hundreds of thousands of hands or more, depending on how fast the engine actually runs per hand, could plausibly bump into that ceiling with a naive synchronous implementation. That's not a reason to abandon the plain-Functions approach up front — it's the exact point at which the v2 stretch goal below stops being hypothetical.

Durable Functions remain a legitimate **v2 stretch goal** if there's ever a deliberate reason to showcase fan-out/fan-in orchestration (e.g., splitting a very large batch into parallel chunks) — but that would be an intentional addition once standard Functions are comfortable, not a default reached for up front. If a real batch request ever times out against the plan's execution limit, that's the trigger to actually build it, not just a hypothetical.

**Update — actually running on Flex Consumption, not classic Consumption**, and the execution-time ceiling turned out not to be the real constraint. Real numbers gathered by timing actual batches at different Instance Memory sizes (this workload is pure CPU, no I/O per hand, so memory tier and CPU allocation are coupled on Flex Consumption):

- 2048MB: ~4.8ms/run (10,000 runs in ~48s; 100,000 runs times out)
- 512MB: ~21ms/run, roughly 4x slower (1,000 runs in 18-26s; 10,000 runs times out)

Azure Functions Flex Consumption's execution timeout defaults to 30 minutes with no hard low ceiling (unlike classic Consumption's 10-minute cap) — the actual wall clicking down on a large batch is **`HttpClient.Timeout`'s plain .NET default of 100 seconds**, on the Blazor app's own `AddHttpClient("BatchFunction", ...)` registration, not an Azure platform limit. This was worth nailing down precisely, since a third-party AI tool (Copilot) initially and incorrectly claimed 100 seconds was a hard Azure cap — disproven by checking Microsoft Learn directly rather than taking that at face value.

"Always-Ready" instances (a fixed number of pre-warmed instances, configured under Scale and Concurrency) would remove per-request cold-start variance and, per the timing above, let more instance memory buy a real, consistent throughput improvement — but Flex Consumption's Always-Ready billing has **no free monthly grant** (unlike its own On-Demand executions, which do), so it's a flat ongoing cost (2048MB continuous ≈ $20.74/mo at the Always-Ready Baseline rate) for a low-traffic personal demo that mostly sits idle. Decided against it: kept the Function App at default Instance Memory with zero Always-Ready instances, and addressed the resulting variable/limited throughput as a UX problem instead of an infrastructure one — see "Why the batch size stays capped by UX messaging" below.

## Why the engine isn't behind a swappable "local vs. remote" interface

Earlier planning considered giving the Blazor UI an abstraction (e.g. `ISimulationRunner`) that could be swapped between an in-process implementation and one that calls the Azure Function, so the UI code wouldn't need to change later. On reflection, this doesn't hold up: local execution returns a result immediately, while a batch job is inherently a different interaction shape (submit → check status/result) once real async orchestration is involved. Forcing both behind one interface either lies about the Function version being synchronous, or forces the local version to pretend it's async for no reason.

The actual resolution: the game engine is a **plain shared class library**, referenced directly by both the Blazor app (for step-through play, and any small/fast runs done locally) and the Azure Functions app (for larger bulk batches). Nobody swaps an implementation — there's one engine, used by two hosts. No interface abstraction is needed to make that work.

## Where session/game state lives

Three different kinds of state, three different homes — deliberately not unified into one:

- **Step-through, in-progress session** (current shoe, dealer/player hands, each seat's bankroll and bet, whose turn it is) — lives in a Scoped, per-circuit service in the Blazor app, wrapping an engine-provided session object. Created fresh per browser connection; calls the engine DLL directly, in-process — no reason to route per-hand decisions through a Function or persist them to SQL.
- **Bulk batch simulation, in-progress** — lives entirely inside the Azure Function's own execution for the duration of that one invocation. No per-hand UI, nothing intermediate persisted; the granular state is discarded once the run finishes.
- **Finished results** — one row per completed run, in Azure SQL, per the existing "not per-hand data in v1" decision below.

**Accepted v1 tradeoff:** because step-through state is only Scoped/per-circuit, a browser refresh loses an in-progress session (new circuit = fresh instance, same behavior demonstrated with the practice bankroll-tracker service). This is intentional, not an oversight, for a solo-demo project. If refresh-survival ever becomes worth adding, the lightweight option is browser-side persistence (`localStorage` via JS interop, serialize/rehydrate the session, no login required) rather than standing up real user accounts + server-side intermediate storage just to solve this — that heavier version was considered and explicitly deferred as disproportionate to the benefit.

## Why Azure SQL, not Cosmos DB

Would have liked to use Cosmos DB to pick up NoSQL experience, but deliberately limited new-skill surface area for this project — Blazor, Azure Functions, and Azure OpenAI already represent three new things to learn at once. SQL is familiar territory, so it's a low-effort Azure-hosted addition rather than another learning curve. Persisting one row per simulation run (parameters, result, summary stats: max drawdown, streak-length frequency, hands played) — not every individual hand, at least not in v1.

## Why Azure SQL moved from serverless to a fixed Basic (5 DTU) tier

Started on Azure SQL's serverless tier, expecting its free monthly vCore-seconds grant to comfortably cover a low-traffic personal demo. That grant got exhausted within days — actual dev/demo usage, plus a background DB warm-up ping (added to reduce cold-start hits on serverless, see `MainLayout.razor`) firing on every page load, added up faster than expected. Once exhausted, serverless bills per-second for real, so continuing to develop against it risked real, unpredictable cost from ordinary local testing.

Switched to the **Basic (5 DTU) fixed tier** — small enough for this project's actual traffic, and priced as a flat predictable monthly cost (~$5/mo) rather than metered by usage. The DB warm-up ping is now largely vestigial (Basic doesn't auto-pause/cold-start the way serverless does) but left in place — harmless, and a cheap insurance policy if the tier ever changes again.

## Why the batch size stays capped by UX messaging, not bigger infrastructure

Once the Flex Consumption memory/throughput tradeoff above was understood, the next question was whether to invest further — Always-Ready instances, or migrating the Function App to a Dedicated App Service Plan — to support genuinely large batches (100,000+ runs) without timing out.

Decided against further infrastructure investment, for a product reason rather than a technical one: this app's actual target audience (employers evaluating a portfolio piece) has no reason to wait 5-10 minutes for a huge batch to finish, and a player seriously chasing high-precision statistics isn't who this project is built for. It's also not mathematically necessary — running several smaller batches (e.g. a handful of 1,000-run batches) gives the same statistical confidence as one giant batch of the same total size (standard error of a proportion scales with `1/sqrt(N)` regardless of how the N runs are split into batches), just with visible run-to-run variance across the separate batches, which has its own communicative value.

Resolution: kept the Function App at its cheap default configuration and `HttpClient.Timeout` at its unconfigured 100-second default, and turned the resulting constraint into intentional, transparent UX on the Batch Simulation page — a collapsed "why might this time out" explainer, a live-updating stopwatch visible while a batch runs (so the 100-second approach is something the user can actually watch happen), and a specific friendly error message on timeout suggesting 1,000-3,000 runs instead of a generic failure. If demand for larger batches ever becomes real (e.g. the project gets outside traffic beyond its original purpose), that's the trigger to revisit Always-Ready or a Dedicated plan — not a default reached for now.

## Why Azure OpenAI

A lightweight AI feature: a plain-English summary/commentary on simulation results, rather than per-hand AI calls, to keep cost and complexity down.

## Why Azure DevOps + Azure App Configuration

Azure DevOps for the CI/CD pipeline — built and live, running the full test suite on every push to `main` before deploying. Azure App Configuration for runtime config (deck count, bankroll targets, etc.) rather than relying solely on pipeline variables was the original plan.

**Update:** never actually built. Runtime config ended up flowing through plain per-environment `appsettings.json`/`appsettings.Development.json` plus Azure App Service application settings instead — enough for this project's actual needs (a handful of values like `BatchFunctionBaseUrl` and connection strings), so a dedicated App Configuration resource hasn't been worth adding yet. Revisit if the number of runtime-tunable values grows enough that pipeline/app-settings management gets unwieldy on its own.

## Why VS Code + C# Dev Kit, not Visual Studio or Rider

This project (Blazor + class library + Azure Functions) is a strong fit for VS Code's current tooling, with no WinForms/WebForms involved this time — also a chance to revisit VS Code after 5+ years away.

## Why Claude Code, not GitHub Copilot

Chosen to get comfortable with what's considered the leading agentic coding tool. The Blazor components and core game logic are being hand-written deliberately, to actually (re)learn Blazor — Claude Code is used for the parts not worth hand-learning: DevOps YAML, Azure Functions plumbing, code review/pairing. Claude.ai (chat) is used separately for higher-level design/architecture discussions.

## Why local dev + cloud deploy, not a cloud-hosted dev environment

Local development on a Windows 11 machine; Azure is used only for deployment/CI-CD, not as the coding environment — standard local-dev/cloud-deploy pattern, avoids Codespaces-style hourly cost/limits.

## On cost

This doesn't need to be free — it needs to be reasonable for a personal demo touched only by its owner and a handful of potential employers. Actual findings after deploying and running real usage/cost experiments (see the two decisions above for the full stories):

- **Azure Functions (Flex Consumption)** — at or near $0 at this traffic level, kept that way deliberately by staying off Always-Ready instances (which have no free grant and would run ~$5-20+/mo depending on memory size, billed continuously whether or not anyone's using the app).
- **Azure SQL** — switched from serverless (whose free grant got burned through in days) to the Basic (5 DTU) fixed tier, ~$5/mo flat.
- **Azure OpenAI, App Service** — App Service has real (if modest) cost at low volume; not chasing free tiers here at the expense of a smoother build. Azure OpenAI's cost is moot for now since the summary feature it would support remains an open decision, not yet built.
- **Budget/spending alerts** are still planned but not yet set up — an outstanding to-do, not a decision that's been made and closed.

## Why player decisions are fixed basic strategy, not configurable or human-controlled

Considered letting the tracked seat play manually (hit/stand/double/split buttons) in step-through mode. Rejected: the project's actual question is how bet-sizing reshapes variance, not play skill — giving the human control over play decisions would confound that. Every seat, including the tracked one, always plays fixed basic strategy; the only lever exposed anywhere in the app is betting mode/config, never in-hand choices.

## Why splits and double-down are in scope for v1

Chosen over deferring to v2 for realism, accepting the added surface area (a seat can hold multiple hands per round after a split) up front rather than retrofitting it later.

## Why streak (Paroli) outcome is net profit per round, not per-hand

Splitting breaks the assumption that a round has one win/loss outcome — a split can produce hands with mixed results. Considered requiring all hands from a split to win for the streak to continue (stricter/more "pure" Paroli) and considered banning splits for the streak seat entirely (sidesteps the question). Settled on: sum profit/loss across every hand in the round; net positive continues the streak, net negative resets it, net zero (push) leaves it unchanged. Keeps the rule at the round level, matching how a human bettor would judge "did that bet pay off."

## Why step-through shows one full round per click, not one action at a time

Considered having the engine record a breadcrumb trail of the tracked seat's individual actions (hit/stand/split/double) and revealing them one click at a time, purely as a UI-level slideshow over an already-fully-computed round (no engine change required either way). Rejected as unnecessary: a round's final state (all hands, dealer's hand, outcome) is legible at a glance without narrating how it got there, and it keeps `RoundResult` and the step-through UI simpler.

## Why streak-length frequency, not just longest streak

A single "longest streak reached" number tells a player almost nothing about how the strategy actually behaved over a run — a real player evaluating Paroli cares about the *distribution*: how often did a streak fizzle at 2 wins versus build all the way to the configured max? Tracking a frequency count per streak length (1 through the configured max) captures that directly, and "longest streak" falls out for free as the highest populated bucket — no need for a separate field alongside it.

## Why card/table graphics, not plain text, for step-through display

While building the engine, a throwaway text-only script (print each hand as cards + total + status) turned out to be surprisingly legible — legible enough to make plain text look like a tempting shortcut for the real UI. Going with actual visual rendering anyway, deliberately: a text-only spectator view would undersell the project as a portfolio piece, and rendering cards/table is itself a skill worth the practice in Blazor.

**Update:** originally planned as AI-generated card image assets, since generating a full deck's art is fast and low-effort. Switched to rendering cards with HTML/CSS instead — scales better across screen sizes than static raster images, and is more idiomatically web-native for a Blazor app than embedding image files. Also arguably better practice: CSS layout/composition skills transfer further than "place an image asset" would have.

## Why persistence is a shared project, not a second Azure Function

Realized the run-history page (Blazor) needs to read back the exact same `BatchSimulationResult` shape the batch Function writes to Azure SQL — so whatever defines that entity/DbContext has to be usable by both: the Function to write a row once a batch finishes, Blazor to read rows back for display. Step-through runs are never persisted at all - a step-through session is a single Run, shown live and discarded, not catalogued. Considered making persistence its own Azure Function, called over HTTP by the batch Function and by Blazor's history page — partly to give the Functions side more to actually do, since a single "run a batch and write one row" function felt thin on its own at first glance. Rejected: Blazor Server is already server-side .NET with direct SQL access, so routing either the write or the read through a second Function's HTTP endpoint would just add a network hop and a point of failure for no real benefit — a classic case of two things in the same solution calling each other over HTTP when they could just share code directly.

Resolution: a small shared project, `BlackjackStreakSimulator.Data`, referenced directly by both `Web` and the Functions app — the exact same "no interface, no swapping, just two hosts calling the same code" pattern already used for the engine, just applied to persistence instead of game logic. The batch function's actual scope (parse the request, call `SimulationRunner`, write the result, shape a response) turned out to already be a legitimate, substantial function on its own once reconsidered — it didn't actually need artificial splitting to feel like "real" Functions work. If more genuine Functions practice is wanted later, a run-history browse endpoint (HTTP GET, reads from SQL) and the Azure OpenAI summary feature are both separate, real concerns naturally suited to being their own functions — not persistence split apart for its own sake.

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
9. Add Azure SQL persistence for batch history
10. Add Azure OpenAI summary feature
11. Set up Azure DevOps pipeline + App Configuration
12. Deploy to Azure App Service, link on resume/LinkedIn

Rationale: the engine and Blazor UI are the parts most worth actually learning — build those first while focus is highest. Functions, SQL persistence, AI, and CI/CD are more mechanical/infrastructure-oriented, and natural candidates for delegating to Claude Code once the core app already works end-to-end locally.
