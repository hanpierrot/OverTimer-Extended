using UnityEngine;
using UnityEngine.UI;

/// One ball on the felt. Only draws: position and visibility come from the simulation through BilliardsApp.
/// Anchored to the felt's bottom-left corner, so anchoredPosition is the felt-local position.
public class BallView : MonoBehaviour
{
    [SerializeField] private Image image;

    private RectTransform _rect;

    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    public string BallId { get; private set; }

    public void Setup(string ballId, Sprite sprite, float diameter)
    {
        BallId = ballId;
        gameObject.name = "Ball " + ballId;
        if (image != null)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
        }
        Rect.sizeDelta = new Vector2(diameter, diameter);
    }

    public void SetPosition(Vector2 feltPosition) => Rect.anchoredPosition = feltPosition;

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }
}
