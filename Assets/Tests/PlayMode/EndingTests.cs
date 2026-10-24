using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Overtime.Blackjack;
using TMPro;
using UnityEngine.TestTools;

/// <summary>
/// Ending Seven-Card Charlie và việc BlackjackApp trả thưởng trước khi lật bài (N14), DESIGN §5.14 D và §5.5.
/// Công tắc ép "ván kế là Charlie" nằm ở CheatFlags (chỉ hoạt động trong Editor và bản dev).
/// </summary>
public class CharlieEndingTests : GameSceneTest
{
    private BlackjackApp _app;

    [UnitySetUp]
    public IEnumerator OpenBlackjack()
    {
        UseFixedFrameRate(50);
        _app = TestHooks.Find<BlackjackApp>();
        yield return OpenLaptopApp(_app);
    }

    private BlackjackGame Game => TestHooks.GetField<BlackjackGame>(_app, "_game");
    private static TMP_Text HintLabel => TestHooks.GetField<TMP_Text>(HintService.Instance, "label");

    [Test]
    public void VanRigBangCheat_RutDuLaThiThangBangCharlie()
    {
        CheatFlags.ForceCharlie = true;
        TestHooks.SetClock(100f);
        HintLabel.text = "";

        TestHooks.ClickButton(_app, "dealButton");
        Assert.AreEqual(HandState.PlayerTurn, Game.State, "Ván rig toàn lá 2 và 3, không thể là natural");

        for (int i = 0; i < 10 && !GameManager.Instance.IsGameOver; i++)
            TestHooks.ClickButton(_app, "hitButton");

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.BlackjackCharlieWin, GameManager.Instance.LastEndReason);
        Assert.AreEqual(Config.blackjackCharlieCards, Game.PlayerHand.Count);
        Assert.AreEqual(HintCatalog.Get(HintId.CharlieTease), HintLabel.text, "Từ lá thứ 4 mỗi lần rút có câu gợi ý");
    }

    [Test]
    public void VanKhongRig_RutBayLaKhongThang()
    {
        // Không có công tắc và Random trả 0,99 nên không trúng 4%: ván thường, thắng/thua theo luật bình thường.
        TestHooks.SetAutoProperty(RngService.Instance, "Random", new FixedRandom(0, 0.99));
        TestHooks.SetClock(100f);

        TestHooks.ClickButton(_app, "dealButton");

        Assert.IsFalse(GameManager.Instance.IsGameOver, "Không được thắng ngay sau khi chia");
        Assert.LessOrEqual(Game.PlayerHand.Count, 2);
    }

    [Test]
    public void Natural_TraTienNgayLucChia_DongAppKhongMatThuong()
    {
        // Tìm seed chia ra natural cho người chơi (và nhà cái không natural).
        int seed = 0;
        while (true)
        {
            var probe = new BlackjackGame(new System.Random(seed));
            probe.Deal();
            if (probe.Result == Outcome.PlayerNatural) break;
            seed++;
            Assert.Less(seed, 5000, "Không tìm được seed natural");
        }
        TestHooks.SetField(_app, "_game", new BlackjackGame(new System.Random(seed)));
        TestHooks.SetAutoProperty(RngService.Instance, "Random", new FixedRandom(0, 0.99));   // không trúng 4% ván rig
        TestHooks.SetMoney(0);
        TestHooks.SetClock(100f);

        TestHooks.ClickButton(_app, "dealButton");

        // Thưởng được cộng trước khoảng chờ lật bài (blackjackRevealDelay), không đợi hoạt ảnh.
        Assert.AreEqual(Config.blackjackNaturalPayout, MoneyService.Instance.Current, "Natural trả ngay lúc chia (Hot Table chưa có chuỗi)");

        Laptop.CloseCurrentApp();
        Assert.AreEqual(Config.blackjackNaturalPayout, MoneyService.Instance.Current, "Đóng app lúc đang lật bài không làm mất thưởng");
    }
}

