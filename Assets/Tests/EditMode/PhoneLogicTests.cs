using System;
using System.Collections.Generic;
using NUnit.Framework;
using Random = System.Random;   // các lớp thuần nhận System.Random (RngService); không dùng UnityEngine.Random

/// <summary>
/// Mục 4.5.6 của kế hoạch kiểm thử: phần logic thuần của Phone (PhonePower, CallScheduler, CallerRoller, CallDialogue, Inbox).
/// DESIGN §5.9; trọng số người gọi và cách xử lý cuộc gọi khi máy tắt theo code (đã chốt, Phụ lục B: B1, B2).
/// PhoneConfig dựng tay với các giá trị của GameConfig nên test không phụ thuộc asset.
/// TC-PHN-18 (banner) không viết vì logic banner sẽ bị bỏ (B4).
/// </summary>
public class PhoneLogicTests
{
    private static PhoneConfig Config() => new PhoneConfig
    {
        weightTelemarketer = 25, weightClient = 25, weightUnknown = 40, weightITRS = 10,
        unknownSpamChance = 0.6f,
        ringIntervalMin = 30f, ringIntervalMax = 60f,
        maxRingDuration = 10f, callMinGap = 12f,
    };

    /// <summary>Tick đến khi máy reo; trả về số giây đã chờ.</summary>
    private static float TickUntilRinging(CallScheduler cs, float step = 0.05f, float limit = 200f)
    {
        float t = 0f;
        while (cs.Current != CallScheduler.State.Ringing && t < limit) { cs.Tick(step); t += step; }
        Assert.AreEqual(CallScheduler.State.Ringing, cs.Current, $"Không reo sau {limit} s");
        return t;
    }

    /// <summary>Sai lệch cho phép của ca thống kê (kế hoạch 4.1.4): 3·√(p(1 − p)/N).</summary>
    private static void AssertRate(double expected, int count, int n, string what)
    {
        double got = count / (double)n;
        double tol = 3 * Math.Sqrt(expected * (1 - expected) / n);
        Assert.AreEqual(expected, got, tol, $"{what}: {got:P2}, mong đợi {expected:P2} ± {tol:P2}");
    }

    // ---------- PhonePower ----------

    [Test]
    public void TC_PHN_1_BatTatVaKhoiDongNamGiay()
    {
        var pw = new PhonePower(5f);
        int changed = 0;
        pw.Changed += _ => changed++;

        pw.Press();
        Assert.AreEqual(PhonePower.State.Off, pw.Current);
        pw.Press();
        Assert.AreEqual(PhonePower.State.Booting, pw.Current);
        pw.Tick(4.5f);
        Assert.AreEqual(PhonePower.State.Booting, pw.Current, "Còn 0,5 s mới xong");
        pw.Tick(0.5f);
        Assert.AreEqual(PhonePower.State.On, pw.Current);
        Assert.IsTrue(pw.IsUsable);
        Assert.AreEqual(3, changed, "Off → Booting → On");
    }

    [Test]
    public void TC_PHN_2_KhoiDongChiDemThoiGianGame()
    {
        var pw = new PhonePower(5f);
        pw.Press();
        pw.Press();
        float progress = pw.BootProgress;

        for (int i = 0; i < 10; i++) pw.Tick(0f);
        pw.Tick(-1f);

        Assert.AreEqual(PhonePower.State.Booting, pw.Current);
        Assert.AreEqual(progress, pw.BootProgress, "Clock dừng (gameDelta 0) thì không tiến");
    }

    [Test]
    public void TC_PHN_3_BiTatCuongBuc_HaiNguoiGiu()
    {
        var pw = new PhonePower(5f);
        pw.Press();
        pw.Press();                                   // đang Booting
        object a = "Cable Eater 1", b = "Cable Eater 2";

        pw.ForceOff(a);
        pw.ForceOff(b);
        Assert.AreEqual(PhonePower.State.Off, pw.Current, "Tắt cưỡng bức hủy lượt khởi động");
        Assert.IsFalse(pw.Press(), "Nút nguồn không hoạt động");

        pw.ReleaseForceOff(a);
        Assert.IsFalse(pw.Press(), "Còn B giữ thì nút vẫn không hoạt động");

        pw.ReleaseForceOff(b);
        Assert.AreEqual(PhonePower.State.Off, pw.Current, "Nhả hết thì máy vẫn tắt (B3: theo code)");
        Assert.IsTrue(pw.Press());
        Assert.AreEqual(PhonePower.State.Booting, pw.Current);
    }

