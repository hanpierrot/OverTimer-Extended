# Billiards — decisions & deviations log

Companion to `BILLIARDS_IMPLEMENTATION.md` (Ari's plan, kept outside the repo). The plan's *spine* (phases, checkpoints, pure-C# sim, table-space angles, mirror-for-view-only, `.Instance` services) stays. This file records what was decided or changed along the way. Newest at the bottom.

## Decisions (Ari)

| Date | Decision | Affects |
|---|---|---|
| 2026-10-13 | Billiards is **less punishing** than Blackjack/Mining. Closing the app while **Aiming** (before / between shots) keeps the round and the bet. Closing **after a shot is fired** (Rolling/Judging) **forfeits** the bet — no free reset after a miss. | Phase 3 task 3.5 (replaces "closing during Aiming forfeits") |
| 2026-10-14 | **Tax counts profit only.** A win's stake is not income. | Phase 3 money; needs a small change in `ITRSService` (and `RunStatsService`) — see below |
| 2026-10-14 | Playing costs real clock time **by design**; payouts must be clearly worth it. | Phase 9 balance; payout formula |
| 2026-10-14 | Plan inconsistencies are fixed on the go, keeping the plan's spine. | this file |
| 2026-10-14 | **Payout is expected-value based**, not win-only: `payout = bet × (1 + netRate × T / betMax) / winRate`, with `T = (par + shotsGranted) / 2 × secondsPerShot`. `billiardsTargetRate` now means expected NET $/s losses included (defaults 1.5 / 3 / 4); new `billiardsAssumedWinRate` (defaults 0.85 / 0.65 / 0.5) is a guess to be replaced by measured win rates in Phase 9. | `BilliardsPayout`, `GameConfig`; replaces plan §3.3 formula and its 82/145/180 table |
| 2026-10-14 | `BilliardsCostModifier` card re-pointed to `Target.Billiards` (it was raising ticket prices). `BlackjackCostModifier` still targets Ticket — left for the Blackjack rebalance. | card asset |
| 2026-10-14 | Blackjack rebalance and the laptop becoming pawnable are **deferred** until the extra income methods/items and the phone/online shop are done. | later |

## Implementation notes for the decisions

- **Profit-only tax.** `ITRSService.HandleMoneyChanged` adds *every* positive money delta to `earnedSinceLastBill`, regardless of reason. Plan: on a win, pay out in two `MoneyService.Add` calls — the stake back with reason `"billiards stake"` (exempt) and the profit with reason `"billiards"` (taxed) — and make `ITRSService` skip the exempt reason. `RunStatsService` counts positive deltas as "money earned" too; skip the same reason there. Stake = what the player actually paid (`bet × CostModifier(Billiards)`), so an Interest-style card makes the table worse, as the plan intends.
- **Forfeit rule.** `BilliardsApp.OnAppClosed`: state Aiming → keep round exactly as is; state Rolling/Judging → drop the round, bet lost; Racked → keep the racked table; Won/Lost → clear.

## Plan fixes already applied

- Appendix B used `new GameConfig()`; it is a ScriptableObject → tests use `ScriptableObject.CreateInstance<GameConfig>()`.
- P12 (Mining/Blackjack NREs on open): fixed 2026-10-12 by re-linking the null service references in the scene.
- P13 (card weights all 0): no longer true — weights are set (e.g. `DisableBilliards` 8, `BilliardsCostModifier` 12), so Billiards cards can already drop from packs.
- `GameManager.GameOver` now sets `Time.timeScale = 0` (the plan assumed it didn't). The sim is driven by `Time.deltaTime`, so it freezes by itself; use `SetUpdate(true)` for any end-of-game tween.
- P7 (Laptop `_switching` soft-lock): fixed in `LaptopController` (task 0.6), play-tested.

## Deviations from the plan

- Phase 1: `AimAssist.cs` (A9, Phase 5) and `BilliardsPendingEffects.cs` (A8, Phase 8) were added early because the Appendix B self-tests call them. Verbatim reference code.
- Phase 1: the new keys were appended to `GameConfig.asset` by hand (Inspector unavailable at the time); verified afterwards that Unity imports the values.
- Phase 1: self-tests grew from 37 to 54 checks (payout table + expected-value check, win-rate validation, enum order, preset loading, asset checks).
- `GameConfig.asset` was re-serialized by Unity (`ForceReserializeAssets`) so every field is written, including the refactor's pawn-value fields. Only additions, no live value changed.
- Par-4 table, 7 s/shot, win payout: Easy $121 / Medium $223 / Hard $304 at a $40 bet; $30 / $56 / $76 at $10. (Replaces the plan's 82/145/180 and 20/36/45.)

## Phase 2 notes (2026-10-14)

- **Pocket radii changed (physics constants):** corner `0.09 -> 0.15`, side `0.075 -> 0.12` (ball radius 0.0714 unchanged). Reason: with the plan's placeholder values the jaw points bounce a ball back out of a corner pocket before it counts as dropped, even on a straight-in shot (`straight_test` potted at no power at all with 0.09). A sweep of approach angles showed corner >= 0.14 drops every angle 10-80 deg and side >= 0.12 drops every angle 20-160 deg. `GameConfig.ValidateBilliards()` now also rejects corner < 1.9 x ball radius and side < 1.4 x ball radius, so art-derived radii (Q1) can't silently undo this. **Any later change to the radii needs a verifier run.**
- **Ball-ball step repeats (max 4 passes per tick, stops early) and a position-only cleanup after the cushion step (max 3 passes).** Not in the plan's step list. Reason: the plan's own S5 limit (no pair overlapping by more than 1% of a radius) was missed by up to 13% in tight clusters with the plain single pass. Now 0.0-0.2%. A pair that is already separating gets no second impulse, so a simple two-ball hit is unchanged.
- **S3 expectation corrected.** The plan says the outgoing cushion angle mirrors the incoming angle, but its own sim spec reverses only the normal component and scales it by the restitution, which makes the outgoing angle steeper (tan out = 0.75 tan in). The sim spec is the physics we keep; S3 now checks vy flips and scales by the restitution while vx is unchanged. **Phase 5's reflected assist line must use the same rule, not a mirror image.**
- **S2 uses valid coordinates** (the plan's cue 0.5 / ball 1.0 would put the ball on the cushion): cue (0.25, 0.5), ball (0.5, 0.5) in preset space.
- Public additions to `BilliardsSim` beyond the A11 contract: `Velocity(int)` and `StepsThisShot` (tests and later previews need them).
- `straight_test` needed no power tuning: 0.5 drops ball 1 in TR, and so does anything from 0.3 to 1.0.
- Editor vs. plain .NET: the same 200-shot S5 sequence gave 74 drops in the Unity Editor but 85 in a .NET 8 run of the same code. Both runs pass every check; it is a real example of the Editor/WebGL determinism worry (plan P4), because the chaotic multi-ball shots amplify tiny float differences. Stored solutions must keep passing the verifier in the target runtime.

## Still open

- Q1 (usable app area; shell measured at 600×375 canvas units), Q3 (art density), Q6 (who owns Phase 8 card work).
- Desk Laptop (`DeskLaptopOpener`) is currently pawnable (`CanBePawned => true`, layer 3, value 300) although Ari intends it not to be for now — awaiting a yes to set `CanBePawned => false`.
