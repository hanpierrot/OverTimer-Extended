# Billiards balance report (Phase 9)

Generated 2026-10-16 08:50 from `GameConfig.asset` and 9 tables. **Nothing in `GameConfig.asset` was changed by this report.**

## How to read the numbers

- **Win rate** comes from a bot that follows each table's recorded solution and re-plans from the real state after a miss (it sees the best shot; its hands are imperfect). It is an UPPER bound for real players, who also have to find the shot. The real numbers come from the in-game round log (cheat menu: `Billiards: round log`, or `Tools/OverTimer/Billiards/Round Log Summary`).
- **Aim error** is one standard deviation of the aim, in degrees (40 trials per table and error level; power error 0.03). Aiming is limited by the mouse: a 90-unit pull moves the aim 0.6 degrees per canvas unit at 960 wide and 0.3 at the 1920 build, and over a long shot an aim error is multiplied by about 1 / (2 x ball radius) at the contact, so 0.6 degrees at 1 unit of range already moves the object ball 5 degrees. **0.15 = a patient player at the 1920 build, 0.3 = a typical player, 0.6 = careless**.
- **Seconds per shot** = the simulated roll time + an ASSUMED 3.0 s aim time (a guess: measure it, then set `billiardsSecondsPerShot`).
- **$/s** is the expected NET money per second of play, losses included, before the 20% ITRS bill (Billiards is taxed on profit only; Mining on everything). Money is clock: $1 = 1 s, and the clock runs down 1 s per second, so anything under 1 $/s loses ground.

## 1. Win rate by difficulty: the bot (all tables averaged)

Mean simulated roll per shot: **1.2 s**, so a shot cycle is about **4.2 s** at the bot's assumed 3.0 s aim time (config says `billiardsSecondsPerShot` = 7.0: a person thinks slower than the bot).

| aim error | difficulty | shots granted | assumed win (config) | bot win | mean shots played | bot seconds/round |
|---|---|---|---|---|---|---|
| 0.15° | Easy | 6.4 | 85% | 98% | 2.7 | 11 |
| 0.15° | Medium | 4.4 | 65% | 97% | 2.7 | 11 |
| 0.15° | Hard | 2.4 | 50% | 79% | 2.4 | 10 |
| 0.30° | Easy | 6.4 | 85% | 96% | 2.9 | 12 |
| 0.30° | Medium | 4.4 | 65% | 93% | 2.9 | 12 |
| 0.30° | Hard | 2.4 | 50% | 63% | 2.4 | 10 |
| 0.60° | Easy | 6.4 | 85% | 95% | 3.5 | 15 |
| 0.60° | Medium | 4.4 | 65% | 84% | 3.3 | 14 |
| 0.60° | Hard | 2.4 | 50% | 38% | 2.4 | 10 |

## 2. Per table at a typical hand (0.3 degrees)

| table | par | shots E/M/H | bot win E/M/H | roll s/shot | payout at $10 E/M/H |
|---|---|---|---|---|---|
| bank_shot_01 | 1 | 5/3/1 | 100% / 100% / 93% | 1.8 | $21 / $32 / $34 |
| blocker_01 | 2 | 6/4/2 | 98% / 98% / 10% | 1.6 | $24 / $40 / $48 |
| called_pocket_01 | 2 | 6/4/2 | 100% / 100% / 98% | 0.4 | $24 / $40 / $48 |
| combo_01 | 2 | 6/4/2 | 100% / 100% / 80% | 1.1 | $24 / $40 / $48 |
| cut_shot_01 | 2 | 6/4/2 | 100% / 100% / 95% | 0.9 | $24 / $40 / $48 |
| eight_trap_01 | 2 | 6/4/2 | 98% / 85% / 43% | 1.5 | $24 / $40 / $48 |
| four_in_order_01 | 4 | 8/6/4 | 95% / 85% / 38% | 1.3 | $30 / $56 / $76 |
| long_five_01 | 5 | 9/7/5 | 78% / 73% / 30% | 1.4 | $33 / $64 / $90 |
| straight_pot_01 | 2 | 6/4/2 | 100% / 100% / 83% | 1.0 | $24 / $40 / $48 |

## 3. The benchmarks: Mining and Blackjack (same GameConfig)

| activity | gross $/s | after the 20% ITRS bill | notes |
|---|---|---|---|
| Mining, perfect swing rate | 3.17 | 2.53 | mean vein pays $58.6 for 25.0 hits; fastest swing 0.7 s; respawn 1.0 s. Needs no skill, only clicking |
| Mining, relaxed (1 swing/s) | 2.25 | 1.80 | |
| Blackjack, basic strategy | -0.03 | n/a | -0.17 clock-seconds per hand over 5.8 s (43% of hands won); ante 20 s, win 40, push 20, natural 50. It does not earn (the Blackjack rebalance is still pending), so Mining is the benchmark |

## 4. What the CURRENT config pays: expected net $/s at a $40 bet

The bot is an upper bound, so players are modelled as a fraction of its win rate (skill) who use proportionally more shots, at a given thinking pace (seconds per shot). Compare with Mining: relaxed 2.25, perfect 3.17 $/s gross.

| difficulty | skill | 4.2 s/shot | 7.0 s/shot | 10.0 s/shot |
|---|---|---|---|---|
| Easy (target 1.5) | 100% | +5.51 | +3.32 | +2.33 |
| Easy (target 1.5) | 80% | +2.67 | +1.61 | +1.13 |
| Easy (target 1.5) | 60% | +1.03 | +0.62 | +0.43 |
| Medium (target 3.0) | 100% | +11.35 | +6.84 | +4.79 |
| Medium (target 3.0) | 80% | +7.08 | +4.27 | +2.99 |
| Medium (target 3.0) | 60% | +3.93 | +2.37 | +1.66 |
| Hard (target 4.0) | 100% | +10.27 | +6.19 | +4.33 |
| Hard (target 4.0) | 80% | +7.27 | +4.38 | +3.07 |
| Hard (target 4.0) | 60% | +4.28 | +2.58 | +1.81 |

