using UnityEngine;

[CreateAssetMenu(fileName = "DisableAppIconCard", menuName = "Overtime/Cards/Effects/Disable App Icon")]
public class DisableAppIconCardSO : EffectCardSO
{
    [SerializeField] private float duration = 15f;
    [SerializeField] private string targetAppName = "Mining";

    public override void OnPlaced(CardItem card)
    {
        DesktopIconView.SetVisible(targetAppName, false);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        DesktopIconView.SetVisible(targetAppName, true);
    }
}
