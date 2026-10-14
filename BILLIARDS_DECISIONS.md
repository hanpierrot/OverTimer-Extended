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

## Phase 3 notes (2026-10-14)

- **Rules as built:** leaving while Aiming (or Racked) keeps the table; leaving while Rolling forfeits the bet and shows "You left mid-shot. That bet is lost." on the next table; Won/Lost clears the table. Verified in play mode.
- **Profit-only tax as built:** a win pays two `MoneyService.Add` calls — the stake back with reason `MoneyService.StakeRefundReason` ("stake refund"), then the profit. `ITRSService` and `RunStatsService` skip that reason. Verified: a $32 payout on a $10 bet raised the tax counter by 22, not 32. If the Billiards cost modifier makes the charge bigger than the payout, the refund is capped at the payout.
- **Charge vs payout:** charge = `round(bet x CostModifier(Billiards))`, payout always from the base bet. Verified: bet 10 at x1.5 charged 15 and still paid 76 on Hard.
- **Layout (Q1 still open):** the app is a copy of the Mining window (380x220, panel scaled 1.5x). Felt 252x126 local units (about 378x189 canvas units), ball radius 0.0714 table units = 18 local units across. The plan's default 588x294 felt does not leave room for the HUD inside the 600x375 laptop shell. Placeholder look: flat green felt, black pocket discs clipped to the felt, 8-ball as the icon.
- **Files not in the Phase 3 list:** `BallSpriteSet.cs` (+ `Assets/Design/Billiards/BallSpriteSet.asset`) is a Phase 4 file, created now because the placeholder balls need real sprites. `MoneyService.cs`, `ITRSService.cs`, `RunStatsService.cs` were touched for the profit-only tax (decided 2026-10-14).
- **Where things live:** prefab `Assets/Design/Prefab/Panels/Laptop/Billiards App.prefab`; icon added inside `Laptop.prefab` (Panel/Apps/Billiards); scene instance under `Canvas` with scene overrides for the close button's controller, `LaptopController.apps` (3 entries) and `GameManager.inputBlockers` (14 entries).
- **Testing note:** when the Unity window is in the background, play mode does not tick (the project has "run in background" off). Tests set `Application.runInBackground = true` for the play session only; it resets itself and `ProjectSettings` stays unchanged. Real mouse input was simulated through `ExecuteEvents` (pointer down/drag/up on the felt), not a physical mouse. The pointer test ran on a flipX table and hit the ball correctly; the other mirrors are covered by the FeltMapper unit tests.
- **Known placeholder gaps (later phases):** icon is the 8-ball, no cue stick/power bar/assist (Phase 5), no ball art scaling/pocket art (Phase 4), HUD is text only (Phase 6), no sound.

## UI reorganization (requested by Ari after Checkpoint 3)

