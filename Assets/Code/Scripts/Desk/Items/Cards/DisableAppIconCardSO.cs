using UnityEngine;

[CreateAssetMenu(fileName = "DriverCrash", menuName = "Overtime/Cards/Effects/Driver Crash")]
public class DisableAppIconCardSO : EffectCardSO
{
    [SerializeField] private float duration = 15f;
    [SerializeField] private string targetAppName = "Mining";

    public override void OnPlaced(CardItem card)
    {
        Debug.Log("Debuff applied!");
        DesktopIconView.SetVisible(targetAppName, false);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        DesktopIconView.SetVisible(targetAppName, true);
    }
}
