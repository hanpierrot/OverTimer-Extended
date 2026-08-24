using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PointerReceiver))]
public class ClickTask : TaskBase, IPawnable
{
    [Header("Settings")] 
    [SerializeField] private int requiredClicks = 1;
    [SerializeField] private float lockDuration = 3f;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;
    
    private int currentClicks = 0;
    private PointerReceiver receiver;
    
    private void Awake() => receiver = GetComponent<PointerReceiver>();

    private void OnEnable() => receiver.ClickDown += HandleClick;

    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if(IsCompleted) return;
        currentClicks++;
        
        if(currentClicks >= requiredClicks) CompleteTask();
    }

    protected override void OnTaskCompleted()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.gray4;
        StartCoroutine(UnlockAfterDelay());
    }

    private IEnumerator UnlockAfterDelay()
    {
        yield return new WaitForSeconds(lockDuration);
        
        currentClicks = 0; 
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        ResetCompletion();
    }
}
