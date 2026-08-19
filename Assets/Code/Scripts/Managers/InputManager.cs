using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private LayerMask interactableLayers = ~0;
    
    public bool IsInputEnabled { get; set; } = true;
    public Transform FocusRoot { get; set; }
    
    private Vector2 lastPointerPos;
    private bool hasLastPointerPos;

    private InputAction clickAction;
    private InputAction pointAction;

    private IPointerInputHandler currentTarget;

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
            currentTarget.OnDragUpdate(GetPointerWorldPos());
            
            if (Mouse.current != null && !Mouse.current.leftButton.isPressed)
            {
                currentTarget.OnDragEnd(GetPointerWorldPos());
                currentTarget = null;
            }
        }

        UpdateMouseSpeed();
    }

    private void UpdateMouseSpeed()
    {
        if (Mouse.current == null) return;
        
        Vector2 delta = Mouse.current.delta.ReadValue();
        
        float normalizedDelta = delta.magnitude / Screen.height;
        float speed = normalizedDelta / Time.deltaTime;

        CountdownTimer.Instance.SetSpeedMult(speed);
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
            handler.OnClickDown(worldPos);
            handler.OnDragStart(worldPos);
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (currentTarget == null) return;
        
        currentTarget.OnDragEnd(GetPointerWorldPos());
        currentTarget = null;
    }

    private Vector2 GetPointerWorldPos()
    {
        Vector2 screenPos = pointAction.ReadValue<Vector2>();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
}