    // ---------- CallScheduler ----------

    [Test]
    public void TC_PHN_4_ChuKyReo_Nho_Nghi()
    {
        var cs = new CallScheduler(Config(), new Random(1));
        var ends = new List<CallEnd>();
        cs.RingStopped += (_, how) => ends.Add(how);

        float wait = TickUntilRinging(cs);
        Assert.That(wait, Is.InRange(30f - 0.05f, 60f + 0.05f), "Khoảng chờ đầu trong [30; 60] s");

        cs.Tick(10f);
        CollectionAssert.AreEqual(new[] { CallEnd.Missed }, ends, "Không trả lời 10 s thì thành cuộc nhỡ");
        Assert.AreEqual(CallScheduler.State.Gap, cs.Current);

        cs.Tick(11.5f);
        Assert.AreEqual(CallScheduler.State.Gap, cs.Current, "Chưa đủ 12 s nghỉ");
        cs.Tick(0.5f);
        Assert.AreEqual(CallScheduler.State.Waiting, cs.Current, "Đủ 12 s thì chờ cuộc gọi mới");

        float next = TickUntilRinging(cs);
        Assert.That(next, Is.InRange(30f - 0.05f, 60f + 0.05f), "Khoảng chờ mới cũng trong [30; 60] s");
    }

    [Test]
    public void TC_PHN_5_CuocGoiDenKhiMayTat_GhiNho_KhongXepHang()
    {
        var cs = new CallScheduler(Config(), new Random(2)) { CanRing = () => false };
        int rang = 0, missed = 0;
        cs.RingStarted += _ => rang++;
        cs.RingStopped += (_, how) => { if (how == CallEnd.Missed) missed++; };

        for (int i = 0; i < 1300; i++) cs.Tick(0.05f);   // 65 s: chắc chắn qua thời điểm gọi đầu tiên

        Assert.AreEqual(0, rang, "Máy tắt thì không reo");
        Assert.GreaterOrEqual(missed, 1, "Cuộc đến hạn được ghi là nhỡ");
        Assert.AreEqual(CallScheduler.State.Waiting, cs.Current, "Lên lịch cuộc mới, không giữ cuộc cũ");

        cs.CanRing = () => true;
        cs.Tick(0.05f);
        Assert.AreNotEqual(CallScheduler.State.Ringing, cs.Current, "Bật máy không làm cuộc gọi cũ reo ngay");
    }

    [Test]
    public void TC_PHN_6_ThaoTacSaiTrangThai_KhongCoTacDung()
    {
        var cs = new CallScheduler(Config(), new Random(3));

        Assert.IsFalse(cs.Answer(), "Waiting: không trả lời được");
        Assert.IsFalse(cs.Decline(), "Waiting: không từ chối được");
        cs.EndCall();
        Assert.AreEqual(CallScheduler.State.Waiting, cs.Current);

        TickUntilRinging(cs);
        cs.EndCall();
        Assert.AreEqual(CallScheduler.State.Ringing, cs.Current, "EndCall chỉ có tác dụng khi đang trong cuộc gọi");
        Assert.IsTrue(cs.Decline());
        Assert.AreEqual(CallScheduler.State.Gap, cs.Current);

        cs.Tick(12f);
        TickUntilRinging(cs);
        Assert.IsTrue(cs.Answer());
        Assert.AreEqual(CallScheduler.State.InCall, cs.Current);
        Assert.IsFalse(cs.Answer(), "InCall: không trả lời lần hai");
        Assert.IsFalse(cs.Decline(), "InCall: không từ chối được");
        cs.EndCall();
        Assert.AreEqual(CallScheduler.State.Gap, cs.Current);
    }

    [Test]
    public void TC_PHN_7_Robocalls_KhoangChoTrongMuoiLamDenBaMuoiGiay()
    {
        var cs = new CallScheduler(Config(), new Random(4)) { IntervalScale = 0.5f };
        TickUntilRinging(cs);                         // khoảng chờ đầu được tính trong constructor, trước khi đặt scale

        const float step = 0.05f;
        for (int i = 0; i < 1000; i++)
        {
            cs.Decline();
            cs.Tick(12f);                             // hết khoảng nghỉ, chuyển Waiting với khoảng chờ mới
            Assert.AreEqual(CallScheduler.State.Waiting, cs.Current, "Khoảng nghỉ vẫn là 12 s");
            float wait = TickUntilRinging(cs, step);
            Assert.That(wait, Is.InRange(15f - step, 30f + step), $"Lần {i}: khoảng chờ {wait:F2} s");
        }
    }

