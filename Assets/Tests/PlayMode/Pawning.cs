using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Cầm đồ: thả Pawn lên một vật. Game xử lý trong Pawn.OnTriggerEnter2D khi collider của Pawn chạm vật,
/// nên test gọi đúng hàm đó với collider của vật (không cần mô phỏng kéo chuột và va chạm vật lý).
/// </summary>
public static class Pawning
{
    public static Pawn Tool => TestHooks.Find<Pawn>();

    public static bool HasCollider(Component target) => target.GetComponentInChildren<Collider2D>(true) != null;

    public static void PawnOnto(Component target)
    {
        var collider = target.GetComponentInChildren<Collider2D>(true);
        Assert.IsNotNull(collider, $"{target.name} không có Collider2D nên Pawn không chạm được");
        TestHooks.Call(Tool, "OnTriggerEnter2D", collider);
    }
}
