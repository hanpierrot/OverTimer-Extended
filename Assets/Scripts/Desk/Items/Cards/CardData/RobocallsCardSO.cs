using UnityEngine;

[CreateAssetMenu(fileName = "Robocalls", menuName = "Overtime/Cards/Effects/Robocalls")]
public class RobocallsCardSO : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.AddRobocalls(card);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.RemoveRobocalls(card);
    }
}
