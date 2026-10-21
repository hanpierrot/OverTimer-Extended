using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingFlourish : MonoBehaviour
{
    [SerializeField] private RectTransform area;
    [SerializeField] private Image[] coins;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float fallSeconds = 1.4f;
    [SerializeField] private float spawnSpread = 1.6f;
    [SerializeField] private float spinSeconds = 0.8f;
    
    private void Awake()
    {
        if (audioSource != null) audioSource.ignoreListenerPause = true;
    }

    private void OnDisable() => DOTween.Kill(this);

    public void Play(string reason, Action onFinished)
    {
        gameObject.SetActive(true);
        StartCoroutine(Run(reason, onFinished));
    }

    public void StopNow()
    {
        StopAllCoroutines();
        DOTween.Kill(this);
        gameObject.SetActive(false);
    }
    
    private IEnumerator Run(string reason, Action onFinished)
    {
        ShowText(reason);
        if (audioSource != null && coinClip != null) audioSource.PlayOneShot(coinClip);

        int loops = Mathf.Max(1, Mathf.CeilToInt(duration / fallSeconds));
        float top = area.rect.height * 0.5f + 40f;
        foreach (var coin in coins)
            Drop(coin, (float)RngService.Instance.Random.NextDouble() * spawnSpread, loops, top);

        yield return new WaitForSecondsRealtime(duration);
        if (onFinished != null) onFinished();
    }

    private void ShowText(string reason)
    {
        reasonText.text = reason;
        reasonText.alpha = 0f;
        reasonText.transform.localScale = Vector3.one * 0.7f;
        reasonText.gameObject.SetActive(true);

        Sequence s = DOTween.Sequence().SetId(this).SetUpdate(true);
        s.Join(reasonText.DOFade(1f, 0.4f));
        s.Join(reasonText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
    }

    private void Drop(Image coin, float delay, int loops, float top)
    {
        RectTransform rt = coin.rectTransform;
        coin.gameObject.SetActive(true);
        rt.anchoredPosition = new Vector2(RandomX(), top);

        rt.DOAnchorPosY(-top, fallSeconds).SetEase(Ease.Linear)
            .SetDelay(delay).SetLoops(loops, LoopType.Restart)
            .SetUpdate(true).SetId(this)
            .OnStepComplete(() => rt.anchoredPosition = new Vector2(RandomX(), top))
            .OnComplete(() =>
            {
                rt.DOKill();
                coin.gameObject.SetActive(false);
            });

        float dir = RngService.Instance.Random.Next(2) == 0 ? 1f : -1f;
        rt.DORotate(new Vector3(0f, 0f, 360f * dir), spinSeconds, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart)
            .SetUpdate(true).SetId(this);
    }

    private float RandomX()
    {
        float half = area.rect.width * 0.5f;
        return Mathf.Lerp(-half, half, (float)RngService.Instance.Random.NextDouble());
    }
}
