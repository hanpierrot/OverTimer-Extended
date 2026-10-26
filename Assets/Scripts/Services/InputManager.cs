using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private PointerEventData _uiPointer;
    private Vector2 pressWorldPos;
    private bool isDragging;
    
    private static readonly List<RaycastResult> UiHits = new();
    
    public bool IsPressing => currentTarget != null;

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
        if (pointAction == null) return;   // a duplicate that Awake destroyed never built its actions
        pointAction.Enable();
        clickAction.Enable();
    }

    private void OnDisable()
    {
        if (pointAction == null) return;
        pointAction.Disable();
        clickAction.Disable();
    }

    private void OnDestroy()
    {
        if (clickAction != null)
        {
            clickAction.started -= OnClickStarted;
            clickAction.canceled -= OnClickCanceled;
            clickAction.Dispose();
        }
        if (pointAction != null) pointAction.Dispose();
        if (Instance == this) Instance = null;
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
    
    public Collider2D PeekTarget()
    {
        if (!IsInputEnabled || IsPressing) return null;
        if (IsPointerOverUI()) return null;
        return PickTarget(GetPointerWorldPos());
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (_uiPointer == null) _uiPointer = new PointerEventData(EventSystem.current);

        _uiPointer.position = pointAction.ReadValue<Vector2>();
        UiHits.Clear();
        EventSystem.current.RaycastAll(_uiPointer, UiHits);
        return UiHits.Count > 0;
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (!IsInputEnabled) return;
        if (IsPointerOverUI()) return;
        
        Vector2 worldPos = GetPointerWorldPos();
        Collider2D hit = PickTarget(worldPos);
        if (hit == null) return;
        
        if (FocusRoot != null && !hit.transform.IsChildOf(FocusRoot)) return;

        if (hit.TryGetComponent(out IPointerInputHandler handler))
        {
            currentTarget = handler;
            pressWorldPos = worldPos;
            isDragging = false;
            
            currentTarget.OnPressStart(worldPos);
            AudioService.Play(SfxId.Click);
        }
    }
    
    private Collider2D PickTarget(Vector2 worldPos)
    {
        CameraView view = CameraRig.Instance != null ? CameraRig.Instance.CurrentView : CameraView.Desk;
        Collider2D best = null;
        bool bestIsZone = true;

        foreach (var c in Physics2D.OverlapPointAll(worldPos, interactableLayers))
        {
            if (!ViewGate.IsAllowed(c, view)) continue;

            bool isZone = c.TryGetComponent(out CameraViewButton _);
            if (best == null || (bestIsZone && !isZone))
            {
                best = c;
                bestIsZone = isZone;
            }
        }

        return best;
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (currentTarget == null) return;
        
        EndPress(GetPointerWorldPos());
    }
    
    private void EndPress(Vector2 worldPos)
    {
        AudioService.Play(SfxId.ClickRelease);
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
