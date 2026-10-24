using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.6 của kế hoạch kiểm thử, TC-GAM-1a, 3…8 (TC-GAM-1b là [Dự kiến], TC-GAM-2 là kiểm thử thủ công).
/// Lịch sử ván và Collection được xóa trước khi nạp scene; fixture khôi phục dữ liệu thật sau mỗi test.
/// </summary>
public class GameLifecycleTests : GameSceneTest
{
    private const int HistorySize = 20;

    protected override void BeforeSceneLoad()
    {
        PlayerPrefs.DeleteKey("RunHistory_NextSlot");
        PlayerPrefs.DeleteKey("RunHistory_Total");
        for (int i = 0; i < HistorySize; i++) PlayerPrefs.DeleteKey($"RunHistory_{i}");
        PlayerPrefs.DeleteKey("CardCollection_UnlockedIds");
    }

    private static GameManager Game => GameManager.Instance;

    // ---------- TC-GAM-1a ----------

    // Không dùng [TestCase]: fixture có test tham số hóa không nạp được trong bản Player WebGL
    // ("DllNotFoundException: libc" lúc nạp test). Mỗi EndReason là một test riêng gọi chung hàm này.
    private static void AssertEndRunsThroughGameOver(EndReason reason)
    {
        Game.GameOver(reason);

        Assert.IsTrue(Game.IsGameOver);
        Assert.AreEqual(reason, Game.LastEndReason);
        Assert.IsFalse(InputManager.Instance.IsInputEnabled, "Input phải bị vô hiệu");
        Assert.IsFalse(ClockService.Instance.IsRunning, "Clock phải dừng");
        Assert.AreEqual(0f, Time.timeScale, "Time.timeScale = 0");
    }

    [Test] public void TC_GAM_1a_ClockWin() => AssertEndRunsThroughGameOver(EndReason.ClockWin);
    [Test] public void TC_GAM_1a_SetCompletionWin() => AssertEndRunsThroughGameOver(EndReason.SetCompletionWin);
    [Test] public void TC_GAM_1a_FinalCountdownWin() => AssertEndRunsThroughGameOver(EndReason.FinalCountdownWin);
    [Test] public void TC_GAM_1a_ClockLoss() => AssertEndRunsThroughGameOver(EndReason.ClockLoss);
    [Test] public void TC_GAM_1a_FinalCountdownLoss() => AssertEndRunsThroughGameOver(EndReason.FinalCountdownLoss);
    [Test] public void TC_GAM_1a_CreepyJesterLoss() => AssertEndRunsThroughGameOver(EndReason.CreepyJesterLoss);
    [Test] public void TC_GAM_1a_TicketJackpotWin() => AssertEndRunsThroughGameOver(EndReason.TicketJackpotWin);
    [Test] public void TC_GAM_1a_TicketCurseLoss() => AssertEndRunsThroughGameOver(EndReason.TicketCurseLoss);
    [Test] public void TC_GAM_1a_MotherLodeWin() => AssertEndRunsThroughGameOver(EndReason.MotherLodeWin);
    [Test] public void TC_GAM_1a_BlackjackCharlieWin() => AssertEndRunsThroughGameOver(EndReason.BlackjackCharlieWin);

    // ---------- TC-GAM-3 ----------

    [Test]
    public void TC_GAM_3_KetThucVanDau_GhiRunHistory0()
    {
        Game.GameOver(EndReason.ClockLoss);

        string json = PlayerPrefs.GetString("RunHistory_0", "");
        Assert.IsNotEmpty(json, "RunHistory_0 phải có bản ghi");
        Assert.AreEqual("ClockLoss", JsonUtility.FromJson<RunRecord>(json).winCondition);
        Assert.AreEqual(1, PlayerPrefs.GetInt("RunHistory_NextSlot", -1));
    }

    // ---------- TC-GAM-4 ----------

