using UnityEngine;

/// <summary>
/// "No More X" / "Close The Shop": hides an app icon for a while. Goes through the shop's IconGate (CardHide /
/// CardRelease), so two overlapping cards keep the app hidden until both end, and a card ending never reveals an app the
/// player has no license for. targetAppName is a Laptop app ("Mining") or a phone app id ("Shop"); case does not matter.
/// </summary>
[CreateAssetMenu(fileName = "DisableAppIconCard", menuName = "Overtime/Cards/Effects/Disable App Icon")]
public class DisableAppIconCardSO : EffectCardSO
{
    [SerializeField] private float duration = 15f;
    [SerializeField] private string targetAppName = "Mining";

    public override void OnPlaced(CardItem card)
    {
        if (ShopService.Instance != null) ShopService.Instance.IconGate.CardHide(targetAppName);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (ShopService.Instance != null) ShopService.Instance.IconGate.CardRelease(targetAppName);
    }
}
