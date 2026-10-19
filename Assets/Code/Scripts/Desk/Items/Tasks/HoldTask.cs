using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PointerReceiver))]
public abstract class HoldTask : TaskBase, IPawnable
{
    protected virtual float CrankTime => GameManager.Instance.GameConfig.crankTime;
    protected abstract int ConfigPawnValue { get; }
    public int PawnValue => ConfigPawnValue;
    public bool CanBePawned => true;

    protected PointerReceiver receiver;
    protected float holdTimer;
    private bool isHolding;

    protected override bool ResetAfterComplete => true;
    protected virtual bool ResetProgressOnRelease => true;
    
    protected virtual void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
    }

    protected virtual void OnEnable()
    {
        receiver.PressStart += HandleHoldStart;
        receiver.PressUpdate += HandleHoldUpdate;
        receiver.PressEnd += HandleHoldEnd; 
    }

    protected virtual void OnDisable()
    {
        receiver.PressStart -= HandleHoldStart;
        receiver.PressUpdate -= HandleHoldUpdate;
        receiver.PressEnd -= HandleHoldEnd;
    }

    /// <summary>A subclass can refuse a new press (the Egg Timer rests after each crank).</summary>
    protected virtual bool CanStartHold => true;

    private void HandleHoldStart(Vector2 worldPos)
    {
        if (!CanStartHold) return;
        isHolding = true;
    }

    private void HandleHoldUpdate(Vector2 worldPos)
    {
        if(!isHolding) return;
        
        holdTimer += Time.deltaTime;
        UpdateHoldSprite();

        if (holdTimer >= CrankTime)
        {
            isHolding = false;
            CompleteTask();
        }
    }

    private void HandleHoldEnd(Vector2 worldPos)
    {
        isHolding = false;
        if(ResetProgressOnRelease)
        {
            holdTimer = 0f;
            OnHoldReset();
        }
    }
    
    protected virtual void OnHoldReset() { }

    protected virtual void UpdateHoldSprite() { }
    
    protected void ResetHoldState()
    {
        isHolding = false;
        holdTimer = 0f;
        ResetCompletion();
    }

    protected override void ApplyReward()
    {
        ClockService.Instance.AddSeconds(GameManager.Instance.GameConfig.timePayout, "hold task");
    }
    
    public void OnPawned() => gameObject.SetActive(false);
}
