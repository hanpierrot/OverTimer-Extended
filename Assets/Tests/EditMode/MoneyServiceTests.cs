using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Mục 4.2.1 của kế hoạch kiểm thử. TC-MON-1 (số dư ban đầu) nằm ở Play Mode vì
/// MoneyService gán số dư trong Start() từ GameManager.
/// Ở Edit Mode, Unity không gọi Awake/Start, nên số dư khởi đầu là 0 và được đặt bằng Add().
/// </summary>
public class MoneyServiceTests
{
    private GameObject _go;
    private MoneyService _money;

    private int _eventCount;
    private int _lastEventValue;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("MoneyService (test)");
        _money = _go.AddComponent<MoneyService>();
        _eventCount = 0;
        _lastEventValue = -1;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    private void GiveMoney(int amount)
    {
        if (amount > 0) _money.Add(amount, "test setup");
    }

    private void ListenToChanges()
    {
        _money.OnMoneyChanged += (value, _) =>
        {
            _eventCount++;
            _lastEventValue = value;
        };
    }

    [Test]
    public void TC_MON_2_TrySpend_DuTien_TraVeTrueVaTruTien()
    {
        GiveMoney(40);

        bool ok = _money.TrySpend(25, "test");

        Assert.IsTrue(ok);
        Assert.AreEqual(15, _money.Current);
    }

    [Test]
    public void TC_MON_3_TrySpend_DungBangSoDu_VeKhong()
    {
        GiveMoney(25);

        bool ok = _money.TrySpend(25, "test");

        Assert.IsTrue(ok);
        Assert.AreEqual(0, _money.Current);
    }

    [Test]
    public void TC_MON_4_TrySpend_ThieuMotDonVi_BiTuChoi()
    {
        GiveMoney(24);

        bool ok = _money.TrySpend(25, "test");

        Assert.IsFalse(ok);
        Assert.AreEqual(24, _money.Current);
    }

    [Test]
    public void TC_MON_5_Add_PhatOnMoneyChangedDungMotLan()
    {
        GiveMoney(40);
        ListenToChanges();

        _money.Add(10, "test");

        Assert.AreEqual(1, _eventCount);
        Assert.AreEqual(50, _lastEventValue);
        Assert.AreEqual(50, _money.Current);
    }

    [Test]
    public void TC_MON_6_TrySpend_Khong_KhongDoiSoDuVaKhongPhatSuKien()
    {
        GiveMoney(40);
        ListenToChanges();

        bool ok = _money.TrySpend(0, "test");

        // Code hiện tại trả về false; kế hoạch ghi [Xác nhận SRS] cho giá trị trả về.
        Assert.IsFalse(ok);
        Assert.AreEqual(40, _money.Current);
        Assert.AreEqual(0, _eventCount);
    }

    [Test]
    public void TC_MON_7_SoTienAm_BiTuChoi()
    {
        GiveMoney(40);
        ListenToChanges();

        bool ok = _money.TrySpend(-5, "test");
        _money.Add(-5, "test");

        Assert.IsFalse(ok);
        Assert.AreEqual(40, _money.Current);
        Assert.AreEqual(0, _eventCount);
    }
}
