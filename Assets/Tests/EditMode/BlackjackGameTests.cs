using System.Collections.Generic;
using NUnit.Framework;
using Overtime.Blackjack;
using UnityEditor;

/// <summary>
/// Mục 4.3.2 của kế hoạch kiểm thử. BlackjackGame là logic C# thuần nên chạy được ở Edit Mode.
/// Các ca cần điều khiển lá bài dùng RiggedRandom (bộ bài xếp sẵn).
/// TC-BJK-7, 8 (Play Mode) và TC-BJK-10 ([Dự kiến]) không nằm ở đây.
/// </summary>
public class BlackjackGameTests
{
    private const string GameConfigPath = "Assets/Settings/Configs/GameConfig.asset";

    private GameConfig _config;

    [OneTimeSetUp]
    public void LoadConfig()
    {
        _config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
        Assert.IsNotNull(_config, $"Không tìm thấy GameConfig ở {GameConfigPath}");
    }

    private static Card C(int rank, Suit suit = Suit.Spades) => new Card(rank, suit);

    /// <summary>Chia ván với thứ tự rút cho trước: P1, D1, P2, D2, rồi các lá rút sau.</summary>
    private static BlackjackGame DealRigged(params Card[] drawOrder)
    {
        var game = new BlackjackGame(new RiggedRandom(drawOrder));
        game.Deal();
        return game;
    }

    private int PayoutFromConfig(BlackjackGame game) =>
        game.Payout(_config.blackjackWinPayout, _config.blackjackPushPayout, _config.blackjackNaturalPayout);

    // ---------- TC-BJK-1 ----------

    [Test]
    public void TC_BJK_1_AtMem_A6La17Soft_RutThem9La16()
    {
        var hand = new List<Card> { C(1), C(6) };

        Assert.AreEqual(17, BlackjackGame.Value(hand));
        Assert.IsTrue(BlackjackGame.IsSoft(hand));

        hand.Add(C(9));

        Assert.AreEqual(16, BlackjackGame.Value(hand));
        Assert.IsFalse(BlackjackGame.IsSoft(hand));
    }

    // ---------- TC-BJK-2 ----------

    [Test]
    public void TC_BJK_2_Natural_KetThucNgayVaTraThuongNatural()
    {
        // Người chơi A + K, nhà cái 5 + 9.
        var game = DealRigged(C(1), C(5), C(13), C(9));

        Assert.AreEqual(HandState.Resolved, game.State);
        Assert.AreEqual(Outcome.PlayerNatural, game.Result);
        Assert.AreEqual(_config.blackjackNaturalPayout, PayoutFromConfig(game));
    }

    // ---------- TC-BJK-3 ----------

