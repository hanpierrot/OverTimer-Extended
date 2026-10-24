using NUnit.Framework;
using UnityEngine;

/// <summary>Mục 4.5.3 của kế hoạch kiểm thử, TC-PWN-1…3: cầm đồ (thả Pawn lên vật, xem Pawning).</summary>
public class PawnTests : GameSceneTest
{
    private static int Money => MoneyService.Instance.Current;

    // ---------- TC-PWN-1 ----------

    [Test]
    public void TC_PWN_1_CamDoLaptop_NhanBang80PhanTramGiaCatalog()
    {
        // Laptop là món khởi đầu duy nhất cầm được: trả 80% giá trị trong ItemCatalog (laptop.asset), không còn $300 cố định.
        var def = ShopService.Instance.Ownership.Def("laptop");
        int expected = PawnMath.PawnValue(def, 0, true);
        Assert.Greater(expected, 0, "Laptop phải cầm được");
        var laptop = TestHooks.Find<DeskLaptopOpener>();
        TestHooks.SetMoney(0);

        Pawning.PawnOnto(laptop);

        Assert.AreEqual(expected, Money);
        Assert.IsFalse(laptop.gameObject.activeSelf, "Laptop biến mất khỏi bàn");
    }

    // ---------- TC-PWN-2 ----------

    private static void AssertCannotPawn(Component target)
    {
        TestHooks.SetMoney(10);
        if (!Pawning.HasCollider(target))
        {
            // Không có collider thì Pawn không chạm được; kiểm tra thêm là vật cũng không khai báo IPawnable.
            Assert.IsNull(target.GetComponent<IPawnable>(), $"{target.name} có IPawnable nhưng không có collider");
            return;
        }

        Pawning.PawnOnto(target);

        Assert.AreEqual(10, Money, $"{target.name}: tiền không được đổi");
        Assert.IsTrue(target.gameObject.activeSelf, $"{target.name}: vật không được biến mất");
    }

    [Test] public void TC_PWN_2a_Meter_KhongCamDoDuoc() => AssertCannotPawn(Meter.Instance);
    [Test] public void TC_PWN_2b_Clock_KhongCamDoDuoc() => AssertCannotPawn(ClockService.Instance);
    [Test] public void TC_PWN_2c_Phone_KhongCamDoDuoc() => AssertCannotPawn(PhoneController.Instance);
    [Test] public void TC_PWN_2e_Cat_KhongCamDoDuoc() => AssertCannotPawn(TestHooks.Find<CatController>());

    // ---------- TC-PWN-3 ----------

    [Test]
    public void TC_PWN_3_CamDoTheDangCoHieuUng_HuyHieuUngVaTraVePool()
    {
        var effect = ScriptableObject.CreateInstance<CountingCardSO>();
        var card = Cards.Place(effect);

        Pawning.PawnOnto(card);

        Assert.AreEqual(1, effect.RemovedCount, "OnRemoved phải được gọi");
        Assert.IsFalse(card.gameObject.activeSelf, "CardItem trở về pool");
        Assert.AreEqual(0, Cards.HandCount);
        Object.Destroy(effect);
    }
}
