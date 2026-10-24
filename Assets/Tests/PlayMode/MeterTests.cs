using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.2.2 của kế hoạch kiểm thử, TC-CLK-7…12: Meter đổi toàn bộ tiền thành thời gian ($1 = 1 s).
/// “Nhấp Meter” gọi đúng listener của nút feedButton (TestHooks.ClickMeter). Bước Arrange và lần nhấp
/// nằm trong cùng một khung hình nên Clock không kịp trôi giữa hai bước.
/// </summary>
public class MeterTests : GameSceneTest
{
    private static int Money => MoneyService.Instance.Current;
    private static float Clock => ClockService.Instance.RemainingTime;

    [Test]
    public void GiaTriTrongGameConfigKhopKeHoach()
    {
        Assert.AreEqual(10, Config.meterMinFeed, "Kế hoạch ghi mức tối thiểu của Meter là $10");
        Assert.AreEqual(1f, Config.exchangeRate, "Kế hoạch ghi $1 = 1 s");
    }

    // ---------- TC-CLK-7, 8, 9 ----------

    [Test]
    public void TC_CLK_7_DuoiMucToiThieu_TuChoi()
    {
        TestHooks.SetMoney(9);
        TestHooks.SetClock(120f);

        TestHooks.ClickMeter();

        Assert.AreEqual(9, Money);
        Assert.AreEqual(120f, Clock);
    }

    [Test]
    public void TC_CLK_8_DungMucToiThieu_Doi10Giay()
    {
        TestHooks.SetMoney(10);
        TestHooks.SetClock(120f);

        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money);
        Assert.AreEqual(130f, Clock);
    }

    [Test]
    public void TC_CLK_9_DungToanBoSoDu()
    {
        TestHooks.SetMoney(57);
        TestHooks.SetClock(120f);

        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money);
        Assert.AreEqual(177f, Clock);
    }

    // ---------- TC-CLK-10 ----------

    [UnityTest]
    public IEnumerator TC_CLK_10_FinalCountdownTriggered_MeterBiKhoa()
    {
        // Đưa Final Countdown sang Triggered bằng đúng đường của game: Armed, rồi Clock tự đếm về 0.
        UseFixedFrameRate(50);
        yield return null;
        FinalCountdownTracker.Instance.Arm(180f);
        TestHooks.SetClock(0.05f);
        for (int i = 0; i < 20 && FinalCountdownTracker.Instance.CurrentPhase != FinalCountdownTracker.Phase.Triggered; i++)
            yield return null;
        Assert.AreEqual(FinalCountdownTracker.Phase.Triggered, FinalCountdownTracker.Instance.CurrentPhase);
        Assert.IsTrue(Meter.Instance.IsLocked);

        TestHooks.SetMoney(50);
        float clockBefore = Clock;

        TestHooks.ClickMeter();

        Assert.AreEqual(50, Money);
        Assert.AreEqual(clockBefore, Clock, "Clock không được cộng thêm khi Meter bị khóa");
    }

    // ---------- TC-CLK-11 ----------

    [Test]
    public void TC_CLK_11_MeterDayClockVuot600_Thang()
    {
        TestHooks.SetMoney(57);
        TestHooks.SetClock(590f);

        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money);
        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.ClockWin, GameManager.Instance.LastEndReason);
    }

    // ---------- TC-CLK-12 ----------

    [Test]
    public void TC_CLK_12a_NhapDupCungKhungHinh_ChiCongMotLan()
    {
        TestHooks.SetMoney(50);
        TestHooks.SetClock(120f);

        TestHooks.ClickMeter();
        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money);
        Assert.AreEqual(170f, Clock);
    }

    [UnityTest]
    public IEnumerator TC_CLK_12b_NhapLanHaiSau50ms_BiTuChoi()
    {
        UseFixedFrameRate(20);             // 1 khung hình = 50 ms
        yield return null;
        TestHooks.SetMoney(50);
        TestHooks.SetClock(120f);

        TestHooks.ClickMeter();
        Assert.AreEqual(170f, Clock);

        yield return null;                 // 50 ms sau
        float clockBeforeSecondClick = Clock;
        TestHooks.ClickMeter();

        Assert.AreEqual(0, Money);
        Assert.AreEqual(clockBeforeSecondClick, Clock, "Lần nhấp thứ hai không được cộng thêm thời gian");
        Assert.AreEqual(169.95f, Clock, 0.001f, "Chỉ trôi đúng 50 ms giữa hai lần nhấp");
    }
}
