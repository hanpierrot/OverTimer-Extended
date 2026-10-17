using System.Reflection;
using NUnit.Framework;
using UnityEngine.UI;

/// <summary>
/// Truy cập phần private của game, CHỈ dùng cho bước Arrange (đặt trạng thái ban đầu) và để bấm nút UI.
/// Game không có setter công khai cho Money/Clock (đúng thiết kế: chỉ MoneyService/ClockService được đổi),
/// nên test đặt thẳng giá trị để không phát sự kiện thừa (thuế ITRS, thống kê ván…).
/// </summary>
public static class TestHooks
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    public static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, Instance);
        Assert.IsNotNull(field, $"{target.GetType().Name} không có field '{name}' (mã game đã đổi?)");
        field.SetValue(target, value);
    }

    public static T GetField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, Instance);
        Assert.IsNotNull(field, $"{target.GetType().Name} không có field '{name}' (mã game đã đổi?)");
        return (T)field.GetValue(target);
    }

    /// <summary>Đặt giá trị cho auto-property có private set (ghi thẳng vào backing field).</summary>
    public static void SetAutoProperty(object target, string property, object value)
        => SetField(target, $"<{property}>k__BackingField", value);

    public static void SetStaticAutoProperty<TOwner>(string property, object value)
    {
        var field = typeof(TOwner).GetField($"<{property}>k__BackingField", Static);
        Assert.IsNotNull(field, $"{typeof(TOwner).Name} không có static property '{property}'");
        field.SetValue(null, value);
    }

    public static void SetMoney(int amount) => SetAutoProperty(MoneyService.Instance, "Current", amount);

    public static void SetClock(float seconds) => SetAutoProperty(ClockService.Instance, "RemainingTime", seconds);

    /// <summary>Thay bộ sinh ngẫu nhiên của RngService bằng seed cố định (quy ước kiểm thử 4.1.4).</summary>
    public static void SeedRng(int seed) => SetAutoProperty(RngService.Instance, "Random", new System.Random(seed));

    /// <summary>Nhấp Meter: gọi đúng listener mà nút feedButton gắn trong Meter.Awake.</summary>
    public static void ClickMeter() => GetField<Button>(Meter.Instance, "feedButton").onClick.Invoke();
}
