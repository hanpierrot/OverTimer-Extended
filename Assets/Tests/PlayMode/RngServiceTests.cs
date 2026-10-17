using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Mục 4.2.3 của kế hoạch kiểm thử, TC-RNG-1: hai RngService cùng seed cho cùng dãy số.
/// Không cần scene: RngService tạo Random trong Awake(), nên test tạo GameObject ở trạng thái tắt,
/// đặt seed vào hai field [SerializeField] của nó, rồi bật lên để Awake() chạy như trong game.
/// </summary>
public class RngServiceTests
{
    private const int Seed = 12345;
    private const int Count = 100;

    private static int[] SequenceFromNewService(int seed)
    {
        // RngService là singleton: bỏ tham chiếu Instance cũ để Awake() của bản mới không tự hủy.
        TestHooks.SetStaticAutoProperty<RngService>("Instance", null);

        var go = new GameObject("RngService (test)");
        go.SetActive(false);
        var service = go.AddComponent<RngService>();
        TestHooks.SetField(service, "useFixedSeed", true);
        TestHooks.SetField(service, "seed", seed);
        go.SetActive(true);                                    // Awake() chạy ở đây

        Assert.AreSame(service, RngService.Instance);
        var numbers = new int[Count];
        for (int i = 0; i < Count; i++) numbers[i] = service.Random.Next();

        Object.Destroy(go);
        return numbers;
    }

    [Test]
    public void TC_RNG_1_CungSeed_HaiDayGiongHetNhau()
    {
        var previous = RngService.Instance;
        try
        {
            int[] first = SequenceFromNewService(Seed);
            int[] second = SequenceFromNewService(Seed);

            CollectionAssert.AreEqual(first, second);
        }
        finally
        {
            TestHooks.SetStaticAutoProperty<RngService>("Instance", previous);
        }
    }

    [Test]
    public void KhacSeed_HaiDayKhacNhau()
    {
        // Ca đối chứng: nếu seed bị bỏ qua (luôn dùng seed ngẫu nhiên hoặc cố định cứng), TC-RNG-1 vẫn có thể đạt nhờ may mắn.
        var previous = RngService.Instance;
        try
        {
            CollectionAssert.AreNotEqual(SequenceFromNewService(Seed), SequenceFromNewService(Seed + 1));
        }
        finally
        {
            TestHooks.SetStaticAutoProperty<RngService>("Instance", previous);
        }
    }
}
