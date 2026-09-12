using UnityEngine;

[CreateAssetMenu(fileName = "FinalCountdown", menuName = "Overtime/Cards/Effects/Final Countdown")]
public class FinalCountdownCardSO : EffectCardSO
{
    [SerializeField] private float resetSeconds = 180f;

    public override bool isDebuff =>
        FinalCountdownTracker.Instance != null &&
        FinalCountdownTracker.Instance.CurrentPhase != FinalCountdownTracker.Phase.None;

    public override bool BlocksPawning =>
        FinalCountdownTracker.Instance != null &&
        FinalCountdownTracker.Instance.CurrentPhase == FinalCountdownTracker.Phase.Triggered;

    public override void OnPlaced(CardItem card)
    {
        var tracker = FinalCountdownTracker.Instance;
        if (tracker == null) return;

        switch (tracker.CurrentPhase)
        {
            case FinalCountdownTracker.Phase.None:
                tracker.Arm(resetSeconds);
                break;

            case FinalCountdownTracker.Phase.Armed:
                GameManager.Instance.Lose();
                break;

            case FinalCountdownTracker.Phase.Triggered:
                tracker.RegisterWin(this);
                break;
        }
    }
    
    public override void OnRemoved(CardItem card)
    {
        var tracker = FinalCountdownTracker.Instance;
        if (tracker != null && tracker.CurrentPhase == FinalCountdownTracker.Phase.Armed)
            tracker.Disarm();
    }
}
