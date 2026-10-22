using DG.Tweening;
using UnityEngine;

/// <summary>
/// A puff of smoke at a position (a parcel springing open, a bought item appearing on the desk). Pooled by DoorController.
/// Placeholder art: a white circle that grows and fades. When the real smoke arrives, give this prefab a ParticleSystem
/// (it is played too) and, if you like, clear the sprite.
/// </summary>
public class PoofEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private float seconds = 0.55f;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float endScale = 2.6f;

    private Tween _tween;

    public void Play(Vector3 position)
    {
        if (_tween != null) _tween.Kill();
        transform.position = position;
        gameObject.SetActive(true);

        if (particles != null) { particles.Clear(); particles.Play(); }
        if (sprite != null)
        {
            transform.localScale = Vector3.one * startScale;
            var c = sprite.color; c.a = 0.9f; sprite.color = c;
            _tween = DOTween.Sequence()
                .Join(transform.DOScale(endScale, seconds).SetEase(Ease.OutQuad))
                .Join(sprite.DOFade(0f, seconds).SetEase(Ease.InQuad))
                .OnComplete(() => gameObject.SetActive(false))
                .SetTarget(this);
        }
        else _tween = DOVirtual.DelayedCall(seconds, () => gameObject.SetActive(false)).SetTarget(this);
    }

    private void OnDisable()
    {
        if (_tween != null) _tween.Kill();
    }
}
