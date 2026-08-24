using System;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
public class DragTask : TaskBase, IPawnable
{
    [Header("Settings")] 
    [SerializeField] private Transform handle;
    [SerializeField] private Vector2 dragAxis = Vector2.down;
    [SerializeField] private float requiredDistance = 2f;
    [SerializeField] private bool reset = true;
    
    [Header("Spawn On Complete")]
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private Vector3 spawnPoint;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;
    
    private PointerReceiver receiver;
    private Vector3 startPos;
    private Vector2 pointerStartWPos;
    private float draggedDist;

    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        
        if(handle == null) handle = transform;
        startPos = handle.position;
    }
    
    private void OnEnable()
    {
        receiver.DragStart += HandleDragStart;
        receiver.DragUpdate += HandleDragUpdate;
        receiver.DragEnd += HandleDragEnd;
    }

    private void OnDisable()
    {
        receiver.DragStart -= HandleDragStart;
        receiver.DragUpdate -= HandleDragUpdate;
        receiver.DragEnd -= HandleDragEnd;
    }

    public void HandleDragStart(Vector2 worldPos)
    {
        if (IsCompleted) return;
        pointerStartWPos = worldPos;
        
        CountdownTimer.Instance.PauseCountdown();
    }

    public void HandleDragUpdate(Vector2 worldPos)
    {
        if(IsCompleted) return;

        Vector2 delta = worldPos - pointerStartWPos;
        float projected = Vector2.Dot(delta, dragAxis.normalized);
        draggedDist = Mathf.Clamp(projected, 0f, requiredDistance);
        
        handle.position = startPos + (Vector3)(dragAxis.normalized * draggedDist);

        if (draggedDist >= requiredDistance)
        {
            CompleteTask();
        }
    }

    public void HandleDragEnd(Vector2 worldPos)
    {
        CountdownTimer.Instance.ResumeCountdown();
        
        if (IsCompleted) return;

        if (reset)
        {
            draggedDist = 0f;
            handle.position = startPos;
        }
    }

    protected override void OnTaskCompleted()
    {
        if (spawnPrefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint : transform.position;
            Instantiate(spawnPrefab, pos, Quaternion.identity);
        }
    }
}
