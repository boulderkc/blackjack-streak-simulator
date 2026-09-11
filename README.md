# Blackjack Streak Simulator

A .NET / Blazor web app that simulates a "Paroli"-style positive-progression betting strategy in blackjack across thousands of simulated hands, to see how it reshapes bankroll risk and variance compared to flat betting.

Built as a personal project to get current, hands-on experience with Blazor, Azure Functions, Azure SQL, and Azure OpenAI — built end-to-end and deployed live.

**[Live demo →](#)** *(link once deployed)*

## The idea

Paroli is a positive-progression betting system: double your bet through a win streak of N hands, and drop back to your base bet the moment you lose. This project isn't trying to prove the strategy beats the house edge — it can't, mathematically — it's testing *how it reshapes variance*: does it produce fewer bankroll bust-outs, in exchange for a capped upside, compared to just betting the same amount every hand?

## Features

- Configurable starting bankroll and win-goal target — a run "fails" at zero bankroll, "succeeds" at reaching the goal
- Standard Vegas rules, 5-player table, dealer plays from a configurable multi-deck shoe
- Two run modes: watch a bulk simulation play out round-by-round, rendered as actual card and table graphics rather than plain text (engine always plays fixed basic strategy — spectator view, not manual play), or run the whole batch at once for results only
- Configurable streak length (how many consecutive wins define a "streak")
- Toggle between streak (Paroli) betting and flat/regular betting, for direct comparison
- Batch history, persisted and browsable, with summary stats (max drawdown, streak-length frequency — how many streaks concluded at each length, 1 through the configured max, hands played)
- A plain-English AI-generated summary of each batch is an idea under consideration, not yet built

## Tech stack

| Layer | Choice |
|---|---|
| Core engine | C# class library (.NET 10), unit-tested independently of the UI (142 tests) — shared by both the Blazor app and the Azure Functions app |
| Front end | Blazor Server (Interactive Server render mode), MudBlazor components |
| Batch simulation | Azure Functions, HTTP-triggered, Flex Consumption plan (standard Functions — not Durable Functions) |
| Persistence | Azure SQL Database, Basic (5 DTU) tier |
| AI summary | Azure OpenAI — under consideration, not yet built |
| CI/CD | Azure DevOps |
| Hosting | Azure App Service |

See [`docs/DECISIONS.md`](docs/DECISIONS.md) for the reasoning behind each of these choices.

## Architecture

The game engine is a single shared class library — it isn't reimplemented per host. The Blazor app references it directly for step-through play; the Azure Function references it directly to run bulk batches at scale. Neither is a "fallback" for the other; they're two hosts calling the same code. The same pattern applies to persistence: a small shared project writes finished batch results to Azure SQL from the Function, and the Blazor app references that same project to read batch history back for display — both referenced directly rather than routed through an extra Function call for either side.

```
Blazor UI (step-through) ──────────────► BlackjackEngine (class library)
        (ephemeral - one Run, shown live, never persisted)

Blazor UI (kicks off batch) ──► Azure Function ──► BlackjackEngine (class library)
                                     │
                                     ▼
                         BlackjackStreakSimulator.Data ──► Azure SQL (writes batch summary)

Blazor UI ◄──── BlackjackStreakSimulator.Data ◄──── Azure SQL (reads batch history / detail)
```

## Status

✅ Deployed and running end-to-end on Azure.

- [x] Repo + tooling set up
- [x] Core game engine (class library, 142 unit tests)
- [x] Blazor UI shell (Simulation Configuration, Step-Through, Batch Simulation, Batch History, Home, Technology Stack pages)
- [x] Azure Functions batch simulation
- [x] Azure SQL batch history
- [ ] Azure OpenAI summary feature — under consideration, not committed to
- [x] CI/CD + deployment (Azure DevOps, builds and deploys on every push to `main`)

## Running locally

- **Web app**: `dotnet run` from `BlackjackStreakSimulator.Web` (requires `appsettings.Development.json` with a `BatchFunctionBaseUrl` pointing at a locally-running Function, and a `ConnectionStrings:BlackjackStreakSimulator` — LocalDB works out of the box).
- **Functions**: `func start` (or F5 in VS Code) from `BlackjackStreakSimulator.Functions`, with a `local.settings.json` providing the SQL connection string (not committed — see `.gitignore`).
- **Tests**: `dotnet test` from the repo root runs the full Engine test suite.
