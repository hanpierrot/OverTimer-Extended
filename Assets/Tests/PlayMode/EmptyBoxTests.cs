using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Hộp rỗng sau khi mở gói hàng (DESIGN §5.7): xuất hiện ở Room view, kéo vào Printer để bán, thả sai chỗ thì về góc.</summary>
public class EmptyBoxTests : GameSceneTest
{
    private static DoorController Door => DoorController.Instance;

    private static void OpenOneParcel()
    {
        var parcel = Door.Depot.Receive(1, new string[0]);
        Assert.IsNotNull(parcel);
        for (int i = 0; i < 20 && !Door.Depot.Click(parcel); i++) { }
    }

    private static EmptyBox[] ActiveBoxes => Door.EmptyBoxes.Where(b => b.gameObject.activeSelf).ToArray();

    // Gọi đúng hàm game gọi khi người chơi thả chuột (không mô phỏng kéo chuột).
    private static void Release(EmptyBox box) => TestHooks.Call(box, "OnDragEnded", Vector2.zero);

    [Test]
    public void CoDuVungVaSoHopTheoConfig()
    {
        Assert.IsTrue(Door.HasEmptyBoxArea, "emptyBoxPrefab chưa gán trong DoorController");
        Assert.AreEqual(Config.doorEmptyBoxSlots, Door.EmptyBoxes.Count);
        Assert.AreEqual(0, ActiveBoxes.Length, "Đầu ván chưa có hộp nào");
    }

    [Test]
    public void MoGoiHang_ConLaiMotHopNamTrongVungVaChiXemDuocORoomView()
    {
        OpenOneParcel();

        var boxes = ActiveBoxes;
        Assert.AreEqual(1, boxes.Length);
        Assert.IsTrue(Door.EmptyBoxArea.Contains(boxes[0].transform.position));
        Assert.AreEqual(3, boxes[0].gameObject.layer, "Hộp phải ở layer DeskObject để kéo được");
        var gate = boxes[0].GetComponent<ViewGate>();
        Assert.IsTrue(gate.Allows(CameraView.Room));
        Assert.IsFalse(gate.Allows(CameraView.Desk));
        Assert.IsFalse(gate.Allows(CameraView.Door));
    }

    [Test]
    public void DonHangLeaveOpen_KhongSinhHop()
    {
        Door.Receive(new Order { id = 1, leaveOpen = true, itemIds = new System.Collections.Generic.List<string>() });

        Assert.AreEqual(0, ActiveBoxes.Length);
    }

    [Test]
    public void KeoHopVaoPrinter_BanDuocGiaJunkNhoVaHopBienMat()
    {
        OpenOneParcel();
        var box = ActiveBoxes[0];
        TestHooks.SetMoney(0);

        Pawning.PawnOnto(box);

        Assert.AreEqual(Config.junkSmallPawnValue, MoneyService.Instance.Current);
        Assert.IsFalse(box.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator ThaHopNgoaiVung_RoiThangXuongCanhDuoi()
    {
        OpenOneParcel();
        var box = ActiveBoxes[0];
        var area = Door.EmptyBoxArea;
        box.transform.position = new Vector3(area.Max.x + 6f, area.Max.y, box.transform.position.z);   // ngoài vùng, bên phải, trên cao

        Release(box);
        yield return new WaitForSeconds(0.6f);

        Assert.AreEqual(area.Max.x, box.transform.position.x, 0.01f, "x vượt vùng thì về x gần nhất trong vùng");
        Assert.AreEqual(area.Min.y, box.transform.position.y, 0.01f);
    }

    [Test]
    public void ThaHopTrongVung_GiuNguyenViTri()
    {
        OpenOneParcel();
        var box = ActiveBoxes[0];
        Vector3 before = box.transform.position;

        Release(box);

        Assert.AreEqual(before, box.transform.position);
    }

    [Test]
    public void HetHop_SinhThemThiTaiDungHopCuNhat()
    {
        int slots = Door.EmptyBoxes.Count;
        for (int i = 0; i < slots; i++) Door.SpawnEmptyBox();
        Assert.AreEqual(slots, ActiveBoxes.Length);
        var oldest = Door.EmptyBoxes.First(b => b.gameObject.activeSelf);
        oldest.transform.position = new Vector3(-999f, 0f, 0f);

        var reused = Door.SpawnEmptyBox();

        Assert.AreSame(oldest, reused, "Hộp cũ nhất được tái dùng");
        Assert.AreEqual(slots, ActiveBoxes.Length, "Không sinh thêm hộp mới");
        Assert.IsTrue(Door.EmptyBoxArea.Contains(reused.transform.position), "Hộp tái dùng được đặt lại trong vùng");
    }

    [Test]
    public void GoiYChiHienLanDauTien()
    {
        PlayerPrefs.DeleteKey("HintSeen_" + HintId.EmptyBoxFirst);   // máy này có thể đã xem rồi (PlayerPrefs được khôi phục sau test)
        int shown = 0;
        HintService.HintShown += Count;
        try
        {
            Door.SpawnEmptyBox();
            Door.SpawnEmptyBox();
        }
        finally { HintService.HintShown -= Count; }

        Assert.AreEqual(1, shown, "EmptyBoxFirst dùng ShowOnce");

        void Count(HintId id) { if (id == HintId.EmptyBoxFirst) shown++; }
    }
}
