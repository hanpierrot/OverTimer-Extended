using UnityEngine;
using UnityEngine.UI;

public class WireLineView : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;

    private void Awake()
    {
        if (rect == null) rect = (RectTransform)transform;
    }
    
    public void SetLine(Vector2 from, Vector2 to, Color color)
    {
        Vector2 dir = to - from;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rect.anchoredPosition = from;
        rect.sizeDelta = new Vector2(length, rect.sizeDelta.y);
        rect.localEulerAngles = new Vector3(0f, 0f, angle);

        if (image != null) image.color = color;
    }
}
