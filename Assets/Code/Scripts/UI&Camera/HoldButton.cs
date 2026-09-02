using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public event Action HoldStarted;
    public event Action HoldEnded;

    public void OnPointerDown(PointerEventData eventData) => HoldStarted?.Invoke();
    public void OnPointerUp(PointerEventData eventData) => HoldEnded?.Invoke();
}
