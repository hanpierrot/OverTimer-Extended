---
paths:
  - "Assets/Settings/**"
  - "**/Config/**"
  - "**/*.asset"
---

# GameConfig and Data Assets

## Ownership

- `Assets/Settings/Configs/GameConfig.asset` is **Ari's**. Don't change its values unasked. Propose changes as: field · old → new · why (arithmetic or model) · which throughput it moves (`PROJECT.md` §14).
- Card assets (`Assets/Resources/CardSO/*`) are **Han's** (see `unity-cards.md`).

## The asset is the truth

- Live values are in the asset, not in `Config/GameConfig.cs`. About 20 C# defaults are stale (e.g. `luckyNineBaseReward` default 0 vs live 26). When you touch a field, set its C# default to the live value.
- Adding a field: put it under the right `[Header]`, default = the intended value, then have the asset serialize it (select `GameConfig.asset` in the Inspector, or let the user do it). A field that isn't serialized yet silently uses the C# default.
- Renaming a field: `[FormerlySerializedAs("old")]`, or the live value is lost. (`ultrRareWeight` is a known typo; rename only with this attribute.)
- Arrays (e.g. `blockPayout`) are serialized as hex in YAML; read them in the Inspector, not by eye.

## Billiards physics fields

- `billiardsBallRadius`, pocket radii, speed, decel, restitutions, stop speed and max shot seconds are tied to every stored table solution. Any change → run **Verify Presets** and **Run Self-Tests**, show the results, and expect to re-record tables.
- `GameConfig.ValidateBilliards` must keep passing (corner ≥ 1.9 × ball radius, side ≥ 1.4 ×, `maxShotSpeed/120 < ballRadius`). Margins are thin (3–5%).

## Other data assets

- `BlockConfig.asset` (block order and HP), `TripleMatchSymbols.asset` / `LuckyNineSymbols.asset` (weights and multipliers) change the economy as much as `GameConfig`. Recompute EV/throughput when they change and update `PROJECT.md` §11/§13/§14.
- Record every value change in `PROJECT.md` §10 in the same commit.
