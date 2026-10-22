using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;

/// Writes a table the way the hand-made ones are written (short numbers, one ball per line). JsonUtility.ToJson turns
/// 0.45f into 0.44999998807907107, which makes the files unreadable and every diff noisy.
public static class BilliardsPresetJson
{
    static string N(float v, string format = "0.####") => v.ToString(format, CultureInfo.InvariantCulture);

    static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    static string List(string[] items)
    {
        if (items == null || items.Length == 0) return "[]";
        var parts = new string[items.Length];
        for (int i = 0; i < items.Length; i++) parts[i] = Q(items[i]);
        return "[" + string.Join(", ", parts) + "]";
    }

    public static string ToJson(BilliardsPreset p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"id\": {Q(p.id)},");
        sb.AppendLine($"  \"name\": {Q(p.name)},");
        sb.AppendLine($"  \"par\": {p.par},");
        if (p.shots != null && p.shots.Length > 0) sb.AppendLine($"  \"shots\": [{string.Join(", ", p.shots)}],");
        if (System.Math.Abs(p.payoutScale - 1f) > 1e-6f) sb.AppendLine($"  \"payoutScale\": {N(p.payoutScale)},");

        sb.AppendLine("  \"balls\": [");
        for (int i = 0; i < p.balls.Length; i++)
            sb.AppendLine($"    {{ \"id\": {Q(p.balls[i].id)}, \"x\": {N(p.balls[i].x)}, \"y\": {N(p.balls[i].y)} }}{(i < p.balls.Length - 1 ? "," : "")}");
        sb.AppendLine("  ],");

        sb.AppendLine($"  \"order\": {List(p.order)},");
        sb.AppendLine($"  \"forbidden\": {List(p.forbidden)},");

        var called = p.called ?? new CalledPocket[0];
        if (called.Length == 0) sb.AppendLine("  \"called\": [],");
        else
        {
            sb.AppendLine("  \"called\": [");
            for (int i = 0; i < called.Length; i++)
                sb.AppendLine($"    {{ \"ball\": {Q(called[i].ball)}, \"pocket\": {Q(called[i].pocket)} }}{(i < called.Length - 1 ? "," : "")}");
            sb.AppendLine("  ],");
        }

        sb.AppendLine("  \"solution\": [");
        var solution = p.solution ?? new ShotInput[0];
        for (int i = 0; i < solution.Length; i++)
            sb.AppendLine($"    {{ \"angle\": {N(solution[i].angle, "0.0")}, \"power\": {N(solution[i].power, "0.00")} }}{(i < solution.Length - 1 ? "," : "")}");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// Writes Assets/Resources/BilliardsPresets/<id>.json and imports it.
    public static string Write(BilliardsPreset p)
    {
        string path = $"Assets/Resources/BilliardsPresets/{p.id}.json";
        Directory.CreateDirectory("Assets/Resources/BilliardsPresets");
        File.WriteAllText(path, ToJson(p));
        AssetDatabase.ImportAsset(path);
        return path;
    }

    [MenuItem("Tools/OverTimer/Billiards/Reformat preset files")]
    static void ReformatAll()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources/BilliardsPresets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".json")) continue;
            var p = BilliardsPresetLibrary.Parse(File.ReadAllText(path), path);
            if (p == null) continue;
            File.WriteAllText(path, ToJson(p));
            AssetDatabase.ImportAsset(path);
            n++;
        }
        UnityEngine.Debug.Log($"[Billiards] reformatted {n} preset file(s)");
    }
}
