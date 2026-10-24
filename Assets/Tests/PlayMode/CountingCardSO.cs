/// <summary>
/// Thẻ hiệu ứng chỉ dùng trong test: đếm số lần OnPlaced / OnRemoved / OnPawned được gọi (TC-EFX-2, TC-PWN-3).
/// Tạo bằng ScriptableObject.CreateInstance, không có asset.
/// </summary>
public class CountingCardSO : EffectCardSO
{
    public int PlacedCount;
    public int RemovedCount;
    public int PawnedCount;

    public override void OnPlaced(CardItem card) => PlacedCount++;
    public override void OnRemoved(CardItem card) => RemovedCount++;
    public override void OnPawned(CardItem card) => PawnedCount++;
}
