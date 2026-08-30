using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float dragThreshold = 0.15f;
    
    public bool IsInputEnabled { get; set; } = true;
    public Transform FocusRoot { get; set; }
    
    private Vector2 lastPointerPos;
    private bool hasLastPointerPos;

    private InputAction clickAction;
    private InputAction pointAction;

    private IPointerInputHandler currentTarget;
    private Vector2 pressWorldPos;
    private bool isDragging;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        pointAction = new InputAction("Point", InputActionType.Value, "<Mouse>/position");
        clickAction = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");

        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
    }

    private void OnEnable()
    {
        pointAction.Enable();
        clickAction.Enable();
    }
    
    private void OnDisable()
    {
        pointAction.Disable();
        clickAction.Disable();
    }

    private void Update()
    {
        if(currentTarget != null)
        {
            Vector2 worldPos = GetPointerWorldPos();
            
            currentTarget.OnPressUpdate(worldPos);
            
            if (!isDragging && Vector2.Distance(worldPos, pressWorldPos) >= dragThreshold)
            {
                isDragging = true;
                currentTarget.OnDragStart(pressWorldPos);
            }

            if (isDragging)
                currentTarget.OnDragUpdate(worldPos);

            if (Mouse.current != null && !Mouse.current.leftButton.isPressed)
                EndPress(worldPos);
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (!IsInputEnabled) return;
        
        Vector2 worldPos = GetPointerWorldPos();
        Collider2D hit = Physics2D.OverlapPoint(worldPos, interactableLayers);
        if (hit == null) return;
        
        if (FocusRoot != null && !hit.transform.IsChildOf(FocusRoot)) return;

        if (hit.TryGetComponent(out IPointerInputHandler handler))
        {
            currentTarget = handler;
            pressWorldPos = worldPos;
            isDragging = false;
            
            currentTarget.OnPressStart(worldPos);
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (currentTarget == null) return;
        
        EndPress(GetPointerWorldPos());
    }
    
    private void EndPress(Vector2 worldPos)
    {
        currentTarget.OnPressEnd(worldPos);
        
        if (isDragging)
            currentTarget.OnDragEnd(worldPos);
        else
        {
            currentTarget.OnClickDown(pressWorldPos);
            ClickFeedbackService.Instance?.PlayAt(pressWorldPos);
        }

        currentTarget = null;
    }

    private Vector2 GetPointerWorldPos()
    {
        Vector2 screenPos = pointAction.ReadValue<Vector2>();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
}
