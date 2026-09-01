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
- Simulation run history, persisted and browsable, with summary stats (max drawdown, streak-length frequency — how many streaks concluded at each length, 1 through the configured max, hands played) and a plain-English AI-generated summary of each run

## Tech stack

| Layer | Choice |
|---|---|
| Core engine | C# class library (.NET), unit-tested independently of the UI — shared by both the Blazor app and the Azure Functions app |
| Front end | Blazor Web App, Interactive Server render mode |
| Batch simulation | Azure Functions, HTTP-triggered, Consumption plan (standard Functions — not Durable Functions) |
| Persistence | Azure SQL Database |
| AI summary | Azure OpenAI |
| CI/CD | Azure DevOps, Azure App Configuration |
| Hosting | Azure App Service |

See [`docs/DECISIONS.md`](docs/DECISIONS.md) for the reasoning behind each of these choices.

## Architecture

The game engine is a single shared class library — it isn't reimplemented per host. The Blazor app references it directly for step-through play; the Azure Function references it directly to run bulk batches at scale. Neither is a "fallback" for the other; they're two hosts calling the same code. The same pattern applies to persistence: a small shared project writes finished results to Azure SQL, referenced directly by both hosts rather than routed through an extra Function call.

```
Blazor UI (step-through) ──────────────► BlackjackEngine (class library)
        │
        └─ on finish ─► BlackjackStreakSimulator.Data ─► Azure SQL (writes run summary)

Blazor UI (bulk run) ──► Azure Function ──► BlackjackEngine (class library)
                                │
                                ▼
                    BlackjackStreakSimulator.Data ──► Azure SQL (writes run summary)

Blazor UI ◄─────────────────── Azure SQL (reads run history / detail)
```

## Status

🚧 Early build — currently working through the core game engine and Blazor UI locally before layering in the Azure pieces.

- [x] Repo + tooling set up
- [ ] Core game engine (class library)
- [ ] Blazor UI shell
- [ ] Azure Functions batch simulation
- [ ] Azure SQL run history
- [ ] Azure OpenAI summary feature
- [ ] CI/CD + deployment

## Running locally

*(to be filled in once the project is scaffolded)*