## 5. Affordability (9.4)

- Start: $40, clock 120 s, win at 600 s. Minimum bet $10 = 25% of the starting money, so the first bet is affordable from second one and the player can lose 3 bets in a row and still play.
- The laptop is locked until the wire puzzle ($20) is solved, so the earliest possible first round is after that puzzle, whatever the bet.
- A round costs the clock about 13-35 s (3 shots at the bot's pace up to 5 shots at 7 s) of the 120 s start clock. The clock runs down 1 s per second whatever you do, so the real question is the $/s above, not the entry price: the money only helps if it comes back faster than 1 $/s.

## 6. Recommended values (for sign-off; none applied)

| field | now | recommended | why |
|---|---|---|---|
| `billiardsSecondsPerShot` | 7.0 | **keep 7.0 until measured**, then the logged `shotSeconds` | the bot needs 4.2 s a shot (roll 1.2 s + 3.0 s aim); a person who looks for the shot needs more. 7 is a plausible human pace, not a measurement |
| `billiardsAssumedWinRate` | 0.85, 0.65, 0.50 | **0.75, 0.75, 0.50** | 80% of the bot's win rate at a typical hand (96% / 93% / 63%). Compare with the current values: a small change at most. Replace with the logged win rates after the playtest |
| `billiardsTargetRate` | 1.5, 3.0, 4.0 | **0.6, 1.8, 2.3** | solved so that a typical player (80% skill, 7 s per shot) earns **0.9 / 2.0 / 2.7 $/s** net: Easy 40% of relaxed Mining, Medium 90% of it, Hard halfway between relaxed and perfect Mining ("attractive, not dominant"). Compare what the current numbers pay that same typical player (section 4) |

What those recommended values pay (expected net $/s at a $40 bet):

| difficulty | skill | 4.2 s/shot | 7.0 s/shot | 10.0 s/shot |
|---|---|---|---|---|
| Easy (target 0.6) | 100% | +3.47 | +2.09 | +1.46 |
| Easy (target 0.6) | 80% | +1.45 | +0.87 | +0.61 |
| Easy (target 0.6) | 60% | +0.28 | +0.17 | +0.12 |
| Medium (target 1.8) | 100% | +6.04 | +3.64 | +2.55 |
| Medium (target 1.8) | 80% | +3.44 | +2.07 | +1.45 |
| Medium (target 1.8) | 60% | +1.51 | +0.91 | +0.64 |
| Hard (target 2.3) | 100% | +6.70 | +4.04 | +2.83 |
| Hard (target 2.3) | 80% | +4.42 | +2.66 | +1.87 |
| Hard (target 2.3) | 60% | +2.14 | +1.29 | +0.90 |

A perfect, fast player (100% skill, 4.2 s a shot) would earn 6.7 $/s on Hard with the recommended targets, above perfect Mining (3.2 $/s). That is the ceiling of the skill reward; if you want it lower, scale the Hard target down.

## 7. The table spread: players can pick the difficulty after seeing the table

The racked screen shows the table and the three payouts, and the payout depends only on the difficulty, not on the table. But the bot's win rate varies enormously between tables (section 2). Expected net $/s for a typical player (80% skill, 7 s a shot) at a $40 bet with the CURRENT config:

| table | Easy | Medium | Hard | best pick |
|---|---|---|---|---|
| bank_shot_01 | +2.1 | +6.0 | +8.7 | Hard +8.7 |
| blocker_01 | +1.3 | +3.5 | -1.8 | Medium +3.5 |
| called_pocket_01 | +1.9 | +5.1 | +7.8 | Hard +7.8 |
| combo_01 | +1.8 | +4.8 | +5.9 | Hard +5.9 |
| cut_shot_01 | +1.9 | +5.1 | +7.6 | Hard +7.6 |
| eight_trap_01 | +1.4 | +3.1 | +1.8 | Medium +3.1 |
| four_in_order_01 | +1.4 | +3.2 | +1.9 | Medium +3.2 |
| long_five_01 | +1.0 | +2.7 | +1.4 | Medium +2.7 |
| straight_pot_01 | +1.8 | +4.9 | +6.2 | Hard +6.2 |

A player who always plays Medium earns +4.3 $/s on average, always Hard +4.4; a player who **looks at the table and picks the best difficulty earns +5.4 $/s**, and never has to take the table where Hard loses money (`blocker_01`: about 10% for the bot). Looking before choosing is worth +1.0 $/s over the best fixed difficulty.

**Recommended fix (not applied; needs your call, it touches the preset format and `BilliardsPayout`):** store a measured win rate per table and difficulty in each preset (`winRates`, three numbers, written by this tool at 80% of the bot's typical-hand rate, rounded to 5%) and let `BilliardsPayout.OnWin` use it instead of the single `billiardsAssumedWinRate[difficulty]` when it is present. Then every table pays for how hard it really is, and picking the difficulty after seeing the table stops being free money. Until then, the per-difficulty config values only fit the AVERAGE table.

## Still needs a human

1. At least 20 real rounds across all three difficulties (more than one player), read from the round log; set `billiardsSecondsPerShot` to the measured mean `shotSeconds`, replace the assumed win rates with the logged ones, and re-solve the targets (this report does it).
2. Re-run this report after changing any value (`Tools/OverTimer/Billiards/Balance Report`); it overwrites this file.
3. Update the design docs (plan task 9.5) and sign off the final checkpoint.
