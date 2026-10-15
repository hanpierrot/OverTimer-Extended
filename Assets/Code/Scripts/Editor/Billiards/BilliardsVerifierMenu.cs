#if !BILLIARDS_HARNESS
using UnityEditor;
using UnityEngine;

public static class BilliardsVerifierMenu
{
    [MenuItem("Tools/OverTimer/Billiards/Verify Presets")]
    static void Verify()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Settings/Configs/GameConfig.asset");
        if (cfg == null) { Debug.LogError("[Billiards] GameConfig.asset not found"); return; }
        if (!cfg.ValidateBilliards(out string problem)) { Debug.LogError("[Billiards] GameConfig invalid: " + problem); return; }

        var rows = BilliardsPresetVerifier.VerifyAll(cfg);
        string report = BilliardsPresetVerifier.Report(rows, out _, out int fail, out _, out int knife);
        string text = "[Billiards] Verify Presets\n" + report;
        if (fail > 0) Debug.LogError(text);
        else if (knife > 0) Debug.LogWarning(text);
        else Debug.Log(text);
    }
}
#endif
