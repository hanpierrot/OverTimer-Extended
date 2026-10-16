using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Billiards test buttons for the cheat menu: the debug overlay, a forced table mirror, a forced table and the preset
/// verifier. Adds its buttons to the same list the cutscene buttons use, so CheatMenuController itself is not touched.
/// State lives in BilliardsDebug. The verifier is the same pure-C# code the Editor menu runs, so it works in a build.
public class BilliardsCheats : MonoBehaviour
{
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    private TMP_Text _overlayLabel;
    private TMP_Text _mirrorLabel;
    private TMP_Text _tableLabel;
    private TMP_Text _verifyLabel;
    private TMP_Text _logLabel;
    private int _tableIndex = -1;                                 // -1 = random draw

    private void Start()
    {
        if (buttonContainer == null || buttonPrefab == null) return;

        _overlayLabel = AddButton(ToggleOverlay);
        _mirrorLabel = AddButton(CycleMirror);
        _tableLabel = AddButton(CycleTable);
        _verifyLabel = AddButton(VerifyPresets);
        _logLabel = AddButton(ShowRoundLog);
        BilliardsDebug.Changed += Refresh;
        Refresh();
        if (_verifyLabel != null) _verifyLabel.text = "Billiards: verify presets";
        if (_logLabel != null) _logLabel.text = "Billiards: round log";
    }

    /// Phase 9 playtest log (dev builds only): the full summary goes to the console, the button shows the totals.
    private void ShowRoundLog()
    {
        var rounds = BilliardsRoundLog.LoadAll();
        Debug.Log("[Billiards] Round log summary (" + BilliardsRoundLog.FilePath + ")\n" + BilliardsRoundLog.Summarize(rounds));
        if (_logLabel == null) return;
        int wins = 0;
        foreach (var e in rounds) if (e.outcome == "Win") wins++;
        _logLabel.text = !BilliardsRoundLog.Enabled ? "Round log: dev builds only"
                       : rounds.Count == 0 ? "Round log: no rounds yet"
                       : $"Round log: {rounds.Count} rounds, {wins * 100 / rounds.Count}% won";
    }

    private void OnDestroy() => BilliardsDebug.Changed -= Refresh;

    private TMP_Text AddButton(UnityEngine.Events.UnityAction onClick)
    {
        Button btn = Instantiate(buttonPrefab, buttonContainer);
        btn.onClick.AddListener(onClick);
        return btn.GetComponentInChildren<TMP_Text>();
    }

    private static void ToggleOverlay() => BilliardsDebug.Overlay = !BilliardsDebug.Overlay;

    private static void CycleMirror() =>
        BilliardsDebug.Mirror = (BilliardsDebug.MirrorMode)(((int)BilliardsDebug.Mirror + 1) % 5);

    /// Random, then each table in turn, then random again.
    private void CycleTable()
    {
        var all = BilliardsPresetLibrary.All;
        _tableIndex++;
        if (_tableIndex >= all.Count) _tableIndex = -1;
        BilliardsDebug.PresetId = _tableIndex < 0 ? null : all[_tableIndex].id;
    }

    private void VerifyPresets()
    {
        var gm = GameManager.Instance;
        var cfg = gm != null ? gm.GameConfig : null;
        if (cfg == null || !cfg.ValidateBilliards(out string problem))
        {
            if (_verifyLabel != null) _verifyLabel.text = "Billiards verify: config invalid";
            return;
        }
        var rows = BilliardsPresetVerifier.VerifyAll(cfg);
        string report = BilliardsPresetVerifier.Report(rows, out int pass, out int fail, out int skipped, out int knife);
        Debug.Log("[Billiards] Verify Presets (in-game)\n" + report);          // full per-table report goes to the console
        if (_verifyLabel != null)
            _verifyLabel.text = $"Verify: {pass} pass, {fail} fail, {knife} knife-edge, {skipped} skipped";
    }

    private void Refresh()
    {
        if (_overlayLabel != null) _overlayLabel.text = "Billiards overlay: " + (BilliardsDebug.Overlay ? "ON" : "off");
        if (_mirrorLabel != null) _mirrorLabel.text = "Billiards mirror: " + BilliardsDebug.Mirror;
        if (_tableLabel != null) _tableLabel.text = "Billiards table: " + (BilliardsDebug.PresetId ?? "random");
    }
}
