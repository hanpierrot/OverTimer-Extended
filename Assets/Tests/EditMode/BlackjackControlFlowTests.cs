using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Overtime.Blackjack;
using UnityEngine;

/// <summary>
/// Kiểm thử hộp trắng theo luồng điều khiển cho chức năng Blackjack (một ván chơi).
/// Phân tích đầy đủ (mã đánh số, đồ thị, C(Vg), đường thi hành) ở Docs/KiemThuLuongDieuKhien_Blackjack/KiemThuLuongDieuKhien_Blackjack.md.
/// Mỗi test là một đường thi hành độc lập; tên test ghi mã ca, chú thích ghi đường đi qua các nút.
/// Giá trị mong đợi lấy theo luật Blackjack và GameConfig, không chép từ mã.
/// RigPlayerCards là hàm private: gọi qua reflection để dựng được bộ bài tùy ý (hộp xám, giống TestHooks).
/// </summary>
public class BlackjackControlFlowTests
{
    private GameConfig _config;

    [OneTimeSetUp]
    public void LoadConfig() => _config = TestAssets.LoadGameConfig();

    private static Card C(int rank, Suit suit = Suit.Spades) => new Card(rank, suit);

    private static int Value(params Card[] cards) => BlackjackGame.Value(new List<Card>(cards));

    /// <summary>Chia ván với thứ tự rút cho trước: P1, D1, P2, D2, rồi các lá rút sau.</summary>
    private static BlackjackGame DealRigged(params Card[] drawOrder)
    {
        var game = new BlackjackGame(new RiggedRandom(drawOrder));
        game.Deal();
        return game;
    }

    private static bool IsLow(Card c) => c.Rank == 2 || c.Rank == 3;

    // ---------- Truy cập phần private của BlackjackGame (chỉ cho RigPlayerCards) ----------

    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private static List<Card> DeckOf(BlackjackGame game)
    {
        var field = typeof(BlackjackGame).GetField("_deck", Private);
        Assert.IsNotNull(field, "Không tìm thấy BlackjackGame._deck (đã đổi tên?)");
        return (List<Card>)field.GetValue(game);
    }

    private static Queue<Card> RiggedOf(BlackjackGame game)
    {
        var field = typeof(BlackjackGame).GetField("_rigged", Private);
        Assert.IsNotNull(field, "Không tìm thấy BlackjackGame._rigged (đã đổi tên?)");
        return (Queue<Card>)field.GetValue(game);
    }

    /// <summary>Đặt bộ bài = deck, gọi RigPlayerCards(count), trả về bộ bài và hàng đợi lá đã xếp sau khi gọi.</summary>
    private static (List<Card> deck, Queue<Card> rigged) Rig(int count, params Card[] deck)
    {
        var game = new BlackjackGame(new System.Random(42));
        var d = DeckOf(game);
        d.Clear();
        d.AddRange(deck);

        var method = typeof(BlackjackGame).GetMethod("RigPlayerCards", Private);
        Assert.IsNotNull(method, "Không tìm thấy BlackjackGame.RigPlayerCards (đã đổi tên?)");
        method.Invoke(game, new object[] { count });

        return (d, RiggedOf(game));
    }

    private static Card[] FullDeck()
    {
        var deck = new List<Card>(52);
        for (int s = 0; s < 4; s++)
        for (int r = 1; r <= 13; r++)
            deck.Add(new Card(r, (Suit)s));
        return deck.ToArray();
    }

    /// <summary>Một lá K cộng với `low` lá thấp đầu tiên trong tám lá 2, 3 của bộ bài.</summary>
    private static Card[] KingPlusLow(int low)
    {
        var lows = FullDeck().Where(IsLow).Take(low);
        return new[] { C(13) }.Concat(lows).ToArray();
    }

    // =====================================================================
    // 1. Value(hand): P = 4, C = 5
    // =====================================================================

    [Test]
    public void TC_BJK_VAL_1_TayRong_Tra0()
    {
        // 1 → 2(F) → 7a(F) → 9
        Assert.AreEqual(0, Value());
    }

