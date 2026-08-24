using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PointerReceiver : MonoBehaviour, IPointerInputHandler
{
    public event Action<Vector2> ClickDown;
    public event Action<Vector2> DragStart;
    public event Action<Vector2> DragUpdate;
    public event Action<Vector2> DragEnd;

    // One mechanism for debuffs, the cat, and pawning (DESIGN.md §7.2
    // DeskObject.SetEnabled). Every interactable already has a PointerReceiver,
    // so gating input here covers all of them without a new base class.
    public bool IsInteractable { get; private set; } = true;
    public void SetInteractable(bool value) => IsInteractable = value;

    public void OnClickDown(Vector2 worldPos) { if (IsInteractable) ClickDown?.Invoke(worldPos); }
    public void OnDragStart(Vector2 worldPos) { if (IsInteractable) DragStart?.Invoke(worldPos); }
    public void OnDragUpdate(Vector2 worldPos) { if (IsInteractable) DragUpdate?.Invoke(worldPos); }
    public void OnDragEnd(Vector2 worldPos) { if (IsInteractable) DragEnd?.Invoke(worldPos); }
}
