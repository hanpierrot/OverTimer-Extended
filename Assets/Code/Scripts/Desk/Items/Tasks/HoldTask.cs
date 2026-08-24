using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PointerReceiver))]
public class HoldTask : TaskBase, IPawnable
{
    [Header("Sprite Animation")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] holdSprites;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;

    private PointerReceiver receiver;
    private float holdTimer;
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
        receiver.DragStart += HandleHoldStart;
        receiver.DragUpdate += HandleHoldUpdate;
        receiver.DragEnd += HandleHoldEnd;
    }

    protected virtual void OnDisable()
    {
        receiver.DragStart -= HandleHoldStart;
        receiver.DragUpdate -= HandleHoldUpdate;
        receiver.DragEnd -= HandleHoldEnd;
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

        if (holdTimer >= GameManager.Instance.GameConfig.crankTime)
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
            
            if (spriteRenderer != null && holdSprites.Length > 0)
                spriteRenderer.sprite = holdSprites[0];
        }
    }

    private void UpdateHoldSprite()
    {
        if (spriteRenderer == null || holdSprites.Length == 0) return;

        float progress = Mathf.Clamp01(holdTimer / GameManager.Instance.GameConfig.crankTime);
        int index = Mathf.Min(Mathf.FloorToInt(progress * holdSprites.Length), holdSprites.Length - 1);
        spriteRenderer.sprite = holdSprites[index];
    }
    
    protected void ResetHoldState()
    {
        isHolding = false;
        holdTimer = 0f;
        ResetCompletion();
    }

    protected override void ApplyReward()
    {
        CountdownTimer.Instance.AddTime(GameManager.Instance.GameConfig.timePayout);
    }
}