    [Test]
    public void TC_GAM_4_BoDemVong20O()
    {
        var stats = RunStatsService.Instance;
        void Save(int runNumber) => TestHooks.Call(stats, "SaveRecord", new RunRecord { runNumber = runNumber });

        for (int i = 1; i <= 20; i++) Save(i);
        Assert.AreEqual(0, PlayerPrefs.GetInt("RunHistory_NextSlot", -1), "Sau 20 ván: NextSlot = 0");

        Save(21);
        Assert.AreEqual(21, JsonUtility.FromJson<RunRecord>(PlayerPrefs.GetString("RunHistory_0")).runNumber,
            "Ván 21 ghi đè ô 0");
        Assert.AreEqual(1, PlayerPrefs.GetInt("RunHistory_NextSlot", -1));
    }

    // ---------- TC-GAM-5 ----------

    [UnityTest]
    public IEnumerator TC_GAM_5_CollectionConNguyenSauKhiMoLai()
    {
        var steve = Cards.Asset("Steve");
        CardCollectionManager.Instance.Register(steve);

        yield return ReloadScene();

        Assert.IsTrue(CardCollectionManager.Instance.IsCollected(steve));
        StringAssert.Contains("Steve", PlayerPrefs.GetString("CardCollection_UnlockedIds", ""));
    }

    // ---------- TC-GAM-6 ----------

    [UnityTest]
    public IEnumerator TC_GAM_6_ChoiLai_VanMoiDuLieuGiuNguyen()
    {
        var deeKay = Cards.Asset("DeeKay");
        CardCollectionManager.Instance.Register(deeKay);
        Cards.Place(Cards.Plain(0));
        TestHooks.SetMoney(99);
        Game.GameOver(EndReason.ClockLoss);

        Game.Restart();
        yield return null;
        yield return null;

        Assert.IsFalse(GameManager.Instance.IsGameOver);
        Assert.That(ClockService.Instance.RemainingTime, Is.InRange(Config.startClock - 0.5f, Config.startClock));
        Assert.AreEqual(Config.startMoney, MoneyService.Instance.Current);
        Assert.AreEqual(0, Cards.HandCount);
        Assert.IsTrue(CardCollectionManager.Instance.IsCollected(deeKay), "Collection phải giữ nguyên");
        Assert.AreEqual(1, PlayerPrefs.GetInt("RunHistory_NextSlot", -1), "Lịch sử ván phải giữ nguyên");
    }

    // ---------- TC-GAM-7 ----------

    [UnityTest]
    public IEnumerator TC_GAM_7_HaiDieuKienKetThucCungKhungHinh_GameOverMotLan()
    {
        int gameOverCalls = 0;
        Game.OnGameOver += _ => gameOverCalls++;
        var mei = (SetCompletionCardSO)Cards.Asset("Mei");
        Cards.Place(mei);
        for (int i = 0; i < 3; i++) Cards.Place(mei.pieces[i].alternatives[0]);
        UseFixedFrameRate(50);
        yield return null;
        TestHooks.SetClock(0.01f);

        yield return null;                                       // Clock về 0 trong khung hình này…
        Cards.Place(mei.pieces[3].alternatives[0]);              // …và đặt mảnh cuối cũng trong khung hình đó

        Assert.AreEqual(1, gameOverCalls, "GameOver phải được gọi đúng 1 lần");
        TestContext.Out.WriteLine($"EndReason = {Game.LastEndReason} (thứ tự ưu tiên: [Xác nhận SRS])");
    }

    // ---------- TC-GAM-8 ----------

    [Test]
    public void TC_GAM_8_GoiGameOverLanHai_BiBoQua()
    {
        int gameOverCalls = 0;
        Game.OnGameOver += _ => gameOverCalls++;
        Game.GameOver(EndReason.ClockLoss);
        string recordAfterFirst = PlayerPrefs.GetString("RunHistory_0", "");

        Game.GameOver(EndReason.ClockWin);

        Assert.AreEqual(EndReason.ClockLoss, Game.LastEndReason, "Kết quả ván không được đổi");
        Assert.AreEqual(1, gameOverCalls, "Lần gọi thứ hai phải bị bỏ qua");
        Assert.AreEqual(1, PlayerPrefs.GetInt("RunHistory_NextSlot", -1), "Không được ghi thêm RunRecord");
        Assert.AreEqual(recordAfterFirst, PlayerPrefs.GetString("RunHistory_0", ""));
    }
}
