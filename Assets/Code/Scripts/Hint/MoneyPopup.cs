using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoneyPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text[] pool;
    
    private Canvas _canvas;
    private int _next;
    private float _lastSpawn = -10f;
    private int _stack;

    private GameConfig Config => GameManager.Instance.GameConfig;
    
    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.SetAsLastSibling(); 
        foreach (var t in pool) { t.raycastTarget = false; t.gameObject.SetActive(false); }

        MoneyService.Instance.OnIncome += HandleIncome;
        EggTimerTask.TimePaid += HandleTimePaid;
    }

    private void OnDestroy()
    {
        if (MoneyService.Instance != null) MoneyService.Instance.OnIncome -= HandleIncome;
        EggTimerTask.TimePaid -= HandleTimePaid;
        foreach (var t in pool) DOTween.Kill(t);
    }

    private void HandleIncome(int amount, string reason) => Spawn("+$" + amount, Config.rewardPopupMoneyColor);
    private void HandleTimePaid(float seconds) => Spawn($"+{seconds:0.#}s", Config.rewardPopupTimeColor);

    private void Spawn(string label, Color color)
    {
        if (GameManager.Instance.IsGameOver) return;

        var cfg = Config;
        int cap = Mathf.Clamp(cfg.rewardPopupMaxActive, 1, pool.Length);
        TMP_Text text = pool[_next];
        _next = (_next + 1) % cap; 

        DOTween.Kill(text);
        text.text = label;
        text.color = color;

        float now = Time.unscaledTime;
        _stack = now - _lastSpawn <= cfg.rewardPopupStackSeconds ? _stack + 1 : 0;
        _lastSpawn = now;

        float scale = _canvas.scaleFactor;
        Vector2 start = PointerScreenPosition() + Vector2.up * (_stack * text.preferredHeight * scale);

        RectTransform rt = text.rectTransform;
        rt.position = start;
        rt.localScale = Vector3.one * 0.6f;
        text.gameObject.SetActive(true);

        Sequence s = DOTween.Sequence().SetId(text).SetUpdate(true);
        s.Join(rt.DOMoveY(start.y + cfg.rewardPopupRisePixels * scale, cfg.rewardPopupSeconds).SetEase(Ease.OutQuad));
        s.Join(text.DOFade(0f, cfg.rewardPopupSeconds).SetEase(Ease.InQuad));
        s.Insert(0f, rt.DOScale(1f, cfg.rewardPopupPopSeconds).SetEase(Ease.OutBack));
        s.OnComplete(() => text.gameObject.SetActive(false));
    }

    private static Vector2 PointerScreenPosition()
    {
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }
}
