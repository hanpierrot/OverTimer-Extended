using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class HintService : MonoBehaviour
{
    public static HintService Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text label;

    private readonly Dictionary<HintId, float> _lastShown = new();
    private readonly HashSet<HintId> _seen = new();
    private Sequence _sequence;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        group.alpha = 0f;
    }

    private void OnEnable() => PointerReceiver.AnyClickRejected += HandleRejected;

    private void OnDisable() => PointerReceiver.AnyClickRejected -= HandleRejected;

    private void OnDestroy() => _sequence?.Kill();

    private void HandleRejected(HintId id) => Show(id);

    public void Show(HintId id, params object[] args)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        var config = GameManager.Instance != null ? GameManager.Instance.GameConfig : null;
        float cooldown = config != null ? config.hintRepeatCooldown : 3f;
        
        float now = Time.unscaledTime;
        if (_lastShown.TryGetValue(id, out float last) && now - last < cooldown) return;
        _lastShown[id] = now;
        
        string text = HintCatalog.Get(id);
        label.text = args != null && args.Length > 0 ? string.Format(text, args) : text;
        
        _sequence?.Kill();
        group.alpha = 1f;
        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.AppendInterval(config != null ? config.hintHoldSeconds : 2.5f);
        _sequence.Append(group.DOFade(0f, config != null ? config.hintFadeSeconds : 0.5f));
    }
    
    public void ShowOnce(HintId id, params object[] args)
    {
        string key = $"HintSeen_{id}";
        if (_seen.Contains(id) || PlayerPrefs.GetInt(key, 0) == 1) return;

        _seen.Add(id);
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Show(id, args);
    }
}
