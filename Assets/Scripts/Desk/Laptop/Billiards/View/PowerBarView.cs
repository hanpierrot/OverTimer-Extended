using UnityEngine;
using UnityEngine.UI;

/// A frame with a fill that grows left to right with the shot power. Plain colours for now (placeholder art).
public class PowerBarView : MonoBehaviour
{
    [SerializeField] private RectTransform fill;                 // stretched inside the frame; width follows the power
    [SerializeField] private Image fillImage;
    [SerializeField] private Color low = new Color32(110, 200, 90, 255);
    [SerializeField] private Color mid = new Color32(240, 210, 70, 255);
    [SerializeField] private Color high = new Color32(230, 70, 50, 255);
    [SerializeField] private Color tooWeak = new Color32(150, 150, 150, 255);   // below the minimum: releasing cancels

    /// power 0..1. minPower: below it the shot would be cancelled, so the bar goes grey.
    public void Set(float power, float minPower)
    {
        power = Mathf.Clamp01(power);
        if (fill != null) fill.anchorMax = new Vector2(power, fill.anchorMax.y);
        if (fillImage == null) return;
        fillImage.color = power < minPower ? tooWeak
            : power < 0.5f ? Color.Lerp(low, mid, power * 2f)
            : Color.Lerp(mid, high, (power - 0.5f) * 2f);
    }
}