/// <summary>
/// Ending Mother Lode (DESIGN §5.14 D): <c>miningStreakTarget</c> vein dài tối đa liên tiếp. Vein trượt (Butterfingers),
/// vein ngắn hơn hoặc rời app đều làm mất chuỗi. Mọi ca chạy đồng bộ: coroutine hồi vein không chạy giữa chừng.
/// </summary>
public class MotherLodeTests : GameSceneTest
{
    private MiningApp _app;

    [UnitySetUp]
    public IEnumerator OpenMining()
    {
        UseFixedFrameRate(50);
        _app = TestHooks.Find<MiningApp>();
        yield return OpenLaptopApp(_app);
    }

    private List<BlockTypeDefinition> Vein => TestHooks.GetField<List<BlockTypeDefinition>>(_app, "_vein");
    private int BlockIndex => TestHooks.GetField<int>(_app, "_blockIndex");
    private int Streak => TestHooks.GetField<int>(_app, "_fullVeinStreak");
    private int Target => Config.miningStreakTarget;

    /// <summary>
    /// Sinh vein với độ dài định trước. <paramref name="boostSample"/> là giá trị NextDouble: nhỏ hơn
    /// miningFullVeinStreakChance thì vein sau một vein đầy được nâng lên độ dài tối đa, lớn hơn thì dài ngẫu nhiên 6–9.
    /// </summary>
    private void StartVein(int length, double boostSample)
    {
        TestHooks.SetAutoProperty(RngService.Instance, "Random", new FixedRandom(length, boostSample));
        TestHooks.Call(_app, "StartVein");
        TestHooks.SeedRng(TestSeed);
        Assert.AreEqual(length, Vein.Count);
    }

    private void MineWholeVein()
    {
        int length = Vein.Count;
        for (int i = 0; i < 500 && BlockIndex < length && !GameManager.Instance.IsGameOver; i++)
            TestHooks.Call(_app, "OnHit");
    }

    private void FullVein()
    {
        StartVein(Config.veinLengthMax, -1.0);
        MineWholeVein();
    }

    private void ShortVein()
    {
        StartVein(Config.veinLengthMin, 0.99);
        MineWholeVein();
    }

    [Test]
    public void DuSoVeinDayLienTiep_Thang()
    {
        for (int i = 0; i < Target - 1; i++)
        {
            FullVein();
            Assert.IsFalse(GameManager.Instance.IsGameOver, $"Vein đầy thứ {i + 1}/{Target}: chưa thắng");
        }
        Assert.AreEqual(Target - 1, Streak);

        FullVein();

        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual(EndReason.MotherLodeWin, GameManager.Instance.LastEndReason);
    }

    [Test]
    public void VeinNganHonChenGiua_PhaChuoi()
    {
        for (int i = 0; i < Target - 1; i++) FullVein();
        ShortVein();
        Assert.AreEqual(0, Streak, "Vein ngắn hơn tối đa phá chuỗi");

        for (int i = 0; i < Target - 1; i++) FullVein();
        Assert.IsFalse(GameManager.Instance.IsGameOver, "Chuỗi đếm lại từ đầu");

        FullVein();
        Assert.AreEqual(EndReason.MotherLodeWin, GameManager.Instance.LastEndReason);
    }

    [Test]
    public void VeinTruot_PhaChuoi_DuLaVeinDay()
    {
        for (int i = 0; i < Target - 1; i++) FullVein();
        MiningApp.QueueNextVeinSlip();                          // hiệu ứng thẻ Butterfingers

        FullVein();

        Assert.AreEqual(0, Streak, "Vein trượt không được tính vào chuỗi");
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    [Test]
    public void RoiApp_MatChuoi()
    {
        for (int i = 0; i < Target - 1; i++) FullVein();
        Assert.AreEqual(Target - 1, Streak);

        Laptop.CloseCurrentApp();

        Assert.AreEqual(0, Streak);
    }

    [Test]
    public void SapDuChuoi_CoGoiYMotLodeTease()
    {
        TestHooks.GetField<TMP_Text>(HintService.Instance, "label").text = "";

        for (int i = 0; i < Target - 1; i++) FullVein();

        Assert.AreEqual(HintCatalog.Get(HintId.MotherLodeTease), TestHooks.GetField<TMP_Text>(HintService.Instance, "label").text);
    }
}
