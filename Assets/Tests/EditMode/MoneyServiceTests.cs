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

    // ---------- OnIncome (Reward Popup, DESIGN §5.17) ----------

    [Test]
    public void OnIncome_Add_PhatDungMotLanVoiSoTienVaLyDo()
    {
        int count = 0, lastAmount = 0; string lastReason = null;
        _money.OnIncome += (amount, reason) => { count++; lastAmount = amount; lastReason = reason; };

        _money.Add(12, "mining vein");

        Assert.AreEqual(1, count);
        Assert.AreEqual(12, lastAmount);
        Assert.AreEqual("mining vein", lastReason);
    }

    [Test]
    public void OnIncome_HoanTienCuoc_KhongPhat_NhungSoDuVanTang()
    {
        int incomeCount = 0;
        _money.OnIncome += (_, __) => incomeCount++;
        ListenToChanges();

        _money.Add(10, MoneyService.StakeRefundReason);

        Assert.AreEqual(0, incomeCount, "Tiền cược trả lại không phải thu nhập: không hiện popup");
        Assert.AreEqual(1, _eventCount, "OnMoneyChanged vẫn phát để UI cập nhật số dư");
        Assert.AreEqual(10, _money.Current);
    }

    [Test]
    public void OnIncome_TrySpendVaSoTienKhongDuong_KhongPhat()
    {
        GiveMoney(40);
        int incomeCount = 0;
        _money.OnIncome += (_, __) => incomeCount++;

        _money.TrySpend(10, "test");
        _money.Add(0, "test");
        _money.Add(-5, "test");

        Assert.AreEqual(0, incomeCount);
    }
}
