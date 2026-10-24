using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Mục 4.4.3 của kế hoạch kiểm thử, TC-WIN-1…8 (TC-WIN-9 đang treo, chờ xác nhận chuyện dừng Clock).
/// Thẻ được đặt qua đúng đường nút Place (CardPackRevealPanel.TryPlaceCard).
/// </summary>
public class WinLossCardTests : GameSceneTest
{
    private static GameManager Game => GameManager.Instance;
    private static FinalCountdownTracker Tracker => FinalCountdownTracker.Instance;

    private static CardSO[] MeiPieces()
    {
        var mei = (SetCompletionCardSO)Cards.Asset("Mei");
        Assert.AreEqual(4, mei.pieces.Length, "Kế hoạch ghi bộ Mei có 4 nhóm mảnh ghép");
        return mei.pieces.Select(g => g.alternatives[0]).ToArray();
    }

    /// <summary>Đưa Final Countdown sang Triggered bằng đường của game: Armed rồi để Clock tự về 0.</summary>
    private static IEnumerator TriggerFinalCountdown()
    {
        UseFixedFrameRate(50);
        yield return null;
        Cards.Place(Cards.Asset("FinalCountdown"));
        TestHooks.SetClock(0.05f);
        for (int i = 0; i < 20 && Tracker.CurrentPhase != FinalCountdownTracker.Phase.Triggered; i++) yield return null;
        Assert.AreEqual(FinalCountdownTracker.Phase.Triggered, Tracker.CurrentPhase);
    }

    // ---------- TC-WIN-1, 2 ----------

    [Test]
    public void TC_WIN_1_SetCompletionDuBo_Thang()
    {
        var pieces = MeiPieces();
        Cards.Place(Cards.Asset("Mei"));
        for (int i = 0; i < 3; i++) Cards.Place(pieces[i]);
        Assert.IsFalse(Game.IsGameOver);

        Cards.Place(pieces[3]);

        Assert.IsTrue(Game.IsGameOver);
        Assert.AreEqual(EndReason.SetCompletionWin, Game.LastEndReason);
    }

    [Test]
    public void TC_WIN_2_SetCompletionThieuMotNhom_KhongThang()
    {
        var pieces = MeiPieces();
        Cards.Place(Cards.Asset("Mei"));
        for (int i = 0; i < 3; i++) Cards.Place(pieces[i]);

        Assert.IsFalse(Game.IsGameOver);
    }

    // ---------- TC-WIN-3 ----------

