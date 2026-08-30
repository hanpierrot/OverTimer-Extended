using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pick up the pickaxe icon and drag it into the ore block, then back out -
/// that in-and-out cycle is what fires a Hit, like an actual swing rather
/// than just parking the pickaxe on top of the block. You can repeat this
/// as many times as you like in one hold. Releasing always snaps the pickaxe
/// back to its resting spot; releasing while still inside the block also
/// counts as a hit (letting go force-detaches it, same as pulling out).
///
/// Uses PointerDown/Up rather than Unity's Begin/EndDrag, which only fire
/// after the pointer clears a few pixels of movement - that dead zone made
/// the pickaxe feel like it wouldn't "hold." PointerDown fires the instant
/// the mouse button goes down instead.
///
/// Tracks via world position (Rect.position), not anchoredPosition - the
/// latter is only meaningful relative to this object's own anchor, and if
/// that anchor ever moves in the Inspector, anchoredPosition math silently
/// breaks (it did once already). World position works regardless of anchor.
///
/// Must sit on the object with the visible, raycastable Image (the pickaxe
/// sprite itself) - not a parent container with no Graphic, or the
/// EventSystem never routes pointer events here at all.
/// </summary>
[RequireComponent(typeof(Image))]
public class PickaxeView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform oreTarget;
    [SerializeField] private float hitCooldown = 0.7f;

    private RectTransform _rect;
    private RectTransform _parentRect;
    private Vector3 _homePosition;
    private Camera _eventCamera;
    private bool _held;
    private bool _wasOverlapping;
    private float _lastHitTime = -Mathf.Infinity;

    public event Action Hit;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _parentRect = (RectTransform)_rect.parent;
        _homePosition = _rect.position;

        if (oreTarget == null)
            Debug.LogWarning($"{nameof(PickaxeView)} on {name} has no Ore Target assigned - every swing will miss.", this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _held = true;
        _wasOverlapping = false;
        _eventCamera = eventData.pressEventCamera;
    }

    private void Update()
    {
        if (!_held || Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_parentRect, screenPos, _eventCamera, out var worldPoint))
            _rect.position = worldPoint;

        bool isOverlapping = RectTransformUtility.RectangleContainsScreenPoint(oreTarget, screenPos, _eventCamera);
        if (_wasOverlapping && !isOverlapping)
            TryHit(); // dragged in, then back out - one completed swing
        _wasOverlapping = isOverlapping;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_held) return;
        _held = false;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        if (RectTransformUtility.RectangleContainsScreenPoint(oreTarget, screenPos, _eventCamera))
            TryHit(); // released mid-block - forced detach, still counts

        _wasOverlapping = false;
        _rect.position = _homePosition;
    }

    // Caps swing rate so jittering the pickaxe right at the block's edge
    // can't fire hits faster than an actual swing would allow.
    private void TryHit()
    {
        if (Time.time - _lastHitTime < hitCooldown) return;
        _lastHitTime = Time.time;
        Hit?.Invoke();
    }
}
