using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Mục 4.3.5 của kế hoạch kiểm thử, TC-BIL-1…6: luật bida trong BilliardsReferee (C# thuần).
/// Mỗi ca dựng ShotReport bằng tay, nên không cần bắn bi thật.
/// </summary>
public class BilliardsRefereeTests
{
    private static BilliardsReferee Referee(string[] order, string[] forbidden = null,
                                            Dictionary<string, Pocket> called = null)
        => new BilliardsReferee(order, forbidden, called, strictFirstContact: true, finalWrongPocketFails: true);

    private static ShotReport Shot(string firstContact, bool cueScratched = false,
                                   params (string ball, Pocket pocket)[] pots)
    {
        var report = new ShotReport { firstContact = firstContact, cueScratched = cueScratched };
        report.pots.AddRange(pots);
        return report;
    }

    // ---------- TC-BIL-1 ----------

    [Test]
    public void TC_BIL_1_PotDungBiDauHang_DuocTinh()
    {
        var referee = Referee(new[] { "1", "2" });

        var result = referee.Judge(Shot("1", false, ("1", Pocket.TR)));

        Assert.IsFalse(result.foul);
        CollectionAssert.AreEqual(new[] { "1" }, result.popped);
        Assert.AreEqual("2", referee.Head);
        Assert.AreEqual(Verdict.Continue, result.verdict);
    }

    // ---------- TC-BIL-2 ----------

    [Test]
    public void TC_BIL_2_ChamSaiBiTruoc_PhamLoiVaDatLaiBi()
    {
        var referee = Referee(new[] { "1", "2" });

        var result = referee.Judge(Shot("2", false, ("1", Pocket.TR)));

        Assert.IsTrue(result.foul);
        CollectionAssert.IsEmpty(result.popped);
        CollectionAssert.Contains(result.respot, "1");
        Assert.AreEqual("1", referee.Head);
        Assert.AreEqual(Verdict.Continue, result.verdict);
    }

    // ---------- TC-BIL-3 ----------

    [Test]
    public void TC_BIL_3a_KhongChamBiNao_PhamLoi()
    {
        var referee = Referee(new[] { "1", "2" });

        var result = referee.Judge(Shot(null));

        Assert.IsTrue(result.foul);
        Assert.AreEqual(Verdict.Continue, result.verdict);
        Assert.AreEqual("1", referee.Head);
    }

    [Test]
    public void TC_BIL_3b_BiCaiRoiLo_PhamLoiVaDatLaiCaHaiBi()
    {
        var referee = Referee(new[] { "1", "2" });

        var result = referee.Judge(Shot("1", true, ("1", Pocket.TR), ("cue", Pocket.BL)));

        Assert.IsTrue(result.foul);
        CollectionAssert.Contains(result.respot, "cue");
        CollectionAssert.Contains(result.respot, "1");
        CollectionAssert.IsEmpty(result.popped);
        Assert.AreEqual(Verdict.Continue, result.verdict);
    }

    // ---------- TC-BIL-4 ----------

    private static Dictionary<string, Pocket> EightCalledToBR => new Dictionary<string, Pocket> { ["8"] = Pocket.BR };

    [Test]
    public void TC_BIL_4a_BiCuoiRoiSom_Thua()
    {
        var referee = Referee(new[] { "1", "8" }, called: EightCalledToBR);

        var result = referee.Judge(Shot("1", false, ("8", Pocket.BR)));

        Assert.AreEqual(Verdict.Fail, result.verdict);
        Assert.AreEqual("8 dropped too early", result.failReason);
    }

    [Test]
    public void TC_BIL_4b_BiCuoiRoiKhiPhamLoi_Thua()
    {
        var referee = Referee(new[] { "8" }, called: EightCalledToBR);

        var result = referee.Judge(Shot(null, false, ("8", Pocket.BR)));

        Assert.AreEqual(Verdict.Fail, result.verdict);
        Assert.AreEqual("8 dropped on a foul", result.failReason);
    }

    [Test]
    public void TC_BIL_4c_BiCuoiVaoSaiLoDaGoi_Thua()
    {
        var referee = Referee(new[] { "8" }, called: EightCalledToBR);

        var result = referee.Judge(Shot("8", false, ("8", Pocket.TL)));

        Assert.AreEqual(Verdict.Fail, result.verdict);
        Assert.AreEqual("8 went in the wrong pocket", result.failReason);
    }

    [Test]
    public void TC_BIL_4d_BiCuoiVaoDungLo_Thang()
    {
        var referee = Referee(new[] { "8" }, called: EightCalledToBR);

        var result = referee.Judge(Shot("8", false, ("8", Pocket.BR)));

        Assert.AreEqual(Verdict.Win, result.verdict);
        CollectionAssert.AreEqual(new[] { "8" }, result.popped);
    }

    // ---------- TC-BIL-5 ----------

    [Test]
    public void TC_BIL_5_BiCam_ThuaNgay()
    {
        var referee = Referee(new[] { "1", "2" }, forbidden: new[] { "5" });

        var result = referee.Judge(Shot("1", false, ("5", Pocket.TM)));

        Assert.AreEqual(Verdict.Fail, result.verdict);
        Assert.AreEqual("5 is forbidden", result.failReason);
    }

    // ---------- TC-BIL-6 ----------

    [Test]
    public void TC_BIL_6_BiThuongVaoSaiLoDaGoi_DatLaiKhongThua()
    {
        var called = new Dictionary<string, Pocket> { ["1"] = Pocket.TR };
        var referee = Referee(new[] { "1", "8" }, called: called);

        var result = referee.Judge(Shot("1", false, ("1", Pocket.BL)));

        Assert.IsFalse(result.foul);
        Assert.AreEqual(Verdict.Continue, result.verdict);
        CollectionAssert.Contains(result.respot, "1");
        CollectionAssert.IsEmpty(result.popped);
        Assert.AreEqual("1", referee.Head);
    }
}
