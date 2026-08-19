using System;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
public abstract class DeskObject : MonoBehaviour
{
    protected PointerReceiver Receiver{get; private set;}

    protected virtual void Awake()
    {
        Receiver = GetComponent<PointerReceiver>();
    }
    
    protected virtual void OnEnable()
    {
        Receiver.ClickDown += OnClicked;
        Receiver.DragStart += OnDragStarted;
        Receiver.DragUpdate += OnDragMoved;
        Receiver.DragEnd += OnDragEnded;
    }

    protected virtual void OnDisable()
    {
        Receiver.ClickDown -= OnClicked;
        Receiver.DragStart -= OnDragStarted;
        Receiver.DragUpdate -= OnDragMoved;
        Receiver.DragEnd -= OnDragEnded;
    }

    protected virtual void OnClicked(Vector2 worldPos) { }
    protected virtual void OnDragStarted(Vector2 worldPos) { }
    protected virtual void OnDragMoved(Vector2 worldPos) { }
    protected virtual void OnDragEnded(Vector2 worldPos) { }
}