    [Test]
    public void TC_WIN_3_DatFinalCountdown_Armed()
    {
        Assert.AreEqual(FinalCountdownTracker.Phase.None, Tracker.CurrentPhase);

        Cards.Place(Cards.Asset("FinalCountdown"));

        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, Tracker.CurrentPhase);
    }

    // ---------- TC-WIN-4 ----------

    [UnityTest]
    public IEnumerator TC_WIN_4_FinalCountdownChanLanThuaDau()
    {
        UseFixedFrameRate(50);
        yield return null;
        Cards.Place(Cards.Asset("FinalCountdown"));
        TestHooks.SetClock(1f);

        yield return Frames(55);                 // 1,1 s: Clock tự đếm về 0

        Assert.IsFalse(Game.IsGameOver, "Lần thua đầu phải bị chặn");
        Assert.AreEqual(FinalCountdownTracker.Phase.Triggered, Tracker.CurrentPhase);
        Assert.That(ClockService.Instance.RemainingTime, Is.InRange(179f, 180f), "Clock được đặt lại 180 s");
        Assert.IsTrue(Meter.Instance.IsLocked, "Meter bị khóa");
    }

    // ---------- TC-WIN-5, 6 ----------

    [Test]
    public void TC_WIN_5_BanThuHaiKhiArmed_Thua()
    {
        var fc = Cards.Asset("FinalCountdown");
        Cards.Place(fc);

        Cards.Place(fc);

        Assert.IsTrue(Game.IsGameOver);
        Assert.AreEqual(EndReason.FinalCountdownLoss, Game.LastEndReason);
    }

    [UnityTest]
    public IEnumerator TC_WIN_6_BanThuHaiKhiTriggered_Thang()
    {
        yield return TriggerFinalCountdown();

        Cards.Place(Cards.Asset("FinalCountdown"));

        Assert.IsTrue(Game.IsGameOver);
        Assert.AreEqual(EndReason.FinalCountdownWin, Game.LastEndReason);
    }

    // ---------- TC-WIN-7 ----------

    [Test]
    public void TC_WIN_7a_PawnKhiArmed_ChoPhep_TonPhiDismiss()
    {
        // Khi Armed, FinalCountdownCardSO.isDebuff là true: thẻ là debuff đang chạy nên cầm tốn debuffDismissFee và không nhận pawnValue.
        var card = Cards.Place(Cards.Asset("FinalCountdown"));
        Assert.IsTrue(card.IsDebuffActive, "Armed được tính là debuff đang chạy");
        TestHooks.SetMoney(20);

        Pawning.PawnOnto(card);

        Assert.IsFalse(card.gameObject.activeSelf, "Thẻ phải được cầm đồ");
        Assert.AreEqual(20 - Config.debuffDismissFee, MoneyService.Instance.Current, "Trả phí dismiss, không nhận pawnValue");
        Assert.AreEqual(FinalCountdownTracker.Phase.None, Tracker.CurrentPhase, "Pawn khi Armed thì gỡ Final Countdown");
    }

    [Test]
    public void TC_WIN_7a2_PawnKhiArmed_ThieuTienPhi_TuChoiVaVanArmed()
    {
        var card = Cards.Place(Cards.Asset("FinalCountdown"));
        int broke = Config.debuffDismissFee - 1;
        TestHooks.SetMoney(broke);

        Pawning.PawnOnto(card);

        Assert.IsTrue(card.gameObject.activeSelf, "Không đủ phí thì thẻ ở lại bàn");
        Assert.AreEqual(broke, MoneyService.Instance.Current);
        Assert.AreEqual(FinalCountdownTracker.Phase.Armed, Tracker.CurrentPhase, "Vẫn Armed");
    }

    [UnityTest]
    public IEnumerator TC_WIN_7b_PawnKhiTriggered_TuChoi()
    {
        yield return TriggerFinalCountdown();
        var card = CardHandManager.Instance.Placed.Single();
        TestHooks.SetMoney(0);

        Pawning.PawnOnto(card);

        Assert.IsTrue(card.gameObject.activeSelf, "Không được cầm đồ khi đã Triggered");
        Assert.AreEqual(0, MoneyService.Instance.Current);
    }

    // ---------- TC-WIN-8 ----------

    private const string SentinelKey = "OverTimerTest_Sentinel";

    private static float JesterSeconds(CardSO jester)
    {
        var art = TestHooks.GetField<Sprite[]>(jester, "artPool");
        float interval = TestHooks.GetField<float>(jester, "intervalSeconds");
        return interval * (art.Length - 1);
    }

    [UnityTest]
    [Explicit("Creepy Jester gọi PlayerPrefs.DeleteAll(): xóa TOÀN BỘ PlayerPrefs của project, kể cả khóa của package khác mà fixture không sao lưu. Chỉ chạy khi chọn riêng ca này.")]
    public IEnumerator TC_WIN_8a_CreepyJesterChayHet_XoaDuLieuVaThua()
    {
        var jester = Cards.Asset("CreepyJester");
        PlayerPrefs.SetInt(SentinelKey, 1);
        UseFixedFrameRate(10);
        yield return null;
        Cards.Place(jester);

        yield return Frames(Mathf.CeilToInt(JesterSeconds(jester) * 10) + 5);

        Assert.IsFalse(PlayerPrefs.HasKey(SentinelKey), "PlayerPrefs.DeleteAll() phải được gọi");
        Assert.IsTrue(Game.IsGameOver);
        Assert.AreEqual(EndReason.CreepyJesterLoss, Game.LastEndReason);
    }

    [UnityTest]
    public IEnumerator TC_WIN_8b_CreepyJesterPawnGiuaChung_DungMoiDeDoa()
    {
        var jester = Cards.Asset("CreepyJester");
        PlayerPrefs.SetInt(SentinelKey, 1);
        UseFixedFrameRate(10);
        yield return null;
        var card = Cards.Place(jester);
        // Test chạy khoảng 1,5 lần JesterSeconds (~90 s game). Hóa đơn thuế đầu tiên hết hạn ở giây 75 và phạt 40 s Clock,
        // nên với Clock mặc định ván tự thua (ClockLoss). Nâng Clock lên (vẫn dưới mốc thắng 600) để chỉ kiểm thử Jester.
        TestHooks.SetClock(500f);

        yield return Frames(Mathf.CeilToInt(JesterSeconds(jester) * 10 / 2));   // nửa chừng
        Pawning.PawnOnto(card);
        yield return Frames(Mathf.CeilToInt(JesterSeconds(jester) * 10) + 5);    // quá thời điểm lẽ ra kết thúc

        Assert.IsTrue(PlayerPrefs.HasKey(SentinelKey), "Không được xóa dữ liệu");
        Assert.IsFalse(Game.IsGameOver);
        PlayerPrefs.DeleteKey(SentinelKey);
    }
}
