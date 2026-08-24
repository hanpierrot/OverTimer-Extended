using UnityEngine;
using UnityEngine.Serialization;

public abstract class TaskBase : MonoBehaviour
{
    // Renamed from timeReward: tasks pay MONEY now. Only the Meter converts
    // money -> clock time (DESIGN.md I3) - see MoneyService.
    [FormerlySerializedAs("timeReward")]
    [SerializeField] private int moneyReward = 5;

    public bool IsCompleted { get;  private set; }
    protected virtual bool ResetAfterComplete => false;

    protected void CompleteTask()
    {
        if(IsCompleted) return;
        IsCompleted = true;

        ApplyReward();
        OnTaskCompleted();
        
        if (ResetAfterComplete) IsCompleted = false;
    }
    
    protected void ResetCompletion() => IsCompleted = false;
    
    protected virtual void ApplyReward()
    {
        MoneyService.Instance.Add(moneyReward, "task");
    }

    protected virtual void OnTaskCompleted() { }
}
