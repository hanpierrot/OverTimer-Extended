using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>Nạp asset thật của dự án cho test Edit Mode (giá trị cân bằng không viết cứng trong test).</summary>
public static class TestAssets
{
    /// <summary>Tìm GameConfig theo kiểu, không theo đường dẫn: chuyển asset sang thư mục khác không làm hỏng test.</summary>
    public static GameConfig LoadGameConfig()
    {
        var guids = AssetDatabase.FindAssets("t:GameConfig");
        Assert.IsNotEmpty(guids, "Không tìm thấy asset GameConfig nào trong project");
        Assert.AreEqual(1, guids.Length, "Có hơn một GameConfig: test không biết dùng cái nào");

        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Assert.IsNotNull(config, "Không nạp được GameConfig");
        return config;
    }

    public static BilliardsPreset LoadPreset(string id)
    {
        var preset = BilliardsPresetLibrary.All.FirstOrDefault(p => p.id == id);
        Assert.IsNotNull(preset, $"Không tìm thấy bàn Billiards '{id}' trong Resources/BilliardsPresets");
        return preset;
    }
}
