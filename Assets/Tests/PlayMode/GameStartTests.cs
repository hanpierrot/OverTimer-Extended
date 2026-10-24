using NUnit.Framework;

/// <summary>
/// Smoke test Play Mode: scene game thật có đủ service và đúng trạng thái ván mới.
/// Bao gồm TC-MON-1 và TC-CLK-1 (phải chạy Play Mode vì giá trị được gán trong Start()).
/// </summary>
public class GameStartTests : GameSceneTest
{
    [Test]
    public void SceneCoDuCacService()
    {
        Assert.IsNotNull(GameManager.Instance, "Thiếu GameManager trong scene");
        Assert.IsNotNull(GameManager.Instance.GameConfig, "GameManager chưa gán GameConfig");
        Assert.IsNotNull(MoneyService.Instance, "Thiếu MoneyService trong scene");
        Assert.IsNotNull(ClockService.Instance, "Thiếu ClockService trong scene");
        Assert.IsNotNull(RngService.Instance, "Thiếu RngService trong scene");
        Assert.IsNotNull(Meter.Instance, "Thiếu Meter trong scene");
        Assert.IsNotNull(FinalCountdownTracker.Instance, "Thiếu FinalCountdownTracker trong scene");
    }

    [Test]
    public void TC_MON_1_SoDuBanDau()
    {
        Assert.AreEqual(Config.startMoney, MoneyService.Instance.Current);
        Assert.AreEqual(40, MoneyService.Instance.Current, "Kế hoạch ghi tiền khởi đầu $40");
    }

    [Test]
    public void TC_CLK_1_ClockBanDau()
    {
        float start = Config.startClock;
        Assert.AreEqual(120f, start, "Kế hoạch ghi Clock khởi đầu 120 s");

        // Clock đã chạy được một hai khung hình kể từ Start(), nên cho phép lệch nhỏ.
        Assert.That(ClockService.Instance.RemainingTime, Is.InRange(start - 0.5f, start));
        Assert.IsTrue(ClockService.Instance.IsRunning, "Clock phải tự chạy khi vào ván");
    }
}
