using System.Collections.Generic;
using NUnit.Framework;
using Random = System.Random;   // JunkMailTimer nhận System.Random (RngService)

/// <summary>
/// Mục 4.5.8 của kế hoạch kiểm thử: cửa, bưu kiện và thư (DoorDepot, LetterPile, JunkMailTimer, C# thuần).
/// DESIGN §5.7: tối đa doorParcelSlots (4) bưu kiện, mở sau parcelClicksToOpen (3) lần nhấp; thư tối đa letterSlots (8);
/// thư rác mỗi 60 – 120 s, không có trong 60 s đầu.
/// </summary>
public class DoorMailTests
{
    [Test]
    public void TC_DOR_1_ThapBuuKienDay_BuuKienSauPhaiCho()
    {
        var depot = new DoorDepot(4, 3);
        int added = 0, opened = 0;
        depot.Added += _ => added++;
        depot.Opened += _ => opened++;

        var parcels = new List<Parcel>();
        for (int i = 1; i <= 5; i++) parcels.Add(depot.Receive(i, new[] { "item" + i }));

        Assert.AreEqual(4, depot.Tower.Count);
        Assert.AreEqual(1, depot.Waiting);
        Assert.AreEqual(4, added);

        Assert.IsFalse(depot.Click(parcels[0]));
        Assert.IsFalse(depot.Click(parcels[0]));
        Assert.IsTrue(depot.Click(parcels[0]), "Lần nhấp thứ 3 mở bưu kiện");

        Assert.AreEqual(1, opened);
        Assert.AreEqual(4, depot.Tower.Count, "Bưu kiện đang chờ vào thế chỗ");
        Assert.AreEqual(0, depot.Waiting);
        Assert.AreEqual(5, added);
        Assert.AreSame(parcels[4], depot.Tower[3]);
    }

    [Test]
    public void TC_DOR_2_NhapBuuKienKhongCoTrongThap()
    {
        var depot = new DoorDepot(1, 3);
        var inTower = depot.Receive(1, new[] { "a" });
        var waiting = depot.Receive(2, new[] { "b" });

        Assert.IsFalse(depot.Click(null));
        Assert.IsFalse(depot.Click(waiting), "Bưu kiện đang chờ chưa nhấp được");
        Assert.AreEqual(3, inTower.clicksLeft, "Bưu kiện trong tháp không bị trừ lượt nhấp");
        Assert.AreEqual(3, waiting.clicksLeft);
    }

    [Test]
    public void TC_DOR_3a_DongThuDay_DayThuRacCuNhat()
    {
        var pile = new LetterPile(8);
        var removed = new List<Letter>();
        pile.Removed += removed.Add;

        var oldestJunk = pile.Add(LetterKind.Junk, "j1");
        for (int i = 2; i <= 7; i++) pile.Add(LetterKind.Junk, "j" + i);
        var tax = pile.Add(LetterKind.Tax, "");

        var newest = pile.Add(LetterKind.Junk, "j8");

        Assert.IsNotNull(newest);
        CollectionAssert.AreEqual(new[] { oldestJunk }, removed, "Thư rác cũ nhất bị đẩy ra");
        CollectionAssert.Contains(pile.Letters, tax, "Thư thuế vẫn còn");
        Assert.AreEqual(8, pile.Letters.Count);
    }

    [Test]
    public void TC_DOR_3b_DongThuDayToanThuThue_TuChoiThuMoi()
    {
        var pile = new LetterPile(8);
        for (int i = 0; i < 8; i++) pile.Add(LetterKind.Tax, "");

        Assert.IsNull(pile.Add(LetterKind.Junk, "x"));
        Assert.IsNull(pile.Add(LetterKind.Tax, ""));
        Assert.AreEqual(8, pile.Letters.Count);
    }

    [Test]
    public void TC_DOR_4_LichThuRac()
    {
        const float step = 0.5f;
        for (int seed = 0; seed < 1000; seed++)
        {
            var timer = new JunkMailTimer(new Random(seed), 60f, 120f, 60f);
            Assert.IsFalse(timer.Tick(0f), "Clock dừng thì không có thư");

            float first = TickUntilDue(timer, step);
            Assert.That(first, Is.InRange(60f, 120f + step), $"seed {seed}: thư đầu ở {first} s");

            float next = TickUntilDue(timer, step);
            Assert.That(next, Is.InRange(60f - step, 120f + step), $"seed {seed}: thư kế sau {next} s");
        }
    }

    private static float TickUntilDue(JunkMailTimer timer, float step)
    {
        float t = 0f;
        while (t < 500f)
        {
            t += step;
            if (timer.Tick(step)) return t;
        }
        Assert.Fail("Không có thư rác sau 500 s");
        return t;
    }
}
