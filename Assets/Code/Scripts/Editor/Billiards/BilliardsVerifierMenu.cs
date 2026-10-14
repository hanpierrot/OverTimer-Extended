#if !BILLIARDS_HARNESS
using System.Text;
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
        var sb = new StringBuilder("[Billiards] Verify Presets\n");
        int pass = 0, fail = 0, skipped = 0;
        foreach (var r in rows)
        {
            string status = r.skipped ? "SKIP" : r.pass ? "PASS" : "FAIL";
            sb.AppendLine($"  {status}  {r.id,-22} {r.reason}");
            if (r.skipped) skipped++; else if (r.pass) pass++; else fail++;
        }
        sb.Append($"  {pass} passed, {fail} failed, {skipped} skipped");
        if (fail > 0) Debug.LogError(sb.ToString()); else Debug.Log(sb.ToString());
    }
}
#endif
