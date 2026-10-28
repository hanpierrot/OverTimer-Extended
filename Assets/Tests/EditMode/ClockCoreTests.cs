using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Mục 4.2.4 của kế hoạch kiểm thử: lõi Clock (PauseLock, SpeedModifiers). Hai lớp là C# thuần nên kiểm thử Edit Mode.
/// Kết quả mong đợi theo DESIGN §2 (I2) và §5.4: tạm dừng đếm theo người giữ, tốc độ là tích các hệ số, kẹp trong [0; 3].
/// TC-CLC-7 – 9 cần ClockService chạy trong scene nên nằm ở Play Mode.
/// </summary>
public class ClockCoreTests
{
    private const float Eps = 1e-5f;

    // ---------- PauseLock ----------

    [Test]
    public void TC_CLC_1_CungNguoiGiuHaiLan_ChiTinhMotLan()
    {
        var pl = new PauseLock();
        var events = new List<bool>();
        pl.PausedChanged += events.Add;
        object a = "A";

        Assert.IsTrue(pl.Acquire(a), "Lần giữ đầu làm Clock tạm dừng");
        Assert.IsFalse(pl.Acquire(a), "Giữ lần hai bằng cùng người không đổi gì");

        Assert.AreEqual(1, pl.HolderCount);
        Assert.IsTrue(pl.IsPaused);
        CollectionAssert.AreEqual(new[] { true }, events, "PausedChanged chỉ phát một lần");
    }

    [Test]
    public void TC_CLC_2_HaiNguoiGiuChongNhau_ChiChayLaiKhiNguoiCuoiNha()
    {
        var pl = new PauseLock();
        var events = new List<bool>();
        pl.PausedChanged += events.Add;
        object call = "cuộc gọi", cutscene = "đoạn phim";

        pl.Acquire(call);
        pl.Acquire(cutscene);
        pl.Release(call);
        Assert.IsTrue(pl.IsPaused, "Còn đoạn phim giữ nên Clock vẫn dừng");
        CollectionAssert.AreEqual(new[] { true }, events, "Nhả người đầu không phát sự kiện");

        Assert.IsTrue(pl.Release(cutscene));
        Assert.IsFalse(pl.IsPaused);
        CollectionAssert.AreEqual(new[] { true, false }, events);
    }

    [Test]
    public void TC_CLC_3_NhaNguoiKhongGiuHoacNull_KhongCoTacDung()
    {
        var pl = new PauseLock();
        pl.Acquire("A");

        Assert.IsFalse(pl.Release("C"));
        Assert.IsFalse(pl.Release(null));
        Assert.IsTrue(pl.IsPaused, "A vẫn đang giữ");
        Assert.Throws<ArgumentNullException>(() => pl.Acquire(null));
    }

    // ---------- SpeedModifiers ----------

    [Test]
    public void TC_CLC_4_TocDoLaTichCacHeSo()
    {
        var sm = new SpeedModifiers();
        sm.Set("phone", 1.5f);
        sm.Set("snail", 0.75f);

        Assert.AreEqual(1.125f, sm.Effective, Eps, "Phone reo ×1,5 và Snail Speed ×0,75 nhân với nhau");
    }

    [Test]
    public void TC_CLC_5a_TichVuotBa_BiKepVeBa()
    {
        var sm = new SpeedModifiers();
        sm.Set("a", 1.5f);
        sm.Set("b", 1.5f);
        sm.Set("c", 1.5f);

        Assert.AreEqual(SpeedModifiers.Max, sm.Effective, Eps, "1,5³ = 3,375 bị kẹp về 3");
    }

    [Test]
    public void TC_CLC_5b_TimeScarabBangKhong_DongBangClock()
    {
        var sm = new SpeedModifiers();
        sm.Set("scarab", 0f);
        sm.Set("phone", 1.5f);

        Assert.AreEqual(0f, sm.Effective, "Hệ số 0 dừng Clock dù có hệ số khác");
    }

    [Test]
    public void TC_CLC_6_DatLaiCungKhoa_XoaKhoaLa_GiaTriSai()
    {
        var sm = new SpeedModifiers();
        sm.Set("k", 1.5f);
        sm.Set("k", 0.75f);
        Assert.AreEqual(0.75f, sm.Effective, Eps, "Cùng khóa thì thay giá trị, không nhân dồn");
        Assert.AreEqual(1, sm.Count);

        Assert.DoesNotThrow(() => sm.Remove("không có"));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.Set("k", -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.Set("k", float.NaN));
        Assert.AreEqual(0.75f, sm.Effective, Eps, "Giá trị sai bị từ chối, giá trị cũ giữ nguyên");
    }
}
