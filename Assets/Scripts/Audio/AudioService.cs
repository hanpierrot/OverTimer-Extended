using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the sound effects of <see cref="SfxId"/> through a small pool of AudioSources. Most sounds are driven by events
/// the game already raises (income, cards, game over, hints...), subscribed to in Start; the few that have no event are
/// a one-line AudioService.Play(...) next to the action. Everything is looked up in the AudioCatalogSO, so a missing
/// clip is silent, not an error.
/// </summary>
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [SerializeField] private AudioCatalogSO catalog;
    [SerializeField, Min(1)] private int voices = 8;

    private AudioSource[] _sources;
    private int _next;
    private readonly Dictionary<SfxId, AudioCatalogSO.Entry> _entries = new Dictionary<SfxId, AudioCatalogSO.Entry>();
    private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();
    // Cosmetic randomness only (which clip, pitch jitter): never RngService, so sound can not shift the seeded gameplay stream.
    private readonly System.Random _variation = new System.Random();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _sources = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            var child = new GameObject($"Voice {i}");
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            _sources[i] = source;
        }

        if (catalog == null) { Debug.LogWarning("AudioService has no AudioCatalog assigned: the game will be silent."); return; }
        foreach (var entry in catalog.entries)
            if (entry != null && !_entries.ContainsKey(entry.id)) _entries.Add(entry.id, entry);
    }

    // Other services are read in Start (not Awake/OnEnable): their Awake order is not guaranteed.
    private void Start()
    {
        if (MoneyService.Instance != null) MoneyService.Instance.OnIncome += HandleIncome;
        if (CardHandManager.Instance != null) CardHandManager.Instance.CardPlaced += HandleCardPlaced;
        if (CardCollectionManager.Instance != null) CardCollectionManager.Instance.NewCardRegistered += HandleCardCollected;
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver += HandleGameOver;
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.ModeChanged += HandleDeviceMode;
        CardRevealSlot.CardRevealed += HandleCardRevealed;
        HintService.HintShown += HandleHintShown;
    }

    private void OnDestroy()
    {
        if (MoneyService.Instance != null) MoneyService.Instance.OnIncome -= HandleIncome;
        if (CardHandManager.Instance != null) CardHandManager.Instance.CardPlaced -= HandleCardPlaced;
        if (CardCollectionManager.Instance != null) CardCollectionManager.Instance.NewCardRegistered -= HandleCardCollected;
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= HandleGameOver;
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.ModeChanged -= HandleDeviceMode;
        CardRevealSlot.CardRevealed -= HandleCardRevealed;
        HintService.HintShown -= HandleHintShown;
        if (Instance == this) Instance = null;
    }

    /// <summary>Plays a sound. Safe to call when there is no AudioService (tests, a scene without it).</summary>
    public static void Play(SfxId id, float volumeScale = 1f)
    {
        if (Instance != null) Instance.PlayInternal(id, volumeScale);
    }

    private void PlayInternal(SfxId id, float volumeScale)
    {
        if (!_entries.TryGetValue(id, out var entry) || entry.clips == null || entry.clips.Length == 0) return;

        float now = Time.unscaledTime;
        if (entry.minInterval > 0f && _lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval) return;
        _lastPlayed[id] = now;

        AudioClip clip = entry.clips[entry.clips.Length == 1 ? 0 : _variation.Next(entry.clips.Length)];
        if (clip == null) return;

        AudioSource source = NextSource();
        source.clip = clip;
        source.volume = Mathf.Clamp01(entry.volume * volumeScale);
        source.pitch = 1f + (float)(_variation.NextDouble() * 2.0 - 1.0) * entry.pitchJitter;
        source.ignoreListenerPause = entry.ignoreListenerPause;
        source.Play();
    }

    /// <summary>A voice that is not playing, else the one that has been playing longest.</summary>
    private AudioSource NextSource()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            int index = (_next + i) % _sources.Length;
            if (!_sources[index].isPlaying)
            {
                _next = (index + 1) % _sources.Length;
                return _sources[index];
            }
        }

        var stolen = _sources[_next];
        _next = (_next + 1) % _sources.Length;
        return stolen;
    }

    // ---------- Event handlers ----------

    private void HandleIncome(int amount, string reason)
    {
        if (catalog == null) return;
        Play(amount <= catalog.moneySmallMax ? SfxId.MoneyGainSmall
            : amount <= catalog.moneyMediumMax ? SfxId.MoneyGainMedium
            : SfxId.MoneyGainLarge);
    }

    private void HandleCardPlaced(CardSO card) => Play(SfxId.CardPlace);
    private void HandleCardRevealed() => Play(SfxId.CardFlip);
    private void HandleCardCollected(CardSO card) => Play(SfxId.CardCollected);
    private void HandleDeviceMode(DeviceLayer.Mode mode) => Play(SfxId.UiToggle);

    private void HandleGameOver(EndReason reason) => Play(RunRating.IsWin(reason) ? SfxId.WinStinger : SfxId.LoseStinger);

    private void HandleHintShown(HintId id)
    {
        switch (id)
        {
            case HintId.CharlieTease:
            case HintId.MotherLodeTease:
            case HintId.TicketCursed:
                Play(SfxId.Tease);
                break;

            // A hint that explains why an action was refused.
            case HintId.NotEnoughMoney:
            case HintId.MeterMinFeed:
            case HintId.MeterLocked:
            case HintId.TooManyTickets:
            case HintId.HandFull:
            case HintId.NoRoomToPlace:
            case HintId.NotEnoughTime:
            case HintId.TaxCannotAfford:
            case HintId.CannotPawn:
            case HintId.DebuffDismissCost:
                Play(SfxId.UiError);
                break;
        }
    }
}
