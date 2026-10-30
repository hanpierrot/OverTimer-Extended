using NUnit.Framework;

/// <summary>
/// Mục 4.5.7 của kế hoạch kiểm thử: hạn trả một hóa đơn ITRS (TaxDeadline, C# thuần).
/// DESIGN §5.8: trả trong taxPayWindow (30 s Clock chạy); quá hạn thì phạt một lần; không có nút Ignore.
/// Thời gian dùng giá trị biểu diễn chính xác trong float (29,5 + 0,5) để biên 30 s không bị sai số làm tròn.
/// </summary>
public class TaxDeadlineTests
{
    [Test]
    public void TC_TAX_1_HanTra_QuaBaMuoiGiayThiBiPhatMotLan()
    {
        var td = new TaxDeadline(30, 30f);
        int resolved = 0;
        td.Resolved += _ => resolved++;

        td.Tick(29.5f);
        Assert.AreEqual(TaxDeadline.Outcome.Pending, td.State, "Còn 0,5 s thì chưa bị phạt");
        Assert.AreEqual(0.5f, td.SecondsLeft, 1e-5f);

        td.Tick(0.5f);
        Assert.AreEqual(TaxDeadline.Outcome.Penalized, td.State);
        Assert.AreEqual(0f, td.SecondsLeft);
        Assert.AreEqual(1, resolved, "Resolved phát đúng một lần");
    }

    [Test]
    public void TC_TAX_2_HoaDonDaXuLy_KhongThayDoiNua()
    {
        var td = new TaxDeadline(30, 30f);
        int resolved = 0;
        td.Resolved += _ => resolved++;
        td.Tick(30f);

        td.MarkPaid();
        td.Extend(15f);
        td.Double();
        td.Tick(1f);

        Assert.AreEqual(TaxDeadline.Outcome.Penalized, td.State);
        Assert.AreEqual(30, td.Amount, "Không nhân đôi sau khi đã phạt");
        Assert.AreEqual(0f, td.SecondsLeft, "Không gia hạn sau khi đã phạt");
        Assert.AreEqual(1, resolved, "Không xử lý lần hai");
    }

    [Test]
    public void TC_TAX_3_GiaHanVaNhanDoi()
    {
        var td = new TaxDeadline(30, 30f);
        td.Tick(10f);

        td.Extend(15f);
        td.Double();

        Assert.AreEqual(35f, td.SecondsLeft, 1e-5f, "Cuộc gọi ITRS cộng 15 s: 20 + 15");
        Assert.AreEqual(60, td.Amount, "Thẻ Tax Increase nhân đôi hóa đơn đang mở");
        Assert.AreEqual(TaxDeadline.Outcome.Pending, td.State);
    }
}
