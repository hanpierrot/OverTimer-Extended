using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PointerReceiver))]
public class HoldTask : TaskBase, IPawnable
{
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    protected virtual float CrankTime => GameManager.Instance.GameConfig.crankTime;
    public int PawnValue => pawnValue;
    public bool CanBePawned => true;

    protected PointerReceiver receiver;
    protected float holdTimer;
    private bool isHolding;

    protected override bool ResetAfterComplete => true;
    protected virtual bool ResetProgressOnRelease => true;
    public void SetHoldDuration(float duration) => GameManager.Instance.GameConfig.crankTime = duration;
    
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

    private void HandleHoldStart(Vector2 worldPos)
    {
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
        ClockService.Instance.AddTime(GameManager.Instance.GameConfig.timePayout);
    }
    
    public void OnPawned() => Destroy(gameObject);
}