    [Test]
    public void TC_BJK_VAL_2_MotLaThuong_TraGiaTriLa()
    {
        // 1 → 2(T) → 3(F) → 5 → 6 → 2(F) → 7a(F) → 9
        Assert.AreEqual(10, Value(C(10)));
    }

    [Test]
    public void TC_BJK_VAL_3_MotLaAt_TinhLa11()
    {
        // 1 → 2(T) → 3(T) → 4 → 6 → 2(F) → 7a(F) → 9
        Assert.AreEqual(11, Value(C(1)));
    }

    [Test]
    public void TC_BJK_VAL_4_QuaKhongCoAt_GiuNguyenTong()
    {
        // 1 → (2(T) → 3(F) → 5 → 6)×3 → 2(F) → 7a(T) → 7b(F) → 9
        Assert.AreEqual(25, Value(C(13), C(12), C(5)));
    }

    [Test]
    public void TC_BJK_VAL_5_HaiLaAt_HaMotAtXuong1()
    {
        // 1 → (2(T) → 3(T) → 4 → 6)×2 → 2(F) → 7a(T) → 7b(T) → 8 → 7a(F) → 9
        Assert.AreEqual(12, Value(C(1), C(1, Suit.Hearts)));
    }

    // Kiểm thử vòng lặp while (số lần hạ Át), n = 4 Át trong bộ bài.

    [Test]
    public void TC_BJK_VAL_LP0_Lap0Lan_A6La17()
    {
        Assert.AreEqual(17, Value(C(1), C(6)));
    }

    [Test]
    public void TC_BJK_VAL_LP1_Lap1Lan_A69La16()
    {
        Assert.AreEqual(16, Value(C(1), C(6), C(9)));
    }

    [Test]
    public void TC_BJK_VAL_LP2_Lap2Lan_AAKLa12()
    {
        Assert.AreEqual(12, Value(C(1), C(1, Suit.Hearts), C(13)));
    }

    [Test]
    public void TC_BJK_VAL_LP3_LapNTru1Lan_AAAKLa13()
    {
        Assert.AreEqual(13, Value(C(1), C(1, Suit.Hearts), C(1, Suit.Clubs), C(13)));
    }

    [Test]
    public void TC_BJK_VAL_LP4_LapNLan_AAAAKLa14()
    {
        Assert.AreEqual(14, Value(C(1), C(1, Suit.Hearts), C(1, Suit.Clubs), C(1, Suit.Diamonds), C(13)));
    }

    // Kiểm thử vòng lặp for (số lá trên tay), liền kề với vòng while: vòng while giữ mặc định (lặp 0 lần, không có Át).
    // Hàm không giới hạn số lá nên không xác định được n: chỉ cần 4 ca 0, 1, 2, k. Lặp 0 = VAL-1, lặp 1 = VAL-2.

    [Test]
    public void TC_BJK_VAL_FOR_L2_Lap2Lan_107La17()
    {
        Assert.AreEqual(17, Value(C(10), C(7)));
    }

    [Test]
    public void TC_BJK_VAL_FOR_Lk_Lap5Lan_23456La20()
    {
        Assert.AreEqual(20, Value(C(2), C(3), C(4), C(5), C(6)));
    }

    // =====================================================================
    // 2. Deal(charlieCards): P = 3, C = 4
    // =====================================================================

    [Test]
    public void TC_BJK_DEAL_1_VanThuong_SangLuotNguoiChoi()
    {
        // 1 → 2(F) → 4 → 5(F) → 10. Người chơi 10 + 7, nhà cái 9 + 8.
        var game = DealRigged(C(10), C(9), C(7), C(8));

        Assert.AreEqual(HandState.PlayerTurn, game.State);
        Assert.AreEqual(Outcome.None, game.Result);
        Assert.AreEqual(2, game.PlayerHand.Count);
        Assert.AreEqual(2, game.DealerHand.Count);
    }