    // ---------- CallerRoller ----------

    [Test]
    public void TC_PHN_8a_PhanBoNguoiGoi_KhiCoHoaDon()
    {
        const int n = 100000;
        var rng = new Random(5);
        var cfg = Config();
        var shown = new Dictionary<CallerKind, int>();
        var truth = new Dictionary<CallTruth, int>();
        foreach (CallerKind k in Enum.GetValues(typeof(CallerKind))) shown[k] = 0;
        foreach (CallTruth t in Enum.GetValues(typeof(CallTruth))) truth[t] = 0;

        for (int i = 0; i < n; i++)
        {
            var c = CallerRoller.Roll(rng, cfg, true);
            shown[c.shownAs]++;
            truth[c.truth]++;
        }

        AssertRate(0.25, shown[CallerKind.Telemarketer], n, "Hiển thị Telemarketer");
        AssertRate(0.25, shown[CallerKind.Client], n, "Hiển thị Client");
        AssertRate(0.40, shown[CallerKind.Unknown], n, "Hiển thị Unknown");
        AssertRate(0.10, shown[CallerKind.ITRS], n, "Hiển thị ITRS");
        AssertRate(0.49, truth[CallTruth.Telemarketer], n, "Thật là telemarketer (25 + 40 × 0,6)");
        AssertRate(0.41, truth[CallTruth.Client], n, "Thật là client (25 + 40 × 0,4)");
        AssertRate(0.10, truth[CallTruth.ITRS], n, "Thật là ITRS");
    }

    [Test]
    public void TC_PHN_8b_PhanBoNguoiGoi_KhiKhongCoHoaDon()
    {
        const int n = 100000;
        var rng = new Random(6);
        var cfg = Config();
        int t = 0, c = 0, u = 0, itrs = 0, realTele = 0;

        for (int i = 0; i < n; i++)
        {
            var call = CallerRoller.Roll(rng, cfg, false);
            switch (call.shownAs)
            {
                case CallerKind.Telemarketer: t++; break;
                case CallerKind.Client: c++; break;
                case CallerKind.Unknown: u++; break;
                default: itrs++; break;
            }
            if (call.truth == CallTruth.Telemarketer) realTele++;
        }

        Assert.AreEqual(0, itrs, "Không có hóa đơn thì ITRS không gọi");
        AssertRate(25 / 90.0, t, n, "Hiển thị Telemarketer");
        AssertRate(25 / 90.0, c, n, "Hiển thị Client");
        AssertRate(40 / 90.0, u, n, "Hiển thị Unknown");
        AssertRate(49 / 90.0, realTele, n, "Thật là telemarketer");
    }

    // ---------- CallDialogue, Inbox ----------

    [Test]
    public void TC_PHN_9_DemCauThoai()
    {
        var d = new CallDialogue(3);
        Assert.IsTrue(d.Advance());
        Assert.IsFalse(d.IsFinal, "Câu 2/3 chưa phải câu cuối");
        Assert.IsTrue(d.Advance());
        Assert.IsTrue(d.IsFinal, "Câu 3/3 là câu cuối");
        Assert.IsFalse(d.Advance(), "Không đi qua câu cuối");
        Assert.AreEqual(2, d.Index);

        Assert.IsTrue(new CallDialogue(0).IsFinal, "Kịch bản rỗng coi như một câu");
    }

    [Test]
    public void TC_PHN_17_Inbox_MoiNhatODau_DemChuaDoc()
    {
        var inbox = new Inbox();
        inbox.Add(MessageKind.System, "m1", "");
        inbox.Add(MessageKind.System, "m2", "");
        inbox.Add(MessageKind.System, "m3", "");

        inbox.MarkRead(inbox.All[1]);

        Assert.AreEqual("m3", inbox.All[0].title, "Tin mới nhất ở đầu");
        Assert.AreEqual(2, inbox.Unread);
        Assert.Less(inbox.All[2].id, inbox.All[1].id);
        Assert.Less(inbox.All[1].id, inbox.All[0].id, "id tăng dần theo thứ tự thêm");
    }
}
