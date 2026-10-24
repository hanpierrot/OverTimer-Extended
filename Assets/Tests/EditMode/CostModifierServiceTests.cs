using NUnit.Framework;
using UnityEngine;

/// <summary>
/// CostModifierService có khóa (lỗi N4): mỗi thẻ đăng ký hệ số riêng của mình và hệ số cuối cùng là tích của chúng,
/// nên hai thẻ cùng đích không còn ghi đè nhau và thẻ hết hạn trước không xóa hệ số của thẻ còn lại.
/// Ở Edit Mode Awake không chạy nên Instance không bị chạm tới.
/// </summary>
public class CostModifierServiceTests
{
    private GameObject _go;
    private CostModifierService _service;
    private readonly object _cardA = new object();
    private readonly object _cardB = new object();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("CostModifierService (test)");
        _service = _go.AddComponent<CostModifierService>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_go);

    private const CostModifierService.Target Ticket = CostModifierService.Target.Ticket;

    [Test]
    public void MacDinh_Bang1()
    {
        foreach (CostModifierService.Target target in System.Enum.GetValues(typeof(CostModifierService.Target)))
            Assert.AreEqual(1f, _service.GetMultiplier(target), $"{target}");
    }

    [Test]
    public void MotThe_DatHeSo()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);

        Assert.AreEqual(1.5f, _service.GetMultiplier(Ticket));
    }

    [Test]
    public void HaiTheCungDich_NhanVoiNhau()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);
        _service.SetMultiplier(Ticket, _cardB, 1.5f);

        Assert.AreEqual(2.25f, _service.GetMultiplier(Ticket), 1e-6f, "Không ghi đè: 1,5 × 1,5");
    }

    [Test]
    public void TheHetHanTruoc_KhongXoaHeSoCuaTheCon()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);
        _service.SetMultiplier(Ticket, _cardB, 2f);

        _service.ClearMultiplier(Ticket, _cardA);
        Assert.AreEqual(2f, _service.GetMultiplier(Ticket), "Còn lại hệ số của thẻ B (lỗi N4 cũ: reset về 1)");

        _service.ClearMultiplier(Ticket, _cardB);
        Assert.AreEqual(1f, _service.GetMultiplier(Ticket));
    }

    [Test]
    public void CungKhoa_DatLaiThiThayGiaTri_KhongNhanDoi()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);
        _service.SetMultiplier(Ticket, _cardA, 1.5f);

        Assert.AreEqual(1.5f, _service.GetMultiplier(Ticket));

        _service.SetMultiplier(Ticket, _cardA, 2f);
        Assert.AreEqual(2f, _service.GetMultiplier(Ticket));
    }

    [Test]
    public void XoaKhoaKhongCo_KhongLamGiVaKhongNem()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);

        Assert.DoesNotThrow(() => _service.ClearMultiplier(Ticket, _cardB));
        Assert.DoesNotThrow(() => _service.ClearMultiplier(CostModifierService.Target.Blackjack, _cardA));
        Assert.DoesNotThrow(() => _service.ClearMultiplier(Ticket, null));
        Assert.AreEqual(1.5f, _service.GetMultiplier(Ticket));
    }

    [Test]
    public void CacDichDocLapVoiNhau()
    {
        _service.SetMultiplier(Ticket, _cardA, 1.5f);
        _service.SetMultiplier(CostModifierService.Target.CardPack, _cardA, 3f);

        Assert.AreEqual(1.5f, _service.GetMultiplier(Ticket));
        Assert.AreEqual(3f, _service.GetMultiplier(CostModifierService.Target.CardPack));
        Assert.AreEqual(1f, _service.GetMultiplier(CostModifierService.Target.Billiards));
    }

    [Test]
    public void KhoaNull_BiBoQua()
    {
        _service.SetMultiplier(Ticket, null, 5f);

        Assert.AreEqual(1f, _service.GetMultiplier(Ticket));
    }
}
