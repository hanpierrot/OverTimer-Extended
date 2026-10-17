---
paths:
  - "**/Billiards/**"
  - "Assets/Resources/BilliardsPresets/**"
---

# Billiards Rules

## Determinism

- `BilliardsSim`, `BilliardsReferee`, `BilliardsRound`, `BilliardsPreset*`, `FeltMapper`, `TableMirror`, `BilliardsPayout` stay free of `MonoBehaviour`, `Time`, `Physics2D` and `UnityEngine.Random`. Views and `BilliardsApp` only read sim state.
- Fixed step `1/120 s`; every shot goes through `ShotInput.Quantized` before it is simulated or drawn, so replays are exact.
- Angles are measured in **table space** (2 × 1), never in normalised preset coordinates. Mirroring is view/input only; the sim always runs the table as authored.
- Determinism is only guaranteed within one runtime (Editor vs .NET 8 already differ). Verify solutions in the Editor; a WebGL check is still owed.

## Tables

- Author and change tables only with **Tools/OverTimer/Billiards/Table Editor** (record or Auto-solve; Export refuses unless solved and verified). Never hand-edit solutions in the JSON.
- After changing any physics constant, rule flag, the sim, or a table: run **Verify Presets** (30 jittered trials per shot; knife-edge < 70%) and **Run Self-Tests** (105 checks). Report the counts.
- `straight_test` is a test table that is still in the random pool; leave it unless Ari decides.

## Money and rules

- Charge = `round(bet × CostModifier[Billiards])`; payout is computed from the base bet. On a win, return the stake with `MoneyService.StakeRefundReason` (untaxed), then the profit (taxed).
- Leaving the app while Racked/Aiming keeps the bet; only a shot in flight (Rolling) forfeits.
- Payout values (`billiardsTargetRate`, `billiardsAssumedWinRate`) are pending a retune after real rounds (target ≈ 1.9 / 3.2 / 4.1 $/s for Easy/Medium/Hard). Don't change them without Ari.

## Cards

- Billiards card effects use `BilliardsPendingEffects` (owner-keyed grants, consumed before `EndEffect`, withdrawn in `OnRemoved`, reset on scene load).

## Tools

- The Balance Report (`Tools/OverTimer/Billiards/Balance Report`) overwrites `BILLIARDS_BALANCE.md` at the repo root. After running it, copy the summary into `PROJECT.md` §13.4.
- Update `PROJECT.md` §13.4 for any rule, physics, payout or table change.
