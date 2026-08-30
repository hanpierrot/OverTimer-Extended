using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WireSocket : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image icon;

    public int PairIndex { get; private set; }
    public Vector2 AnchoredPosition => ((RectTransform)transform).anchoredPosition;

    public event Action<WireSocket> Dropped;
    
    public void Setup(int pairIndex, Color color)
    {
        PairIndex = pairIndex;
        if (icon != null) icon.color = color;
    }

    public void OnDrop(PointerEventData eventData) => Dropped?.Invoke(this);
}
