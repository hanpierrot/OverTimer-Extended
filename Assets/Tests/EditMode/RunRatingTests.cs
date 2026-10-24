using System;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Mười kết thúc của một ván (EndReason). Bốn ending cơ chế (Jackpot, Curse, Mother Lode, Charlie) được thêm sau:
/// enum chỉ được thêm vào cuối vì EndReason được lưu theo tên trong lịch sử ván (PROJECT.md §7.2).
/// </summary>
public class RunRatingTests
{
    private static readonly EndReason[] Mechanic =
    {
        EndReason.TicketJackpotWin, EndReason.MotherLodeWin, EndReason.BlackjackCharlieWin,
    };

    private static EndReason[] All => (EndReason[])Enum.GetValues(typeof(EndReason));

    [Test]
    public void EndReason_ThuTuCu_KhongDoi_BonGiaTriMoiNamCuoi()
    {
        Assert.AreEqual(0, (int)EndReason.ClockWin);
        Assert.AreEqual(1, (int)EndReason.SetCompletionWin);
        Assert.AreEqual(2, (int)EndReason.FinalCountdownWin);
        Assert.AreEqual(3, (int)EndReason.ClockLoss);
        Assert.AreEqual(4, (int)EndReason.FinalCountdownLoss);
        Assert.AreEqual(5, (int)EndReason.CreepyJesterLoss);
        Assert.AreEqual(6, (int)EndReason.TicketJackpotWin);
        Assert.AreEqual(7, (int)EndReason.TicketCurseLoss);
        Assert.AreEqual(8, (int)EndReason.MotherLodeWin);
        Assert.AreEqual(9, (int)EndReason.BlackjackCharlieWin);
        Assert.AreEqual(10, All.Length, "Có đúng 10 kết thúc; thêm mới phải cập nhật test này và DESIGN §5.14");
    }

    [Test]
    public void IsMechanicWin_ChiBaEndingThang_CoCheDo()
    {
        foreach (var reason in All)
            Assert.AreEqual(Mechanic.Contains(reason), RunRating.IsMechanicWin(reason), reason.ToString());
    }

    [Test]
    public void IsWin_BaEndingCoCheDoThangVaCurseThua()
    {
        foreach (var reason in Mechanic) Assert.IsTrue(RunRating.IsWin(reason), $"{reason} là thắng");
        Assert.IsFalse(RunRating.IsWin(EndReason.TicketCurseLoss), "Curse là thua");
    }

    [Test]
    public void MoiEndingCoMotTieuDeRieng_TruClockWinPhuThuocConfig()
    {
        // Title(ClockWin) đọc GameConfig từ GameManager nên không gọi được ở Edit Mode.
        var titles = All.Where(r => r != EndReason.ClockWin).Select(RunRating.Title).ToArray();

        foreach (var t in titles) Assert.IsNotEmpty(t, "Ending không có tiêu đề trên màn hình kết thúc");
        CollectionAssert.AllItemsAreUnique(titles);
    }

    [Test]
    public void TryParseReason_ChapNhanTenCuaMoiEnding()
    {
        foreach (var reason in All)
        {
            Assert.IsTrue(RunRating.TryParseReason(reason.ToString(), out var parsed), reason.ToString());
            Assert.AreEqual(reason, parsed);
        }
    }
}
