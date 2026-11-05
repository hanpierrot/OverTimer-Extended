using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PointerReceiver : MonoBehaviour, IPointerInputHandler
{
    public event Action<Vector2> ClickDown;
    public event Action<Vector2> DragStart;
    public event Action<Vector2> DragUpdate;
    public event Action<Vector2> DragEnd;
    
    public event Action<Vector2> PressStart;
    public event Action<Vector2> PressUpdate;
    public event Action<Vector2> PressEnd;
    
    public event Action<HintId> ClickRejected;
    public static event Action<HintId> AnyClickRejected;

    // One mechanism for debuffs, the cat, and pawning (DESIGN.md §7.2
    // DeskObject.SetEnabled). Every interactable already has a PointerReceiver,
    // so gating input here covers all of them without a new base class.
    
    private HintId? _blockedReason;

    public bool IsInteractable { get; private set; } = true;

    // True from a forwarded DragStart until the matching DragEnd. The Cat reads it so it never blocks an object that is
    // being dragged, and the end of a drag is always delivered even if the object became non-interactable meanwhile
    // (otherwise a looping sound or a held position would never be released).
    private bool _dragActive;
    public bool IsDragging => _dragActive;
    public void SetInteractable(bool value, HintId? reason = null)
    {
        IsInteractable = value;
        _blockedReason = value ? null : reason;
    }

    public void OnClickDown(Vector2 worldPos)
    {
        if (IsInteractable)
        {
            ClickDown?.Invoke(worldPos);
            return;
        }
        
        if (_blockedReason.HasValue)
        {
            ClickRejected?.Invoke(_blockedReason.Value);
            AnyClickRejected?.Invoke(_blockedReason.Value);
        }
    }
    public void OnDragStart(Vector2 worldPos)
    {
        if (!IsInteractable) return;
        _dragActive = true;
        DragStart?.Invoke(worldPos);
    }

    public void OnDragUpdate(Vector2 worldPos) { if (IsInteractable || _dragActive) DragUpdate?.Invoke(worldPos); }

    public void OnDragEnd(Vector2 worldPos)
    {
        if (!IsInteractable && !_dragActive) return;
        _dragActive = false;
        DragEnd?.Invoke(worldPos);
    }

    private void OnDisable() => _dragActive = false;
    
    public void OnPressStart(Vector2 worldPos) { if (IsInteractable) PressStart?.Invoke(worldPos); }
    public void OnPressUpdate(Vector2 worldPos) { if (IsInteractable) PressUpdate?.Invoke(worldPos); }
    public void OnPressEnd(Vector2 worldPos) { if (IsInteractable) PressEnd?.Invoke(worldPos); }
}
