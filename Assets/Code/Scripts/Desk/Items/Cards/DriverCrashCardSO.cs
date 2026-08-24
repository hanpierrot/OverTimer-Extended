using UnityEngine;

[CreateAssetMenu(fileName = "DriverCrash", menuName = "Overtime/Cards/Effects/Driver Crash")]
public class DriverCrashCardSO : EffectCardSO
{
    [SerializeField] private float duration = 15f;

    public override void OnPlaced(CardItem card)
    {
        Debug.Log("Debuff applied!");
        MiningApp.SetDisabledStatic(true);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        MiningApp.SetDisabledStatic(false);
    }
}
