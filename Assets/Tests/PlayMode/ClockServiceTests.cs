using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.2.2 của kế hoạch kiểm thử, TC-CLK-2…6: đếm ngược, Phone làm Clock chạy nhanh, biên thắng/thua,
/// Final Countdown không chặn khoản trừ trực tiếp. Thời gian giả lập bằng UseFixedFrameRate (50 khung = 1 s).
/// </summary>
public class ClockServiceTests : GameSceneTest
{
    private const int Fps = 50;
    private const float Tolerance = 0.001f;

    private static ClockService Clock => ClockService.Instance;

    // ---------- TC-CLK-2, 3 ----------

    [UnityTest]
    public IEnumerator TC_CLK_2_DemNguocThuong_Troi1Giay()
    {
        UseFixedFrameRate(Fps);
        yield return null;                 // khung hình đầu đã dùng deltaTime cố định
        TestHooks.SetClock(120f);
        Clock.TickMultiplier = 1f;

        yield return Frames(Fps);

        Assert.AreEqual(119f, Clock.RemainingTime, Tolerance);
    }

    [UnityTest]
    public IEnumerator TC_CLK_3_PhoneDangReo_Troi1Giay()
    {
        UseFixedFrameRate(Fps);
        yield return null;
        TestHooks.SetClock(120f);
        Clock.TickMultiplier = 1.5f;       // giá trị Phone ghi khi đang reo

        yield return Frames(Fps);

        Assert.AreEqual(118.5f, Clock.RemainingTime, Tolerance);
    }

    // ---------- TC-CLK-4 ----------

    [Test]
    public void TC_CLK_4a_Clock599Cong1_Thang()
    {
        TestHooks.SetClock(599f);

        Clock.AddSeconds(1f, "test");

        Assert.AreEqual(600f, Clock.RemainingTime);
        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.ClockWin, GameManager.Instance.LastEndReason);
    }

    [Test]
    public void TC_CLK_4b_Clock598Cong1_ChuaThang()
    {
        TestHooks.SetClock(598f);

        Clock.AddSeconds(1f, "test");

        Assert.AreEqual(599f, Clock.RemainingTime);
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    // ---------- TC-CLK-5 ----------

    [UnityTest]
    public IEnumerator TC_CLK_5_Clock1Troi1Giay_Thua()
    {
        int finishedCount = 0;
        Clock.OnCountdownFinished += () => finishedCount++;
        UseFixedFrameRate(Fps);
        yield return null;
        TestHooks.SetClock(1f);

        // Chờ tối đa 2 s game time; đúng ra phải kết thúc sau khoảng 1 s.
        for (int i = 0; i < Fps * 2 && !GameManager.Instance.IsGameOver; i++) yield return null;

        Assert.AreEqual(0f, Clock.RemainingTime);
        Assert.AreEqual(1, finishedCount, "OnCountdownFinished phải phát đúng 1 lần");
        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.ClockLoss, GameManager.Instance.LastEndReason);
    }

    // ---------- TC-CLK-6 ----------

    [Test]
    public void TC_CLK_6_FinalCountdownArmed_TruTrucTiepVe0_VanThua()
    {
        TestHooks.SetClock(20f);
        FinalCountdownTracker.Instance.Arm(180f);
        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, FinalCountdownTracker.Instance.CurrentPhase);

        Clock.Spend(20f, "test");

        Assert.IsTrue(GameManager.Instance.IsGameOver,
            "Final Countdown chỉ chặn khi Clock tự đếm về 0, không chặn khoản trừ trực tiếp");
        Assert.AreEqual(EndReason.ClockLoss, GameManager.Instance.LastEndReason);
    }
}
