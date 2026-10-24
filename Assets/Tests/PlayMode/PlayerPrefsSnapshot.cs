using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sao lưu và khôi phục mọi khóa PlayerPrefs mà game dùng, để test Play Mode không ghi đè
/// dữ liệu thật của máy chạy test (lịch sử ván, Collection, gợi ý đã xem, hướng dẫn đã xem).
/// Danh sách khóa lấy từ các lời gọi PlayerPrefs trong Assets/Code/Scripts; thêm khóa mới vào AllKeys khi game có khóa mới.
/// </summary>
public sealed class PlayerPrefsSnapshot
{
    private const int RunHistorySize = 20;   // RunStatsService.HistorySize

    private readonly Dictionary<string, (bool exists, int i, string s)> _ints = new();
    private readonly Dictionary<string, (bool exists, int i, string s)> _strings = new();

    private static IEnumerable<string> IntKeys()
    {
        yield return "TutorialSeen";
        yield return "RunHistory_NextSlot";
        yield return "RunHistory_Total";
        foreach (HintId id in Enum.GetValues(typeof(HintId)))
            yield return $"HintSeen_{id}";
    }

    private static IEnumerable<string> StringKeys()
    {
        yield return "CardCollection_UnlockedIds";
        for (int i = 0; i < RunHistorySize; i++)
            yield return $"RunHistory_{i}";
    }

    public static PlayerPrefsSnapshot Take()
    {
        var snap = new PlayerPrefsSnapshot();
        foreach (var key in IntKeys())
            snap._ints[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key, 0), null);
        foreach (var key in StringKeys())
            snap._strings[key] = (PlayerPrefs.HasKey(key), 0, PlayerPrefs.GetString(key, null));
        return snap;
    }

    public void Restore()
    {
        foreach (var (key, v) in _ints)
        {
            if (v.exists) PlayerPrefs.SetInt(key, v.i);
            else PlayerPrefs.DeleteKey(key);
        }
        foreach (var (key, v) in _strings)
        {
            if (v.exists) PlayerPrefs.SetString(key, v.s);
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }
}
