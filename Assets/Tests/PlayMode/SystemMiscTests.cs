using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Overtime.Blackjack;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Kế hoạch kiểm thử (Play Mode): thống kê các kết thúc cơ chế TC-ALT-5, 9, 12; điểm RunRating TC-ALT-13;
/// trạng thái được đặt lại khi chơi lại TC-GAM-11, 12; âm thanh TC-FBK-1, 6.
/// Ca thống kê gọi đúng hàm của game (ScratchTicket.RollSymbols, MiningApp.StartVein, BlackjackApp.OnDeal) với seed cố định
/// và so với xác suất trong GameConfig theo ngưỡng 3σ (mục 4.1.4); không ghim số cân bằng.
/// </summary>
public class SystemMiscTests : GameSceneTest
{
    private const int Fps = 50;

    [UnitySetUp]
    public IEnumerator FixTime()
    {
        UseFixedFrameRate(Fps);
        TestHooks.SetClock(500f);
        TestHooks.Find<CatController>().gameObject.SetActive(false);   // Cat có thể ngồi chặn vật test đang nhấp
        yield return null;
    }

    private static double Sigma3(double p, int n) => 3 * Math.Sqrt(p * (1 - p) / n);

    private static TicketStack Stack(TicketTierKind tier) =>
        UnityEngine.Object.FindObjectsByType<TicketStack>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => TestHooks.GetField<ScratchTicket[]>(s, "ticketPool")[0].TierKind == tier);

    private static ScratchTicket BuyOne(TicketTierKind tier)
    {
        var stack = Stack(tier);
        TestHooks.SetMoney(1000);
        stack.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);
        return TestHooks.GetField<ScratchTicket[]>(stack, "ticketPool").First(t => t.gameObject.activeSelf);
    }

    // ---------- TC-ALT-5 ----------

    [Test]
    public void TC_ALT_5_TyLeJackpotCurse()
    {
        GiveItem("lucky_stack");
        var nine = BuyOne(TicketTierKind.LuckyNine);
        var triple = BuyOne(TicketTierKind.TripleMatch);
        double pJ = Config.luckyNineJackpotChance, pC = Config.luckyNineCurseChance;
        const int n = 100000;

        foreach (int seed in new[] { 1, 2, 3 })
        {
            TestHooks.SeedRng(seed);
            int jackpot = 0, curse = 0, both = 0;
            for (int i = 0; i < n; i++)
            {
                TestHooks.Call(nine, "RollSymbols");
                bool j = TestHooks.GetField<bool>(nine, "_isJackpot"), c = TestHooks.GetField<bool>(nine, "_isCursed");
                if (j) jackpot++;
                if (c) curse++;
                if (j && c) both++;
            }
            Assert.That(jackpot / (double)n, Is.EqualTo(pJ).Within(Sigma3(pJ, n)), $"seed {seed}: Jackpot");
            Assert.That(curse / (double)n, Is.EqualTo(pC).Within(Sigma3(pC, n)), $"seed {seed}: Curse");
            Assert.AreEqual(0, both, "Không vé nào vừa Jackpot vừa Curse");
        }

        for (int i = 0; i < 10000; i++)
        {
            TestHooks.Call(triple, "RollSymbols");
            Assert.IsFalse(TestHooks.GetField<bool>(triple, "_isJackpot") || TestHooks.GetField<bool>(triple, "_isCursed"),
                "Triple Match không bao giờ ra Jackpot/Curse");
        }
    }

    // ---------- TC-ALT-9 ----------

    [UnityTest]
    public IEnumerator TC_ALT_9_XacSuatVeinToiDaKhiCoChuoi()
    {
        var app = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(app);
        int min = Config.veinLengthMin;
        int max = (int)typeof(MiningApp).GetProperty("MaxVeinLength", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(app);
        Assume.That(max, Is.EqualTo(Config.veinLengthMax), "Số khối trong BlockConfig giới hạn độ dài vein");
        const int n = 10000;
        TestHooks.SeedRng(7);

        int full = 0;
        for (int i = 0; i < n; i++)
        {
            TestHooks.SetField(app, "_fullVeinStreak", 1);
            TestHooks.Call(app, "StartVein");
            int len = TestHooks.GetField<List<BlockTypeDefinition>>(app, "_vein").Count;
            if (len == max) full++;
            else Assert.That(len, Is.InRange(min, max - 1), "Các vein còn lại dài 6 … 9");
        }
        double p = Config.miningFullVeinStreakChance;
        Assert.That(full / (double)n, Is.EqualTo(p).Within(Sigma3(p, n)), "Có chuỗi: vein tối đa");

        // Chuỗi = 0: đều trên [min, max].
        var counts = new int[max + 1];
        for (int i = 0; i < n; i++)
        {
            TestHooks.SetField(app, "_fullVeinStreak", 0);
            TestHooks.Call(app, "StartVein");
            counts[TestHooks.GetField<List<BlockTypeDefinition>>(app, "_vein").Count]++;
        }
        double q = 1.0 / (max - min + 1);
        for (int len = min; len <= max; len++)
            Assert.That(counts[len] / (double)n, Is.EqualTo(q).Within(Sigma3(q, n)), $"Chuỗi = 0: độ dài {len}");
    }

    // ---------- TC-ALT-12 ----------

    [UnityTest]
    public IEnumerator TC_ALT_12_TyLeTayCharlie()
    {
        var app = TestHooks.Find<BlackjackApp>();
        yield return OpenLaptopApp(app);
        TestHooks.SeedRng(11);
        const int n = 10000;
        int rigged = 0, normal = 0;
        var firstRank = new int[11];

        for (int i = 0; i < n; i++)
        {
            TestHooks.SetClock(Config.winClock * 0.5f);         // dưới mốc thắng: Spend báo OnTimeChanged và GameManager xét thắng
            var game = new BlackjackGame(RngService.Instance.Random);
            TestHooks.SetField(app, "_game", game);
            TestHooks.Call(app, "OnDeal");
            app.StopAllCoroutines();                              // tay Natural tự mở coroutine Resolve; bỏ để chia tay kế ngay
            TestHooks.SetField(app, "_resolving", false);
            Assert.AreNotEqual(HandState.Idle, game.State,
                $"Đã chia bài (ván {i}, banned {app.Banned}, resolving {TestHooks.GetField<bool>(app, "_resolving")}, over {GameManager.Instance.IsGameOver}, clock {ClockService.Instance.CurrentSeconds})");
            bool isRig = TestHooks.GetField<Queue<Card>>(game, "_rigged").Count > 0;
            if (isRig) rigged++;
            else { normal++; firstRank[game.PlayerHand[0].BaseValue]++; }
        }
        double p = Config.blackjackCharlieChance;
        Assert.That(rigged / (double)n, Is.EqualTo(p).Within(Sigma3(p, n)), "Tay được gian lận");

        // Tay thường: lá đầu theo bộ bài chuẩn (giá trị 1 … 9 mỗi loại 1/13, giá trị 10 là 4/13).
        for (int v = 1; v <= 10; v++)
        {
            double pv = v == 10 ? 4.0 / 13 : 1.0 / 13;
            Assert.That(firstRank[v] / (double)normal, Is.EqualTo(pv).Within(Sigma3(pv, normal)), $"Tay thường: lá đầu giá trị {v}");
        }
    }

    // ---------- TC-ALT-13 ----------

    [Test]
    public void TC_ALT_13_DiemRunRatingKetThucCoChe()
    {
        var c = Config;
        float wSum = c.ratingOutcomeWeight + c.ratingSurvivalWeight + c.ratingMoneyWeight;
        float half = Mathf.Max(1f, c.ratingMoneyHalfPoint);

        var jackpot = new RunRecord { winCondition = nameof(EndReason.TicketJackpotWin), bestTime = 200f, totalMoneyEarned = Mathf.RoundToInt(half) };
        float expectedA = 100f * (c.ratingOutcomeWeight * c.ratingMechanicWin + c.ratingSurvivalWeight * 1f + c.ratingMoneyWeight * 0.5f) / wSum;
        Assert.AreEqual(expectedA, RunRating.Score(jackpot), 0.01f, "(a) thắng cơ chế: sống sót = 1, tiền = mốc nửa → 0,5");

        var curse = new RunRecord { winCondition = nameof(EndReason.TicketCurseLoss), bestTime = 300f, totalMoneyEarned = 0 };
        float expectedB = 100f * (c.ratingOutcomeWeight * c.ratingMechanicLoss + c.ratingSurvivalWeight * Mathf.Clamp01(300f / c.winClock)) / wSum;
        Assert.AreEqual(expectedB, RunRating.Score(curse), 0.01f, "(b) thua cơ chế, Clock cao nhất 300 s, $0");

        Debug.Log($"[TC-ALT-13] (a) {RunRating.Score(jackpot):0.0}  (b) {RunRating.Score(curse):0.0}");
    }

    // ---------- TC-GAM-11 ----------

    [UnityTest]
    public IEnumerator TC_GAM_11_CoGianLanBiXoaKhiChoiLai()
    {
        CheatFlags.NextLuckyNine = CheatFlags.NextTicket.Jackpot;
        CheatFlags.ForceCharlie = true;

        var old = GameManager.Instance;
        GameManager.Instance.Restart();
        while (GameManager.Instance == null || GameManager.Instance == old) yield return null;
        yield return Frames(2);
        TestHooks.SeedRng(TestSeed);

        Assert.AreEqual(CheatFlags.NextTicket.None, CheatFlags.NextLuckyNine, "Cờ vé đã đặt lại");
        Assert.IsFalse(CheatFlags.ForceCharlie, "Cờ Charlie đã đặt lại");

        GiveItem("lucky_stack");
        var ticket = BuyOne(TicketTierKind.LuckyNine);
        Assert.IsFalse(TestHooks.GetField<bool>(ticket, "_isJackpot"), "Vé bình thường (seed cố định không ra Jackpot)");

        var app = TestHooks.Find<BlackjackApp>();
        yield return OpenLaptopApp(app);
        TestHooks.SetClock(500f);
        TestHooks.Call(app, "OnDeal");
        var game = TestHooks.GetField<BlackjackGame>(app, "_game");
        Assert.AreEqual(0, TestHooks.GetField<Queue<Card>>(game, "_rigged").Count, "Tay bài bình thường");
    }

    // ---------- TC-GAM-12 ----------

    [UnityTest]
    public IEnumerator TC_GAM_12_TrangThaiTinhDatLaiKhiChoiLai()
    {
        LaptopLock.Add(new object());
        Cards.Place(Cards.Asset("DisableMining"));
        var shop = ShopService.Instance;
        Assert.IsTrue(shop.AddToCart("power_bank"));
        TestHooks.SetMoney(shop.Quote().total);
        Assert.IsTrue(shop.TryBuy());
        Assert.IsTrue(LaptopLock.IsLocked);
        Assert.IsFalse(shop.IconGate.IsVisible("Mining"));
        Assert.AreEqual(1, shop.Delivery.InTransit.Count);
        // Cable Eater tắt Phone, nên “Laptop khóa bởi Cable Eater” và “cuộc gọi đang reo” không cùng lúc được. Ca này kiểm phần
        // trạng thái tĩnh: thay thẻ bằng một khóa LaptopLock (đúng static mà Cable Eater dùng) để Phone vẫn reo được.
        var phone = PhoneController.Instance;
        phone.Calls.ForceRingNow();
        for (int i = 0; i < 10 && !phone.IsRinging; i++) yield return null;
        Assert.IsTrue(phone.IsRinging, "Phone đang reo");

        var old = GameManager.Instance;
        GameManager.Instance.Restart();
        while (GameManager.Instance == null || GameManager.Instance == old) yield return null;
        yield return Frames(3);

        shop = ShopService.Instance;
        Assert.IsFalse(LaptopLock.IsLocked, "Laptop không khóa");
        Assert.IsFalse(LaptopController.IsLocked);
        Assert.IsTrue(shop.IconGate.IsVisible("Mining"), "Mining hiện như đầu ván");
        Assert.IsFalse(shop.IconGate.IsVisible("Blackjack"), "Blackjack ẩn (chưa có license)");
        Assert.IsFalse(shop.IconGate.IsVisible("Billiards"), "Billiards ẩn (chưa có license)");
        var pending = (HashSet<string>)typeof(DesktopIconView)
            .GetField("s_pendingHidden", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
        Assert.IsFalse(pending.Contains("Mining"), "DesktopIconView không còn giữ Mining ẩn");
        Assert.AreEqual(0, shop.Delivery.InTransit.Count, "Không còn đơn");
        Assert.IsFalse(PhoneController.Instance.IsRinging, "Không còn cuộc gọi");
        Assert.IsTrue(PhoneController.Instance.Power.IsUsable, "Phone bật");
        // MissingReferenceException hay lỗi khác trong Console làm GameSceneTest báo Lỗi ở TearDown.
    }

    // ---------- TC-FBK-1 ----------

    [Test]
    public void TC_FBK_1_PhatAmThanhKhiKhongCoAudioService()
    {
        var service = AudioService.Instance;
        TestHooks.SetStaticAutoProperty<AudioService>("Instance", null);
        try
        {
            Assert.IsNull(AudioService.Instance);
            Assert.DoesNotThrow(() => AudioService.Play(SfxId.Click));
        }
        finally
        {
            TestHooks.SetStaticAutoProperty<AudioService>("Instance", service);
        }
    }

    // ---------- TC-FBK-6 ----------

    [UnityTest]
    public IEnumerator TC_FBK_6_AmThanhKhongLamLechDayNgauNhien()
    {
        var quiet = new List<string>();
        yield return Scenario(quiet, noisy: false);

        yield return SceneReload();
        var noisy = new List<string>();
        yield return Scenario(noisy, noisy: true);

        CollectionAssert.AreEqual(quiet, noisy, "Dãy RngService giống hệt nhau dù có hay không có âm thanh");
    }

    private static IEnumerator SceneReload()
    {
        var old = GameManager.Instance;
        GameManager.Instance.Restart();
        while (GameManager.Instance == null || GameManager.Instance == old) yield return null;
        yield return null;
    }

    /// <summary>60 s game: vé và vein lấy từ RngService; ghi lại mọi kết quả ngẫu nhiên.</summary>
    private IEnumerator Scenario(List<string> log, bool noisy)
    {
        TestHooks.Find<CatController>().gameObject.SetActive(false);
        TestHooks.SetClock(500f);
        var audio = AudioService.Instance;
        if (!noisy) TestHooks.SetStaticAutoProperty<AudioService>("Instance", null);   // như khi không có AudioService (TC-FBK-1)
        TestHooks.SeedRng(TestSeed);
        var mining = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(mining);
        TestHooks.SeedRng(TestSeed);
        // Lịch gọi của Phone chạy theo thời gian thực (unscaledDeltaTime), nên khung reo phụ thuộc tốc độ máy chứ không
        // phụ thuộc RNG; tắt Phone để chỉ so các lần rút RngService chạy theo khung (vé, vein).
        PhoneController.Instance.ForceOff(log);
        var sfx = (SfxId[])Enum.GetValues(typeof(SfxId));

        for (int frame = 0; frame < 60 * Fps; frame++)
        {
            if (noisy) foreach (var id in sfx) AudioService.Play(id);
            if (frame % 250 == 0)
            {
                var pool = TestHooks.GetField<ScratchTicket[]>(Stack(TicketTierKind.TripleMatch), "ticketPool");
                var before = pool.Where(x => x.gameObject.activeSelf).ToList();
                BuyOne(TicketTierKind.TripleMatch);
                var t = pool.Single(x => x.gameObject.activeSelf && !before.Contains(x));
                t.GetComponent<PointerReceiver>().OnClickDown(Vector2.zero);   // mở vé như người chơi: ô cào chỉ khởi tạo khi xem
                Assert.IsTrue(TestHooks.GetField<bool>(t, "_isBeingViewed"), "Vé đã mở");
                log.Add("ticket " + string.Join(",", TestHooks.GetField<int[]>(t, "_activeSymbolIndex")));
                yield return null;
                t.CashOut();
                TestHooks.Call(mining, "StartVein");
                log.Add("vein " + TestHooks.GetField<List<BlockTypeDefinition>>(mining, "_vein").Count);
            }
            yield return null;
        }
        log.Add("next " + RngService.Instance.Random.Next());
        if (!noisy) TestHooks.SetStaticAutoProperty<AudioService>("Instance", audio);
    }
}
