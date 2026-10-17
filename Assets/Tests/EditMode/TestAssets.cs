using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>Nạp asset thật của dự án cho test Edit Mode (giá trị cân bằng không viết cứng trong test).</summary>
public static class TestAssets
{
    public const string GameConfigPath = "Assets/Settings/Configs/GameConfig.asset";

    public static GameConfig LoadGameConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
        Assert.IsNotNull(config, $"Không tìm thấy GameConfig ở {GameConfigPath}");
        return config;
    }

    public static BilliardsPreset LoadPreset(string id)
    {
        var preset = BilliardsPresetLibrary.All.FirstOrDefault(p => p.id == id);
        Assert.IsNotNull(preset, $"Không tìm thấy bàn Billiards '{id}' trong Resources/BilliardsPresets");
        return preset;
    }
}
