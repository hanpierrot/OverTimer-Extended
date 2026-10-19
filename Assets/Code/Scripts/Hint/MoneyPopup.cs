using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MoneyPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text[] pool;
    [SerializeField] private Color popupColor = new Color32(0x4C, 0xAF, 0x72, 0xFF);
    
    private int _last;
    private int _pending;
    private float _flushAt;
    private int _next;

    private void Start()
    {
        _last = GameManager.Instance.GameConfig.startMoney;
        foreach (var t in pool) t.gameObject.SetActive(false);
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDestroy()
    {
        if (MoneyService.Instance != null) MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        foreach (var t in pool) DOTween.Kill(t);
    }

    private void HandleMoneyChanged(int current, string reason)
    {
        int delta = current - _last;
        _last = current;

        if (delta <= 0 || reason == MoneyService.StakeRefundReason) return;
        if (GameManager.Instance.IsGameOver) return;

        if (_pending == 0) _flushAt = Time.time + GameManager.Instance.GameConfig.moneyPopupMergeSeconds;
        _pending += delta;
    }

    private void Update()
    {
        if (_pending > 0 && Time.time >= _flushAt)
        {
            Spawn(_pending);
            _pending = 0;
        }
    }
    
    private void Spawn(int amount)
    {
        var config = GameManager.Instance.GameConfig;
        TMP_Text text = pool[_next];
        _next = (_next + 1) % pool.Length;

        DOTween.Kill(text);
        text.text = "+$" + amount;
        text.color = popupColor;
        text.rectTransform.anchoredPosition = Vector2.zero;
        text.gameObject.SetActive(true);

        Sequence s = DOTween.Sequence().SetId(text);
        s.Join(text.rectTransform.DOAnchorPosY(config.moneyPopupRise, config.moneyPopupSeconds).SetEase(Ease.OutQuad));
        s.Join(text.DOFade(0f, config.moneyPopupSeconds).SetEase(Ease.InQuad));
        s.OnComplete(() => text.gameObject.SetActive(false));
    }
}