    [Test]
    public void TC_BJK_DEAL_2_VanCharlie_NguoiChoiNhanLaThap()
    {
        // 1 → 2(T) → 3 → 4 → 5(F) → 10. Deal(7): hai lá đầu của người chơi lấy từ hàng đợi lá 2, 3.
        var game = new BlackjackGame(new System.Random(7));
        game.Deal(7);

        Assert.IsTrue(game.PlayerHand.All(IsLow), "Hai lá đầu của ván Charlie phải là 2 hoặc 3");
        Assert.AreEqual(5, RiggedOf(game).Count, "Còn 7 - 2 = 5 lá thấp chờ cho các lần Hit");
        Assert.AreEqual(HandState.PlayerTurn, game.State);
        Assert.AreEqual(Outcome.None, game.Result);
    }

    [Test]
    public void TC_BJK_DEAL_3_NguoiChoiNatural_NhaCaiKhong()
    {
        // 1 → 2(F) → 4 → 5(T) → 6(F) → 8 → 9 → 10. Người chơi A + K, nhà cái 5 + 9.
        var game = DealRigged(C(1), C(5), C(13), C(9));

        Assert.AreEqual(Outcome.PlayerNatural, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    [Test]
    public void TC_BJK_DEAL_4_HaiBenCungNatural_Hoa()
    {
        // 1 → 2(F) → 4 → 5(T) → 6(T) → 7 → 9 → 10. Người chơi A + K, nhà cái A + Q.
        var game = DealRigged(C(1), C(1, Suit.Hearts), C(13), C(12));

        Assert.AreEqual(Outcome.Push, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    // =====================================================================
    // 3. Hit(): P = 3, C = 4
    // =====================================================================

    [Test]
    public void TC_BJK_HIT_1_ChuaChiaVan_KhongRut()
    {
        // 1a(F) → 2 → 6. Ván chưa chia (Idle).
        var game = new BlackjackGame(new System.Random(1));
        game.Hit();

        Assert.AreEqual(0, game.PlayerHand.Count);
        Assert.AreEqual(HandState.Idle, game.State);
    }

    [Test]
    public void TC_BJK_HIT_2_Du21_KhongRutThem()
    {
        // 1a(T) → 1b(F) → 2 → 6. Người chơi 5 + 6, Hit được 10 = 21 (3 lá), Hit lần nữa.
        var game = DealRigged(C(5), C(9), C(6), C(8), C(10));
        game.Hit();
        Assert.AreEqual(21, game.PlayerValue);

        game.Hit();

        Assert.AreEqual(3, game.PlayerHand.Count);
        Assert.AreEqual(HandState.PlayerTurn, game.State);
        Assert.AreEqual(Outcome.None, game.Result);
    }

    [Test]
    public void TC_BJK_HIT_3_RutKhongQua21_VanTiepTuc()
    {
        // 1a(T) → 1b(T) → 3 → 4(F) → 6. Người chơi 10 + 2, Hit được 5 = 17.
        var game = DealRigged(C(10), C(9), C(2), C(8), C(5));
        game.Hit();

        Assert.AreEqual(3, game.PlayerHand.Count);
        Assert.AreEqual(17, game.PlayerValue);
        Assert.AreEqual(HandState.PlayerTurn, game.State);
        Assert.AreEqual(Outcome.None, game.Result);
    }

    [Test]
    public void TC_BJK_HIT_4_RutQua21_ThuaNgay()
    {
        // 1a(T) → 1b(T) → 3 → 4(T) → 5 → 6. Người chơi 10 + 2, Hit được 10 = 22.
        var game = DealRigged(C(10), C(9), C(2), C(8), C(10, Suit.Hearts));
        game.Hit();

        Assert.AreEqual(22, game.PlayerValue);
        Assert.AreEqual(Outcome.PlayerBust, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    // =====================================================================
    // 4. DoubleDown(): P = 3, C = 4
    // =====================================================================

    [Test]
    public void TC_BJK_DBL_1_ChuaChiaVan_KhongGapDoi()
    {
        // 1a(F) → 2 → 7.
        var game = new BlackjackGame(new System.Random(1));
        game.DoubleDown();

        Assert.IsFalse(game.DoubledDown);
        Assert.AreEqual(0, game.PlayerHand.Count);
    }

    [Test]
    public void TC_BJK_DBL_2_Co3La_TuChoi()
    {
        // 1a(T) → 1b(F) → 2 → 7. Người chơi 2 + 3, Hit được 4 (3 lá).
        var game = DealRigged(C(2), C(10), C(3), C(7), C(4), C(5));
        game.Hit();

        game.DoubleDown();

        Assert.IsFalse(game.DoubledDown);
        Assert.AreEqual(3, game.PlayerHand.Count);
        Assert.AreEqual(HandState.PlayerTurn, game.State);
    }

    [Test]
    public void TC_BJK_DBL_3_GapDoiKhongQua21_TuDungVaThang()
    {
        // 1a(T) → 1b(T) → 3 → 4(F) → 6 → 7. Người chơi 5 + 6, Double được 10 = 21; nhà cái 10 + 7.
        var game = DealRigged(C(5), C(10), C(6), C(7), C(10, Suit.Hearts));
        game.DoubleDown();

        Assert.IsTrue(game.DoubledDown);
        Assert.AreEqual(3, game.PlayerHand.Count);
        Assert.AreEqual(Outcome.PlayerWin, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    [Test]
    public void TC_BJK_DBL_4_GapDoiQua21_ThuaNgay()
    {
        // 1a(T) → 1b(T) → 3 → 4(T) → 5 → 7. Người chơi 10 + 6, Double được 10 = 26; nhà cái 10 + 7.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(6), C(7), C(10, Suit.Clubs));
        game.DoubleDown();

        Assert.IsTrue(game.DoubledDown);
        Assert.AreEqual(Outcome.PlayerBust, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
        Assert.AreEqual(2, game.DealerHand.Count, "Thua ngay thì nhà cái không rút thêm");
    }

    // =====================================================================
    // 5. Stand(): P = 5, C = 6
    // =====================================================================

    [Test]
    public void TC_BJK_STD_1_NhaCai18KhongRut_NhaCaiThang()
    {
        // 1(F) → 3 → 4(F) → 6(F) → 8(T) → 9 → 13 → 14. Người chơi 10 + 7, nhà cái 10 + 8.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(7), C(8));
        game.Stand();

        Assert.AreEqual(2, game.DealerHand.Count);
        Assert.AreEqual(Outcome.DealerWin, game.Result);
        Assert.AreEqual(HandState.Resolved, game.State);
    }

    [Test]
    public void TC_BJK_STD_2_ChuaChiaVan_KhongLamGi()
    {
        // 1(T) → 2 → 14.
        var game = new BlackjackGame(new System.Random(1));
        game.Stand();

        Assert.AreEqual(HandState.Idle, game.State);
        Assert.AreEqual(Outcome.None, game.Result);
    }

    [Test]
    public void TC_BJK_STD_3_NhaCaiRutRoiThang()
    {
        // 1(F) → 3 → 4(T) → 5 → 4(F) → 6(F) → 8(T) → 9 → 13 → 14. Người chơi 10 + 7, nhà cái 10 + 6, rút 2 = 18.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(7), C(6), C(2));
        game.Stand();

        Assert.AreEqual(3, game.DealerHand.Count);
        Assert.AreEqual(18, game.DealerValue);
        Assert.AreEqual(Outcome.DealerWin, game.Result);
    }

    [Test]
    public void TC_BJK_STD_4_NhaCaiRutQua21()
    {
        // 1(F) → 3 → 4(T) → 5 → 4(F) → 6(T) → 7 → 13 → 14. Nhà cái 10 + 6, rút 10 = 26.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(7), C(6), C(10, Suit.Clubs));
        game.Stand();

        Assert.AreEqual(26, game.DealerValue);
        Assert.AreEqual(Outcome.DealerBust, game.Result);
    }

    [Test]
    public void TC_BJK_STD_5_NguoiChoiCaoHon_NguoiChoiThang()
    {
        // 1(F) → 3 → 4(F) → 6(F) → 8(F) → 10(T) → 11 → 13 → 14. Người chơi 10 + 9, nhà cái 10 + 8.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(9), C(8));
        game.Stand();

        Assert.AreEqual(Outcome.PlayerWin, game.Result);
    }

    [Test]
    public void TC_BJK_STD_6_BangDiem_Hoa()
    {
        // 1(F) → 3 → 4(F) → 6(F) → 8(F) → 10(F) → 12 → 13 → 14. Cả hai 10 + 8.
        var game = DealRigged(C(10), C(10, Suit.Hearts), C(8), C(8, Suit.Hearts));
        game.Stand();

        Assert.AreEqual(Outcome.Push, game.Result);
    }

    // =====================================================================
    // 6. BlackjackApp.SettleHand(cfg, result, doubled, ref streak): P = 6, C = 7
    // =====================================================================

    private float Bonus(int streak) => Mathf.Min(streak * _config.blackjackStreakStep, _config.blackjackStreakCap);

    [Test]
    public void TC_BJK_SET_1_Thang_KhongGapDoi()
    {
        // 1(F) → 3 → 4a(F) → 4b(T) → 5(F) → 7 → 8 → 11
        int streak = 0;
        int payout = BlackjackApp.SettleHand(_config, Outcome.PlayerWin, false, ref streak);

        Assert.AreEqual(_config.blackjackWinPayout, payout);
        Assert.AreEqual(1, streak);
    }

    [Test]
    public void TC_BJK_SET_2_Thang_CoGapDoi()
    {
        // 1(T) → 2 → 4a(F) → 4b(T) → 5(F) → 7 → 8 → 11
        int streak = 0;
        int payout = BlackjackApp.SettleHand(_config, Outcome.PlayerWin, true, ref streak);

        Assert.AreEqual(_config.blackjackWinPayout * 2, payout);
        Assert.AreEqual(1, streak);
    }

    [Test]
    public void TC_BJK_SET_3_Natural()
    {
        // 1(F) → 3 → 4a(T) → 5(T) → 6 → 8 → 11
        int streak = 0;
        int payout = BlackjackApp.SettleHand(_config, Outcome.PlayerNatural, false, ref streak);

        Assert.AreEqual(_config.blackjackNaturalPayout, payout);
        Assert.AreEqual(1, streak);
    }

    [Test]
    public void TC_BJK_SET_4_NhaCaiQua21_ChuoiThang2_CoThuongHotTable()
    {
        // 1(F) → 3 → 4a(F) → 4b(F) → 4c(T) → 5(F) → 7 → 8 → 11
        int streak = 2;
        int payout = BlackjackApp.SettleHand(_config, Outcome.DealerBust, false, ref streak);

        Assert.AreEqual(Mathf.RoundToInt(_config.blackjackWinPayout * (1f + Bonus(2))), payout);
        Assert.AreEqual(3, streak);
    }

    [Test]
    public void TC_BJK_SET_5_Hoa_GiuNguyenChuoi()
    {
        // 1(F) → 3 → 4a(F) → 4b(F) → 4c(F) → 4d(T) → 9 → 11
        int streak = 2;
        int payout = BlackjackApp.SettleHand(_config, Outcome.Push, false, ref streak);

        Assert.AreEqual(_config.blackjackPushPayout, payout);
        Assert.AreEqual(2, streak);
    }

    [Test]
    public void TC_BJK_SET_6_NhaCaiThang_MatChuoi()
    {
        // 1(F) → 3 → 4a(F) → 4b(F) → 4c(F) → 4d(F) → 10 → 11
        int streak = 3;
        int payout = BlackjackApp.SettleHand(_config, Outcome.DealerWin, false, ref streak);

        Assert.AreEqual(0, payout);
        Assert.AreEqual(0, streak);
    }

    [Test]
    public void TC_BJK_SET_7_Hoa_CoGapDoi()
    {
        // 1(T) → 2 → 4a(F) → 4b(F) → 4c(F) → 4d(T) → 9 → 11
        int streak = 0;
        int payout = BlackjackApp.SettleHand(_config, Outcome.Push, true, ref streak);

        Assert.AreEqual(_config.blackjackPushPayout * 2, payout);
        Assert.AreEqual(0, streak);
    }

    // =====================================================================
    // 7. RigPlayerCards(count): P = 6, C = 7
    // =====================================================================

    [Test]
    public void TC_BJK_RIG_1_BoBaiRong()
    {
        // 1 → 2(F) → 6 → 7(F) → 9 → 10(F) → 15
        var (deck, rigged) = Rig(1);

        Assert.AreEqual(0, rigged.Count);
        Assert.AreEqual(0, deck.Count);
    }

    [Test]
    public void TC_BJK_RIG_2_ChiCoLaCao_KhongXep()
    {
        // 1 → 2(T) → 3a(F) → 3b(F) → 5 → 2(F) → 6 → 7(F) → 9 → 10(F) → 15
        var (deck, rigged) = Rig(1, C(13));

        Assert.AreEqual(0, rigged.Count);
        CollectionAssert.AreEqual(new[] { C(13) }, deck);
    }

    [Test]
    public void TC_BJK_RIG_3_MotLa2_DuocXep()
    {
        // 1 → 2(T) → 3a(T) → 4 → 5 → 2(F) → 6 → 7(F) → 9 → 10(T) → 11(T) → 12 → 14 → 10(F) → 15
        var (deck, rigged) = Rig(1, C(2));

        CollectionAssert.AreEqual(new[] { C(2) }, rigged);
        Assert.AreEqual(0, deck.Count);
    }

    [Test]
    public void TC_BJK_RIG_4_MotLa3_DuocXep()
    {
        // 1 → 2(T) → 3a(F) → 3b(T) → 4 → 5 → 2(F) → 6 → 7(F) → 9 → 10(T) → 11(T) → 12 → 14 → 10(F) → 15
        var (deck, rigged) = Rig(1, C(3));

        CollectionAssert.AreEqual(new[] { C(3) }, rigged);
        Assert.AreEqual(0, deck.Count);
    }

    [Test]
    public void TC_BJK_RIG_5_HaiLaThap_XaoRoiXepCa2()
    {
        // ... → 6 → 7(T) → 8 → 7(F) → 9 → (10(T) → 11(T) → 12 → 14)×2 → 10(F) → 15
        var (deck, rigged) = Rig(2, C(2), C(3));

        CollectionAssert.AreEquivalent(new[] { C(2), C(3) }, rigged);
        Assert.AreEqual(0, deck.Count);
    }

    [Test]
    public void TC_BJK_RIG_6_Count0_TraLaVeBoBai()
    {
        // ... → 9 → 10(T) → 11(F) → 13 → 14 → 10(F) → 15
        var (deck, rigged) = Rig(0, C(2));

        Assert.AreEqual(0, rigged.Count);
        CollectionAssert.AreEqual(new[] { C(2) }, deck);
    }

    [Test]
    public void TC_BJK_RIG_7_BoBaiDay_Count7_NhuKhiChoi()
    {
        // Đường dài nhất: cả ba vòng lặp chạy nhiều lần; nút 11 đi cả T (7 lần) lẫn F (1 lần).
        var (deck, rigged) = Rig(7, FullDeck());

        Assert.AreEqual(7, rigged.Count);
        Assert.IsTrue(rigged.All(IsLow), "Mọi lá được xếp phải là 2 hoặc 3");
        Assert.AreEqual(45, deck.Count, "52 - 7 lá đã xếp");
        Assert.AreEqual(1, deck.Count(IsLow), "Lá thấp thứ 8 được trả lại bộ bài");
    }

    // Kiểm thử vòng lặp 10 (số lá thấp trong bộ bài), n = 8 lá 2 và 3; count = 7 như trong game.

    [TestCase(0, 0, 1, TestName = "TC_BJK_RIG_LP0_Lap0Lan")]
    [TestCase(1, 1, 1, TestName = "TC_BJK_RIG_LP1_Lap1Lan")]
    [TestCase(2, 2, 1, TestName = "TC_BJK_RIG_LP2_Lap2Lan")]
    [TestCase(4, 4, 1, TestName = "TC_BJK_RIG_LPk_Lap4Lan")]
    [TestCase(7, 7, 1, TestName = "TC_BJK_RIG_LP7_LapNTru1Lan")]
    [TestCase(8, 7, 2, TestName = "TC_BJK_RIG_LP8_LapNLan")]
    public void TC_BJK_RIG_LP_SoLaThap(int lowCards, int expectedRigged, int expectedDeck)
    {
        var (deck, rigged) = Rig(7, KingPlusLow(lowCards));

        Assert.AreEqual(expectedRigged, rigged.Count);
        Assert.AreEqual(expectedDeck, deck.Count);
    }
}
