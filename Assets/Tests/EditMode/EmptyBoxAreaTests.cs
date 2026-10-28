using NUnit.Framework;
using UnityEngine;

/// <summary>Hình học của vùng đặt hộp rỗng (DESIGN §5.7): hình chữ nhật cho bởi hai góc chéo.</summary>
public class EmptyBoxAreaTests
{
    [Test]
    public void HaiGocCheoDuocSapLaiThanhMinMax()
    {
        var area = new EmptyBoxArea(new Vector2(4f, 0f), new Vector2(0f, 2f));

        Assert.AreEqual(new Vector2(0f, 0f), area.Min);
        Assert.AreEqual(new Vector2(4f, 2f), area.Max);
    }

    [Test]
    public void Contains_TinhCaBienVaLoaiDiemNgoai()
    {
        var area = new EmptyBoxArea(new Vector2(0f, 0f), new Vector2(4f, 2f));

        Assert.IsTrue(area.Contains(new Vector2(2f, 1f)));
        Assert.IsTrue(area.Contains(new Vector2(0f, 0f)));
        Assert.IsTrue(area.Contains(new Vector2(4f, 2f)));
        Assert.IsFalse(area.Contains(new Vector2(-0.01f, 1f)));
        Assert.IsFalse(area.Contains(new Vector2(2f, 2.01f)));
    }

    [Test]
    public void RandomPoint_LuonNamTrongVung()
    {
        var area = new EmptyBoxArea(new Vector2(-5.3f, -4.45f), new Vector2(7.78f, 2.5f));
        var rng = new System.Random(7);

        for (int i = 0; i < 200; i++) Assert.IsTrue(area.Contains(area.RandomPoint(rng)));
    }

    [Test]
    public void DropPoint_KepXVaoVungVaLayYCuaCanhDuoi()
    {
        var area = new EmptyBoxArea(new Vector2(0f, 1f), new Vector2(10f, 6f));

        Assert.AreEqual(new Vector2(0f, 1f), area.DropPoint(new Vector2(-3f, 5f)));
        Assert.AreEqual(new Vector2(10f, 1f), area.DropPoint(new Vector2(14f, 2f)));
        Assert.AreEqual(new Vector2(6f, 1f), area.DropPoint(new Vector2(6f, 9f)));
    }
}
