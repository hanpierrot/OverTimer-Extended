using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Overtime.Blackjack;

/// <summary>
/// Ending Charlie (DESIGN §5.14 D): một ván bị "rig" để các lá người chơi rút đầu tiên là lá 2 hoặc 3, nhờ đó có thể
/// rút tới blackjackCharlieCards lá mà không bust. BlackjackGame là C# thuần nên kiểm thử ở Edit Mode.
/// </summary>
public class BlackjackCharlieTests
{
    private const int CharlieCards = 7;

    private static Card C(int rank, Suit suit = Suit.Spades) => new Card(rank, suit);

    private static int RiggedLeft(BlackjackGame game)
    {
        var field = typeof(BlackjackGame).GetField("_rigged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "BlackjackGame không còn hàng đợi _rigged (mã game đã đổi?)");
        return ((Queue<Card>)field.GetValue(game)).Count;
    }

    // ---------- Ván rig ----------

    [Test]
    public void VanRig_MoiLaNguoiChoiRutDeuLa2Hoac3_VaKhongBustTrongBayLa()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var game = new BlackjackGame(new System.Random(seed));
            game.Deal(CharlieCards);

            Assert.AreEqual(HandState.PlayerTurn, game.State, $"seed {seed}: 2 + 3 không thể là natural");

            while (game.PlayerHand.Count < CharlieCards)
            {
                Assert.IsTrue(game.CanHit, $"seed {seed}: còn phải rút được ở {game.PlayerHand.Count} lá");
                game.Hit();
                Assert.AreNotEqual(Outcome.PlayerBust, game.Result, $"seed {seed}: bust ở {game.PlayerHand.Count} lá");
            }

            foreach (var card in game.PlayerHand)
                Assert.That(card.Rank, Is.EqualTo(2).Or.EqualTo(3), $"seed {seed}: lá {card} không phải 2 hoặc 3");
            Assert.IsTrue(game.HasCharlie(CharlieCards), $"seed {seed}: 7 lá không bust phải là Charlie");
        }
    }

    [Test]
    public void VanRig_ConLai5LaRigSauKhiChia2La()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var game = new BlackjackGame(new System.Random(seed));
            game.Deal(CharlieCards);
            Assert.AreEqual(2, game.DealerHand.Count);
            // 8 lá 2 và 3 trong bộ bài: 7 lá dành cho người chơi, 1 lá được trả lại bộ bài ở vị trí ngẫu nhiên.
            Assert.AreEqual(CharlieCards - 2, RiggedLeft(game), $"seed {seed}: còn lại 5 lá rig sau khi chia 2 lá");
        }
    }

    [Test]
    public void VanThuong_KhongRig_GiongHetDealKhongThamSo()
    {
        var a = new BlackjackGame(new System.Random(77));
        var b = new BlackjackGame(new System.Random(77));
        a.Deal();
        b.Deal(0);

        CollectionAssert.AreEqual(a.PlayerHand, b.PlayerHand);
        CollectionAssert.AreEqual(a.DealerHand, b.DealerHand);
        Assert.AreEqual(0, RiggedLeft(b));
    }

    [Test]
    public void DoubleDown_XoaRig_LaThuBaLayTuBoBaiThuong()
    {
        var game = new BlackjackGame(new System.Random(5));
        game.Deal(CharlieCards);
        Assert.AreEqual(CharlieCards - 2, RiggedLeft(game));

        game.DoubleDown();

        Assert.AreEqual(0, RiggedLeft(game), "Double không được rút từ hàng rig");
        Assert.AreEqual(3, game.PlayerHand.Count);
    }

    // ---------- HasCharlie ----------

    [Test]
    public void HasCharlie_BayLaKhongBust_Dung()
    {
        // Người chơi: 2♠ 2♥ rồi rút 2♦ 2♣ 3♠ 3♥ 3♦ = 7 lá, tổng 17. Nhà cái 10 + 7 = 17.
        var game = new BlackjackGame(new RiggedRandom(
            C(2, Suit.Spades), C(10, Suit.Spades), C(2, Suit.Hearts), C(7, Suit.Spades),
            C(2, Suit.Diamonds), C(2, Suit.Clubs), C(3, Suit.Spades), C(3, Suit.Hearts), C(3, Suit.Diamonds)));
        game.Deal();

        for (int i = 0; i < 4; i++) game.Hit();
        Assert.IsFalse(game.HasCharlie(CharlieCards), "6 lá chưa đủ");

        game.Hit();
        Assert.AreEqual(CharlieCards, game.PlayerHand.Count);
        Assert.AreEqual(17, BlackjackGame.Value(game.PlayerHand));
        Assert.IsTrue(game.HasCharlie(CharlieCards));
    }

    [Test]
    public void HasCharlie_DuSoLaNhungBust_Sai()
    {
        // Người chơi 10♠ 6♥ rút K♥ = 26: bust, dù đủ 3 lá.
        var game = new BlackjackGame(new RiggedRandom(
            C(10, Suit.Spades), C(5, Suit.Spades), C(6, Suit.Hearts), C(7, Suit.Spades), C(13, Suit.Hearts)));
        game.Deal();

        game.Hit();

        Assert.AreEqual(Outcome.PlayerBust, game.Result);
        Assert.IsFalse(game.HasCharlie(3), "Bust thì không phải Charlie");
    }
}
