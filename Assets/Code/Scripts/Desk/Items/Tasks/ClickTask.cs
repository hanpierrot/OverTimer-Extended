using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
public class ClickTask : TaskBase, IPawnable
{
    [Header("Wobble")]
    [SerializeField] private float wobbleAngle = 5f;
    [SerializeField] private float wobbleDuration = 0.4f;
    [SerializeField] private int wobbleCycles = 3;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    private int MinClicks => GameManager.Instance.GameConfig.clickTaskMinClicks;
    private int MaxClicks => GameManager.Instance.GameConfig.clickTaskMaxClicks;
    private float LockDuration => GameManager.Instance.GameConfig.clickTaskLockDuration;
    
    public int PawnValue => pawnValue;
    
    private int currentClicks = 0;
    private int requiredClicks;
    private PointerReceiver receiver;
    private Coroutine wobbleRoutine;
    
    private void Awake() => receiver = GetComponent<PointerReceiver>();

    private void OnEnable() => receiver.ClickDown += HandleClick;

    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void RollRequiredClicks()
    {
        requiredClicks = RngService.Instance.Random.Next(MinClicks, MaxClicks + 1);
    }
    
    private void HandleClick(Vector2 worldPos)
    {
        if(IsCompleted) return;
        currentClicks++;
        
        PlayWobble();
        
        if(currentClicks >= requiredClicks) CompleteTask();
    }
    
    private void PlayWobble()
    {
        if (wobbleRoutine != null) StopCoroutine(wobbleRoutine);
        wobbleRoutine = StartCoroutine(WobbleRoutine());
    }

    private IEnumerator WobbleRoutine()
    {
        float t = 0f;
        while (t < wobbleDuration)
        {
            t += Time.deltaTime;
            float decay = 1f - (t / wobbleDuration);
            float angle = Mathf.Sin(t / wobbleDuration * wobbleCycles * Mathf.PI * 2f) * wobbleAngle * decay;
            transform.localEulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }
        
        transform.localEulerAngles = Vector3.zero;
        wobbleRoutine = null;
    }

    protected override void OnTaskCompleted()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.gray2;
        StartCoroutine(UnlockAfterDelay());
    }

    private IEnumerator UnlockAfterDelay()
    {
        yield return new WaitForSeconds(LockDuration);
        
        currentClicks = 0; 
        RollRequiredClicks();
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        ResetCompletion();
    }
    
    public void OnPawned() => Destroy(gameObject);
}