- **Layout groups instead of hand-placed rows.** `Panel/Content` is a VerticalLayoutGroup: HeaderRow (table name left, ball order right), Table, Status, BottomRow. HeaderRow, the racked controls and the playing bar are HorizontalLayoutGroups. `BottomRow` has a fixed height and holds two panels, `RackedPanel` (bet -/+, three difficulty buttons) and `PlayPanel` (SHOTS LEFT, bet and win amount). They swap with SetActive without moving the table.
- **Window:** `Panel` is 640x480 canvas units at scale 1 (was 380x220 at scale 1.5, i.e. 570x330). **Felt 588x294**, which is the plan's default Q1 size: a ball is 0.0714 x 588 = 42 units, exactly the sprite's native 42 px, and at the 1920x1080 build (canvas scale 2) it is an integer 2x. That settles Q1 for now; it is easy to change (Table `LayoutElement` + the felt size are the only places).
- **Ball order as images:** `BallQueueView` (a Phase 6 file, built now at Ari's request) fills a HorizontalLayoutGroup with one ball image per queue entry, head ball larger (46 vs 34), plus a dark strip with the called pocket (mirrored) on balls that need one. The ball art comes from the existing `BallSpriteSet` asset, which already binds ball ids to sprites and is shared with the table.
- **Text sizes (canvas units):** title 30, table name and "Order:" 22, status bar 26 (was 13 local = about 20), bet 30, difficulty buttons 18, shots left 34. Status bar now also gives a hint while aiming.
- **Frame art:** the window and buttons use the same pixel-art sprites as Mining/Blackjack, which set `pixelsPerUnitMultiplier` to 2 inside 1.5x / 1.15x panels. For this 1x panel the frame uses 1.33 and buttons 1.5 so the border thickness looks the same.
- **Housekeeping found on the way:**
  - The scene instance of the app can pick up layout-driven overrides if the scene is saved while the app is active. I reset it to prefab values and re-applied the two intended overrides (close button -> laptop, inactive). Don't save the scene with the Billiards app open.
  - `Mining App.prefab` was accidentally re-saved (root active flag and two layout positions) while I used it as the base for the new prefab; restored from git, now clean.
  - The Billiards prefab root is inactive like the other two app prefabs.
- **Known polish left:** the bottom bar is empty on the win/lose panel, and "Order:" shows alone when the queue is empty after a win.

## Phase 4, scope cut by Ari: table look only (2026-10-15)

Ari: "add the table first so the game looks natural, the rest we should postpone, I need to test some features myself."

- **Done:** the table. `Panel/Content/Table` (630x336) = wooden rails (21 units) around the 588x294 felt. Rails are four `RawImage`s cut from the kit's `Billiards Table Big.png`, the felt is the kit's `Background Table` tile, the cushion shadow (9 units inside each edge) is the kit's `Border Table` tile, and `TableView` draws the six pocket holes as hard-edged block-pixel discs (3 units per art pixel, 1 grey rim pixel) sized from the SAME capture radii the simulation uses, so the visible hole and the capture area agree to the nearest art pixel. The table has a `RectMask2D` so the holes stop at the table edge.
- **Q3 (art density) settled for now:** the kit's reference table is itself 3x3-block art (rail bands are exactly 3 px wide, 21 px thick), so the table is 3 canvas units per art pixel and the balls (native 42 px sprites) 1 unit per art pixel. The kit's own pocket tiles were not used: their holes are about 8 px (24 units at 3x), far smaller than the physics needs (corner capture radius 44 units). If Han redraws the table, keep the felt rectangle (588x294) and the capture radii and nothing else has to move.
- **Layout change that came with it:** the window is 668x474 (was 640x480) and sits 12 units below centre so it clears the clock/money HUD (top-left, 240x40) and the camera buttons (right edge, 80 wide); both draw above app windows. To make room, the table name and the ball-order strip moved into the blue title bar (`Panel/TitleRow`, cream text, ball icons 34/28), and the old HeaderRow is gone.
- **Postponed, nothing wired in:** debug overlay (pockets, cushions, jaw points, ball radius rings), pot/respot tweens, snap-to-art-pixel drawing, cheat buttons for forcing a mirror and toggling the overlay, the four-mirror and alignment screenshots, and the profiler check of the nested canvas. Drafts of the overlay and the tweened `BallView` were written and then pulled back out; the design is in the plan (4.2, 4.3, 4.4, 4.5), they are quick to redo. Only whole-pixel ball size (`Mathf.Round`) was kept.
- **Not measured:** that no main-canvas rebuild happens while balls roll. It holds by construction (the table is a nested Canvas), but it was not profiled.

## Still open

- Q1 (usable app area; shell measured at 600×375 canvas units), Q3 (art density), Q6 (who owns Phase 8 card work).
- Desk Laptop (`DeskLaptopOpener`) is currently pawnable (`CanBePawned => true`, layer 3, value 300) although Ari intends it not to be for now — awaiting a yes to set `CanBePawned => false`.
