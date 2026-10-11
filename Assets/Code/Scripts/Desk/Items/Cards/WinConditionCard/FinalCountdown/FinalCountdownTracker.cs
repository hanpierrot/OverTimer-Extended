using UnityEngine;

public class FinalCountdownTracker : MonoBehaviour
{
    public static FinalCountdownTracker Instance { get; private set; }

    public enum Phase { None, Armed, Triggered }
    public Phase CurrentPhase { get; private set; } = Phase.None;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Arm(float resetSeconds)
    {
        CurrentPhase = Phase.Armed;

        if (ClockService.Instance != null)
        {
            ClockService.Instance.ArmFinalCountdownIntercept(resetSeconds);
            ClockService.Instance.OnFinalCountdownTriggered += HandleTriggered;
        }
    }
    
    public void Disarm()
    {
        CurrentPhase = Phase.None;

        if (ClockService.Instance != null)
        {
            ClockService.Instance.DisarmFinalCountdownIntercept();
            ClockService.Instance.OnFinalCountdownTriggered -= HandleTriggered;
        }
    }

    private void HandleTriggered()
    {
        CurrentPhase = Phase.Triggered;
        if (ClockService.Instance != null) ClockService.Instance.OnFinalCountdownTriggered -= HandleTriggered;
        if (Meter.Instance != null) Meter.Instance.SetLocked(true);
        
        CutsceneController.Instance?.PlayFinalCountdownIntro();
    }
    
    public void RegisterWin(CardSO card)
    {
        CardCollectionManager.Instance?.Register(card);
        GameManager.Instance.GameOver(EndReason.FinalCountdownWin);
    }
}
