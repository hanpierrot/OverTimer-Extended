using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Smoke test Play Mode: nạp scene game thật và kiểm tra trạng thái ván mới.
/// Bao gồm TC-MON-1 và TC-CLK-1 (phải chạy Play Mode vì giá trị được gán trong Start()).
/// </summary>
public class GameStartTests
{
    private const string GameScene = "SampleScene";

    [UnitySetUp]
    public IEnumerator LoadGameScene()
    {
        yield return SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
        yield return null; // chờ một khung hình để mọi Start() chạy xong
    }

    [Test]
    public void SceneCoDuCacService()
    {
        Assert.IsNotNull(GameManager.Instance, "Thiếu GameManager trong scene");
        Assert.IsNotNull(GameManager.Instance.GameConfig, "GameManager chưa gán GameConfig");
        Assert.IsNotNull(MoneyService.Instance, "Thiếu MoneyService trong scene");
        Assert.IsNotNull(ClockService.Instance, "Thiếu ClockService trong scene");
        Assert.IsNotNull(RngService.Instance, "Thiếu RngService trong scene");
    }

    [Test]
    public void TC_MON_1_SoDuBanDau()
    {
        Assert.AreEqual(GameManager.Instance.GameConfig.startMoney, MoneyService.Instance.Current);
        Assert.AreEqual(40, MoneyService.Instance.Current, "Kế hoạch ghi tiền khởi đầu $40");
    }

    [Test]
    public void TC_CLK_1_ClockBanDau()
    {
        float start = GameManager.Instance.GameConfig.startClock;
        Assert.AreEqual(120f, start, "Kế hoạch ghi Clock khởi đầu 120 s");

        // Clock đã chạy được một hai khung hình kể từ Start(), nên cho phép lệch nhỏ.
        Assert.That(ClockService.Instance.RemainingTime, Is.InRange(start - 0.5f, start));
    }
}