    [Test]
    public void TC_BJK_3a_Tay12RutLa10_Tong22ThuaNgay()
    {
        // Người chơi 10 + 2, nhà cái 9 + 8, Hit được 10.
        var game = DealRigged(C(10), C(9), C(2), C(8), C(10, Suit.Hearts));

        game.Hit();

        Assert.AreEqual(22, game.PlayerValue);
        Assert.AreEqual(Outcome.PlayerBust, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    [Test]
    public void TC_BJK_3b_Tay11RutLa10_Tong21ChuaThua()
    {
        // Người chơi 5 + 6, nhà cái 9 + 8, Hit được 10.
        var game = DealRigged(C(5), C(9), C(6), C(8), C(10));

        game.Hit();

        Assert.AreEqual(21, game.PlayerValue);
        Assert.AreEqual(Outcome.None, game.Result);
        Assert.AreEqual(HandState.PlayerTurn, game.State);
        Assert.IsFalse(game.CanHit, "Đã 21 thì không được Hit thêm, chỉ còn Stand");
    }

    // ---------- TC-BJK-4 ----------

    [Test]
    public void TC_BJK_4a_NhaCai16_RutThem()
    {
        // Người chơi 10 + 8, nhà cái 10 + 6, lá kế tiếp 2.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(8), C(6), C(2));

        game.Stand();

        Assert.AreEqual(3, game.DealerHand.Count);
        Assert.AreEqual(18, game.DealerValue);
    }

    [Test]
    public void TC_BJK_4b_NhaCai17Cung_Dung()
    {
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(8), C(7), C(2));

        game.Stand();

        Assert.AreEqual(2, game.DealerHand.Count);
        Assert.AreEqual(17, game.DealerValue);
    }

    [Test]
    public void TC_BJK_4c_NhaCaiSoft17_Dung()
    {
        // Nhà cái A + 6 (soft 17).
        var game = DealRigged(C(10), C(1), C(8), C(6), C(2));

        game.Stand();

        Assert.AreEqual(2, game.DealerHand.Count);
        Assert.AreEqual(17, game.DealerValue);
    }

    // ---------- TC-BJK-5 ----------

    [Test]
    public void TC_BJK_5_Double_KhiCo3La_BiTuChoi()
    {
        // Người chơi 2 + 3, Hit được 4 (3 lá, tổng 9), nhà cái 10 + 7.
        var game = DealRigged(C(2), C(10), C(3), C(7), C(4), C(5));

        game.Hit();
        Assert.AreEqual(3, game.PlayerHand.Count);

        Assert.IsFalse(game.CanDouble);
        game.DoubleDown();

        Assert.AreEqual(3, game.PlayerHand.Count);
        Assert.IsFalse(game.DoubledDown);
        Assert.AreEqual(HandState.PlayerTurn, game.State);
    }

    // ---------- TC-BJK-6 ----------

    [Test]
    public void TC_BJK_6_BangThuong_Thang()
    {
        // Người chơi 10 + 9 = 19, nhà cái 10 + 8 = 18.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(9), C(8));
        game.Stand();

        Assert.AreEqual(Outcome.PlayerWin, game.Result);
        Assert.AreEqual(_config.blackjackWinPayout, PayoutFromConfig(game));
    }

    [Test]
    public void TC_BJK_6_BangThuong_Hoa()
    {
        // Cả hai 10 + 8 = 18.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(8), C(8, Suit.Hearts));
        game.Stand();

        Assert.AreEqual(Outcome.Push, game.Result);
        Assert.AreEqual(_config.blackjackPushPayout, PayoutFromConfig(game));
    }

    [Test]
    public void TC_BJK_6_BangThuong_Natural()
    {
        var game = DealRigged(C(1), C(5), C(13), C(9));

        Assert.AreEqual(Outcome.PlayerNatural, game.Result);
        Assert.AreEqual(_config.blackjackNaturalPayout, PayoutFromConfig(game));
    }

    [Test]
    public void TC_BJK_6_BangThuong_DoubleThang()
    {
        // Người chơi 5 + 6, Double được 10 = 21; nhà cái 10 + 7 = 17.
        var game = DealRigged(C(5), C(10), C(6), C(7), C(10, Suit.Hearts));

        game.DoubleDown();

        Assert.IsTrue(game.DoubledDown);
        Assert.AreEqual(Outcome.PlayerWin, game.Result);
        Assert.AreEqual(_config.blackjackWinPayout * 2, PayoutFromConfig(game));
    }

    [Test]
    public void BangThuong_GiaTriTrongGameConfigKhopKeHoach()
    {
        // Kế hoạch ghi $40 / $20 / $50 / $80. Nếu ai đó đổi GameConfig thì ca này báo để cập nhật tài liệu.
        Assert.AreEqual(40, _config.blackjackWinPayout);
        Assert.AreEqual(20, _config.blackjackPushPayout);
        Assert.AreEqual(50, _config.blackjackNaturalPayout);
    }

    // ---------- TC-BJK-9 ----------

    [Test]
    public void TC_BJK_9_CungSeed_ThuTuBaiGiongNhau()
    {
        var a = new BlackjackGame(new System.Random(12345));
        var b = new BlackjackGame(new System.Random(12345));

        a.Deal();
        b.Deal();

        CollectionAssert.AreEqual(a.PlayerHand, b.PlayerHand);
        CollectionAssert.AreEqual(a.DealerHand, b.DealerHand);
    }
}
