using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// One ball on the felt. Only draws: position and visibility come from the simulation through BilliardsApp.
/// Anchored to the felt's bottom-left corner, so anchoredPosition is the felt-local position.
/// Potting and re-spotting are visual-only tweens; the simulation has long since decided the outcome.
/// The tweens follow Time.timeScale (no SetUpdate(true)), so a game over freezes them like everything else.
public class BallView : MonoBehaviour
{
    private const float PotSeconds = 0.15f;
    private const float PunchSeconds = 0.25f;

    [SerializeField] private Image image;

    private RectTransform _rect;
    private bool _potted;

    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    public string BallId { get; private set; }
    public Vector2 FeltPosition => Rect.anchoredPosition;

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

    /// Jump straight to a state with no animation (a new table, or coming back to a kept one).
    public void ResetTo(bool potted, Vector2 feltPosition)
    {
        KillTweens();
        _potted = potted;
        SetVisible(!potted);
        if (!potted) SetPosition(feltPosition);
    }

    /// Follow the simulation. pocketFeltPosition is where a ball that just dropped rolls to.
    public void Sync(bool potted, Vector2 feltPosition, Vector2 pocketFeltPosition, bool animate)
    {
        if (potted)
        {
            if (_potted) return;
            _potted = true;
            if (animate) PlayPot(pocketFeltPosition); else { KillTweens(); SetVisible(false); }
            return;
        }

        if (_potted)                                       // put back on the table (a respot after a foul or a wrong pocket)
        {
            _potted = false;
            KillTweens();
            SetVisible(true);
            SetPosition(feltPosition);
            if (animate) Rect.DOPunchScale(Vector3.one * 0.3f, PunchSeconds, 6, 0.6f).SetId(this);
            return;
        }

        SetVisible(true);
        SetPosition(feltPosition);
    }

    /// Hide for good (a cleared table), cancelling whatever animation is running.
    public void Hide()
    {
        KillTweens();
        _potted = true;
        SetVisible(false);
    }

    public void KillTweens()
    {
        DOTween.Kill(this);
        if (_rect != null) _rect.localScale = Vector3.one;
    }

    private void PlayPot(Vector2 pocketFeltPosition)
    {
        KillTweens();
        DOTween.Sequence()
            .Join(Rect.DOAnchorPos(pocketFeltPosition, PotSeconds).SetEase(Ease.InQuad))
            .Join(Rect.DOScale(0f, PotSeconds).SetEase(Ease.InQuad))
            .OnComplete(() => { SetVisible(false); Rect.localScale = Vector3.one; })
            .SetId(this);
    }

    public void SetPosition(Vector2 feltPosition) => Rect.anchoredPosition = feltPosition;

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }

    private void OnDestroy() => DOTween.Kill(this);
}
