# Basic Strategy Reference

Every seat in a simulation — including the tracked player — always plays this exact strategy. No human ever makes a hit/stand/double/split decision anywhere in the app (see `CLAUDE.md`). This document is a human-readable transcription of `BlackjackStreakSimulator.Engine/BasicStrategy.cs`, kept in sync by hand — if the code changes, this file (and the web app's copy) need updating too, neither is generated from it.

A color-coded, easier-to-scan version of these same tables lives in the Web app at [`BlackjackStreakSimulator.Web/wwwroot/basic-strategy.html`](../BlackjackStreakSimulator.Web/wwwroot/basic-strategy.html) (also linked from the app's home page) — open it directly in a browser (GitHub won't render `.html` files as a page in its file browser, only as source, so this markdown version is the one that reads cleanly there).

**House rules assumed** (see `CLAUDE.md` for the full list): dealer hits soft 17 · double down allowed on any two cards · double after split (DAS) allowed · resplit up to 4 hands total, except Aces (never resplit, one card each, no further hitting).

**Legend:** H = Hit · S = Stand · D = Double (if not available, see the note under Soft Totals) · Sp = Split

Dealer's up card runs across the top of every table: **2 3 4 5 6 7 8 9 10 A**

## Hard totals (no Ace, or an Ace already forced to count as 1)

| Player total | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | A |
|---|---|---|---|---|---|---|---|---|---|---|
| 8 or less | H | H | H | H | H | H | H | H | H | H |
| 9 | H | D | D | D | D | H | H | H | H | H |
| 10 | D | D | D | D | D | D | D | D | H | H |
| 11 | D | D | D | D | D | D | D | D | D | D |
| 12 | H | H | S | S | S | H | H | H | H | H |
| 13 | S | S | S | S | S | H | H | H | H | H |
| 14 | S | S | S | S | S | H | H | H | H | H |
| 15 | S | S | S | S | S | H | H | H | H | H |
| 16 | S | S | S | S | S | H | H | H | H | H |
| 17 or more | S | S | S | S | S | S | S | S | S | S |

Note: 11 doubles against everything, including a dealer Ace — this is specific to dealer-hits-soft-17 games; in dealer-stands-soft-17 games it's usually just a Hit.

## Soft totals (an Ace currently counted as 11)

| Player total | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | A |
|---|---|---|---|---|---|---|---|---|---|---|
| 13 (A,2) | H | H | H | D | D | H | H | H | H | H |
| 14 (A,3) | H | H | H | D | D | H | H | H | H | H |
| 15 (A,4) | H | H | D | D | D | H | H | H | H | H |
| 16 (A,5) | H | H | D | D | D | H | H | H | H | H |
| 17 (A,6) | H | D | D | D | D | H | H | H | H | H |
| 18 (A,7) | D | D | D | D | D | S | S | H | H | H |
| 19 (A,8) | S | S | S | S | D | S | S | S | S | S |
| 20 or more | S | S | S | S | S | S | S | S | S | S |

Note on the "D" cells above: if doubling isn't available (the hand already hit once), soft 13–17 fall back to **Hit**, but soft 18 and 19 fall back to **Stand** instead — those totals are already strong enough on their own. Easy to get backwards; it's the one genuinely non-obvious wrinkle in this chart.

## Pairs (same rank only — see `CLAUDE.md` on why King+Jack doesn't qualify even though both are worth 10)

| Pair | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | A |
|---|---|---|---|---|---|---|---|---|---|---|
| A,A | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp |
| 10-value (10/J/Q/K) | S | S | S | S | S | S | S | S | S | S |
| 9,9 | Sp | Sp | Sp | Sp | Sp | S | Sp | Sp | S | S |
| 8,8 | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp | Sp |
| 7,7 | Sp | Sp | Sp | Sp | Sp | Sp | H | H | H | H |
| 6,6 | Sp | Sp | Sp | Sp | Sp | H | H | H | H | H |
| 5,5 | D | D | D | D | D | D | D | D | H | H |
| 4,4 | H | H | H | Sp | Sp | H | H | H | H | H |
| 3,3 | Sp | Sp | Sp | Sp | Sp | Sp | H | H | H | H |
| 2,2 | Sp | Sp | Sp | Sp | Sp | Sp | H | H | H | H |

Notes:
- 10-value pairs never split, regardless of rank — 20 is already a stand.
- 5,5 never splits either — it's played as a hard 10 (see the hard totals table) instead.
- 6,6 splits only against 2–6 — a stricter cutoff than 2,2 / 3,3 / 7,7, which split against 2–7. Easy to conflate since they look like the same shape of rule.
- A pair otherwise eligible to split won't be offered `Split` once a seat already has 4 hands for the round, or if this hand is itself a split Ace (which never resplits) — it falls through to the hard/soft table for its total instead.
