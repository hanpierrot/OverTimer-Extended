using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WirePlug : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;

    public int PairIndex { get; private set; }
    public Color WireColor { get; private set; }
    public Vector2 AnchoredPosition => ((RectTransform)transform).anchoredPosition;

    public event Action<WirePlug> DragBegan;
    public event Action<PointerEventData> DragMoved;
    public event Action DragEnded;

    public void Setup(int pairIndex, Color wireColor)
    {
        PairIndex = pairIndex;
        WireColor = wireColor;
        if (icon != null) icon.color = wireColor;
    }
    
    public void OnPointerDown(PointerEventData eventData) => DragBegan?.Invoke(this);
    public void OnDrag(PointerEventData eventData) => DragMoved?.Invoke(eventData);
    public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke();
}
