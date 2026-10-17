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

    public static object Call(object target, string method, params object[] args)
    {
        var info = FindMethod(target.GetType(), method);
        Assert.IsNotNull(info, $"{target.GetType().Name} không có hàm '{method}' (mã game đã đổi?)");
        return info.Invoke(target, args);
    }

    private static MethodInfo FindMethod(System.Type type, string name)
    {
        // Hàm private của lớp cha (ví dụ HoldTask) không thấy được từ lớp con, nên đi ngược lên cây kế thừa.
        for (var t = type; t != null; t = t.BaseType)
        {
            var m = t.GetMethod(name, Instance | BindingFlags.DeclaredOnly);
            if (m != null) return m;
        }
        return null;
    }

    public static T GetFieldInHierarchy<T>(object target, string name)
    {
        for (var t = target.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(name, Instance | BindingFlags.DeclaredOnly);
            if (f != null) return (T)f.GetValue(target);
        }
        Assert.Fail($"{target.GetType().Name} không có field '{name}' (mã game đã đổi?)");
        return default;
    }

    /// <summary>Tìm component trong scene, kể cả trên GameObject đang tắt (app Laptop, panel…).</summary>
    public static T Find<T>() where T : UnityEngine.Object
    {
        var found = UnityEngine.Object.FindAnyObjectByType<T>(UnityEngine.FindObjectsInactive.Include);
        Assert.IsNotNull(found, $"Không tìm thấy {typeof(T).Name} trong scene");
        return found;
    }

    /// <summary>Bấm một nút UI private của component (gọi đúng listener mà game đã gắn).</summary>
    public static void ClickButton(object owner, string buttonField) => GetField<Button>(owner, buttonField).onClick.Invoke();

    public static void SetMoney(int amount) => SetAutoProperty(MoneyService.Instance, "Current", amount);

    public static void SetClock(float seconds) => SetAutoProperty(ClockService.Instance, "RemainingTime", seconds);

    /// <summary>Thay bộ sinh ngẫu nhiên của RngService bằng seed cố định (quy ước kiểm thử 4.1.4).</summary>
    public static void SeedRng(int seed) => SetAutoProperty(RngService.Instance, "Random", new System.Random(seed));

    /// <summary>Nhấp Meter: gọi đúng listener mà nút feedButton gắn trong Meter.Awake.</summary>
    public static void ClickMeter() => GetField<Button>(Meter.Instance, "feedButton").onClick.Invoke();
}
