---
paths:
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.meta"
---

# Scenes, Prefabs and GUIDs

## Prefer the Editor

- When an Editor is connected (Unity MCP), make scene/prefab changes through it and save from it. Hand-edit YAML only when no Editor is reachable, say so, keep the edit to the smallest block, and ask the user to open the scene afterwards to confirm it loads.
- Never invent or change a `guid` or `fileID`. Never delete or regenerate a `.meta` file. A `.meta` always travels with its asset (move/rename in the Unity Project window).

## The one scene

- `Assets/Design/Scenes/SampleScene.unity` is shared by everyone, so it is the worst merge surface in the project.
- **Tell Han before you change it.** Prefer changing a prefab over adding scene overrides; add new things as prefab instances.
- Before saving: exit Play mode, close every panel (Laptop, apps, card panels, Tax), put the camera back, and check `git diff --stat` shows only your change. **Never save the scene with the Billiards app open** (its layout overrides leak into the scene).
- List every scene change in your report (object path, component, field, old → new).

## Prefabs

- Panel prefabs are saved **inactive** like their siblings; their scripts don't `Awake` until opened. Objects that must initialise at load (e.g. future shop items) are saved active and hide themselves in `Start`.
- Don't re-save `Mining App.prefab` (or any prefab) by accident while inspecting it; check its diff.
- Five prefabs carry a stale serialized `pawnValue: 20` that no script reads. Leave it unless asked; Unity drops it on the next save.
- Desk objects that the Printer can pawn must be on layer 3 `DeskObject`.

## Merging

- `.gitattributes` routes scenes, prefabs and assets to `unityyamlmerge`. If the driver isn't configured on the machine (see `AGENTS.md` §2.3), git falls back to text merge. **Never hand-resolve a scene/prefab conflict**: stop and ask the user (usually: take one side, then redo the other change in the Editor).

## Panels and input

- A panel that blocks desk input gets an `InputBlocker` component (not the removed `GameManager.inputBlockers` list).
- Wire laptop apps by string: `LaptopApp.appName` must equal the `DesktopIconView.appName`, and the app must be in `LaptopController.apps`.
