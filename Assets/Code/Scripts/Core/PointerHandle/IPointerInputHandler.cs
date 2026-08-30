using UnityEngine;

public interface IPointerInputHandler
{
    void OnClickDown(Vector2 worldPos);
    void OnDragStart(Vector2 worldPos);
    void OnDragUpdate(Vector2 worldPos);
    void OnDragEnd(Vector2 worldPos);
    
    void OnPressStart(Vector2 worldPos);
    void OnPressUpdate(Vector2 worldPos);
    void OnPressEnd(Vector2 worldPos);
}
