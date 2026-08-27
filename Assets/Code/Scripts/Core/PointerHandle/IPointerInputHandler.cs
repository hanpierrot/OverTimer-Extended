using UnityEngine;

public interface IPointerInputHandler
{
    void OnClickDown(Vector2 worldPos);
    void OnDragStart(Vector2 worldPos);
    void OnDragUpdate(Vector2 worldPos);
    void OnDragEnd(Vector2 worldPos);
}
