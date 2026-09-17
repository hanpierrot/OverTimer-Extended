using UnityEngine;

[System.Serializable]
public struct PieceGroup
{
    public CardSO[] alternatives;
}

[CreateAssetMenu(fileName = "SetCompletionAnchor", menuName = "Overtime/Cards/Effects/Set Completion Anchor")]
public class SetCompletionCardSO : EffectCardSO
{
    public PieceGroup[] pieces;

    public override void OnPlaced(CardItem card) => SetCompletionTracker.Instance?.BeginTracking(this, pieces);
    public override void OnRemoved(CardItem card) => SetCompletionTracker.Instance?.StopTracking();
}
