using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.3.4 của kế hoạch kiểm thử, TC-TSK-1…6: Egg Timer, Click Task, Power Bank, Wire Task, Laptop bị khóa.
/// Thao tác chuột đi qua các hàm public của PointerReceiver (OnPressStart/Update/End, OnClickDown) –
/// đúng đường mà InputManager gọi khi người chơi nhấp hoặc giữ chuột.
/// Mỗi khung hình = 0,02 s (UseFixedFrameRate(50)).
/// </summary>
public class DeskTaskTests : GameSceneTest
{
    private const int Fps = 50;
    private const float Dt = 1f / Fps;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        yield return null;
    }

    /// <summary>Vật dụng trên bàn đang bật (vật bị tắt không nhận input, ca kiểm thử không còn ý nghĩa).</summary>
    private static T ActiveOnDesk<T>() where T : Component
    {
        var item = TestHooks.Find<T>();
        Assert.IsTrue(item.gameObject.activeInHierarchy, $"{typeof(T).Name} đang tắt trong scene");
        return item;
    }

    private static float Clock => ClockService.Instance.RemainingTime;
    private static int Money => MoneyService.Instance.Current;

    /// <summary>Giữ chuột trên vật trong số khung hình cho trước (mỗi khung gọi PressUpdate như InputManager).</summary>
    private static IEnumerator Hold(PointerReceiver receiver, int frames, bool release = true)
    {
        receiver.OnPressStart(Vector2.zero);
        for (int i = 0; i < frames; i++)
        {
            yield return null;
            receiver.OnPressUpdate(Vector2.zero);
        }
        if (release) receiver.OnPressEnd(Vector2.zero);
    }

    // ---------- TC-TSK-1, 2 ----------

    [UnityTest]
    public IEnumerator TC_TSK_1_EggTimer_Giu1Giay_Cong3Giay()
    {
        var receiver = ActiveOnDesk<EggTimerTask>().GetComponent<PointerReceiver>();
        TestHooks.SetClock(100f);

        yield return Hold(receiver, Fps + 1);   // 1,0 s (+1 khung bù sai số cộng dồn số thực)

        // Clock trôi tự nhiên (Fps + 1) × 0,02 s trong lúc giữ; phần còn lại phải là +3 s.
        float naturalDrift = (Fps + 1) * Dt;
        Assert.AreEqual(100f - naturalDrift + 3f, Clock, 0.001f);
    }

    [UnityTest]
    public IEnumerator TC_TSK_2_EggTimer_ThaSom_KhongCongVaDatLaiTienTrinh()
    {
        var task = ActiveOnDesk<EggTimerTask>();
        var receiver = task.GetComponent<PointerReceiver>();
        TestHooks.SetClock(100f);

        yield return Hold(receiver, 45);        // 0,9 s rồi thả

        Assert.AreEqual(100f - 45 * Dt, Clock, 0.001f, "Thả sớm không được cộng thời gian");
        Assert.AreEqual(0f, TestHooks.GetFieldInHierarchy<float>(task, "holdTimer"), "Tiến trình phải về 0");
    }

    // ---------- TC-TSK-3 ----------

    [UnityTest]
    public IEnumerator TC_TSK_3_ClickTask_NhapDuSoLan_Cong5VaHoi3Giay()
    {
        var task = Object.FindObjectsByType<ClickTask>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .First(t => t.GetType() == typeof(ClickTask));   // bỏ qua PictureClickTask (lớp con)
        Assert.IsTrue(task.gameObject.activeInHierarchy, "ClickTask đang tắt trong scene");
        var receiver = task.GetComponent<PointerReceiver>();
        int required = TestHooks.GetField<int>(task, "requiredClicks");
        Assert.That(required, Is.InRange(3, 8));
        TestHooks.SetMoney(0);

        for (int i = 0; i < required - 1; i++) receiver.OnClickDown(Vector2.zero);
        Assert.AreEqual(0, Money, $"Mới nhấp {required - 1}/{required} lần, chưa được thưởng");

        receiver.OnClickDown(Vector2.zero);
        Assert.AreEqual(5, Money);
        Assert.IsTrue(task.IsCompleted);

        receiver.OnClickDown(Vector2.zero);      // đang hồi: không thưởng thêm
        Assert.AreEqual(5, Money);

        yield return Frames(3 * Fps + 5);       // hết 3 s hồi
        Assert.IsFalse(task.IsCompleted, "Sau 3 s phải dùng lại được");
        Assert.AreEqual(0, TestHooks.GetField<int>(task, "currentClicks"));
        Assert.That(TestHooks.GetField<int>(task, "requiredClicks"), Is.InRange(3, 8));
    }

    // ---------- TC-TSK-4 ----------

    [UnityTest]
    public IEnumerator TC_TSK_4_PowerBank_Giu2Giay_Cong8VaVaoHoi()
    {
        GiveItem("power_bank");                     // Power Bank là món mua ở Shop, đầu ván bị ẩn
        var receiver = ActiveOnDesk<PowerBankTask>().GetComponent<PointerReceiver>();
        TestHooks.SetMoney(0);

        yield return Hold(receiver, 2 * Fps + 1);

        Assert.AreEqual(8, Money);
        Assert.IsFalse(receiver.IsInteractable, "Power Bank phải vào thời gian hồi");
    }

    // ---------- TC-TSK-5 ----------

    private static void Connect(WirePlug plug, WireSocket socket)
    {
        plug.OnPointerDown(null);
        socket.OnDrop(null);
        plug.OnEndDrag(null);
    }

    [Test]
    public void TC_TSK_5a_WireTask_NoiDung5Cap_MoKhoaLaptop()
    {
        var wire = TestHooks.Find<WireTask>();
        var plugs = TestHooks.GetField<WirePlug[]>(wire, "plugs");
        var sockets = TestHooks.GetField<WireSocket[]>(wire, "sockets");
        Assert.AreEqual(5, plugs.Length, "Kế hoạch ghi Wire Task có 5 cặp dây");
        wire.BeginPuzzle();
        TestHooks.SetMoney(0);
        Assert.IsTrue(Laptop.Disabled, "Laptop phải bị khóa trước khi giải");

        foreach (var plug in plugs)
            Connect(plug, sockets.First(s => s.PairIndex == plug.PairIndex));

        Assert.AreEqual(20, Money);
        Assert.IsTrue(wire.IsCompleted);
        Assert.IsFalse(Laptop.Disabled, "Laptop phải được mở khóa");
    }

    [Test]
    public void TC_TSK_5b_WireTask_NoiSai_BiTuChoi()
    {
        var wire = TestHooks.Find<WireTask>();
        var plugs = TestHooks.GetField<WirePlug[]>(wire, "plugs");
        var sockets = TestHooks.GetField<WireSocket[]>(wire, "sockets");
        wire.BeginPuzzle();
        TestHooks.SetMoney(0);

        var plug = plugs[0];
        Connect(plug, sockets.First(s => s.PairIndex != plug.PairIndex));

        Assert.AreEqual(0, TestHooks.GetField<int>(wire, "_connectedCount"), "Cặp sai không được tính");
        Assert.AreEqual(0, Money);
        Assert.IsTrue(Laptop.Disabled);
    }

    // ---------- TC-TSK-6 ----------

    [Test]
    public void TC_TSK_6_LaptopKhiChuaGiaiWireTask_KhongMoDuoc()
    {
        var opener = TestHooks.Find<DeskLaptopOpener>();
        var panel = TestHooks.GetField<GameObject>(opener, "laptopPanel");
        Assert.IsTrue(Laptop.Disabled);
        Assert.IsFalse(panel.activeSelf);

        TestHooks.Call(opener, "OnClicked", Vector2.zero);

        Assert.IsFalse(panel.activeSelf, "Laptop chưa có điện thì không được mở");
    }
}
