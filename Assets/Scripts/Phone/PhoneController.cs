using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Phone (plan Phase 1.3). Sits on the desk Phone object and owns the pure phone state: power, the call
/// scheduler, the Inbox and the banner queue. Views (PhoneHome, CallScreen, IncomingCallPill) only read it.
/// Clicking the desk phone opens the phone UI through the DeviceLayer; calls are answered on the phone UI.
///
/// Timing: rings, call gaps, the telemarketer pitch and booting count ClockService.GameDelta (only while the clock runs).
/// Banners and the client-choice countdown use real time, because the clock is paused during a client call.
/// A ringing phone speeds the clock (SetSpeed keyed to this); client and ITRS calls pause it and lock the Laptop.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PhoneController : DeskObject
{
    public static PhoneController Instance { get; private set; }

    [SerializeField] private GameObject ringingVisual;
    [SerializeField] private AudioClip ringSound;
    [Tooltip("The phone's OWN source (it used to share one with the tax notice, so stopping the ring cut the tax nag).")]
    [SerializeField] private AudioSource audioSource;

    public PhonePower Power { get; private set; }
    public CallScheduler Calls { get; private set; }
    public Inbox Inbox { get; } = new Inbox();
    public BannerQueue Banners { get; private set; }
    public PhoneConfig Config { get; private set; }

    /// <summary>The call being handled right now (answered, not yet finished), else null.</summary>
    public CallInfo ActiveCall { get; private set; }
    public bool IsRinging => Calls != null && Calls.Current == CallScheduler.State.Ringing;
    public CallInfo RingingCall => IsRinging ? Calls.Call : null;
    /// <summary>Which line of the caller's script is on screen (null when no call is active).</summary>
    public CallDialogue Dialogue { get; private set; }
    /// <summary>What the caller is saying right now.</summary>
    public string CallerLine { get; private set; } = "";
    /// <summary>The player's button label for this line ("Got you.", "Hang up."...).</summary>
    public string ReplyLabel { get; private set; } = "";
    /// <summary>False while the telemarketer is still pitching: the reply button is greyed out (no countdown is shown).</summary>
    public bool ReplyEnabled => ActiveCall != null && Dialogue != null && !_goodbye && !(ActiveCall.truth == CallTruth.Telemarketer && Dialogue.Index == 0 && _pitchLeft > 0f);

    [Header("Callers")]
    [Tooltip("Caller names, pictures and conversations. Assets/Data/Configs/CallerProfiles.asset")]
    [SerializeField] private CallerProfileCatalog catalog;
    private CallerProfileCatalog _fallbackCatalog;
    public CallerProfileCatalog Catalog
    {
        get
        {
            if (catalog != null) return catalog;
            if (_fallbackCatalog == null)
            {
                Debug.LogWarning("[Phone] No CallerProfileCatalog assigned: using the built-in default callers and lines.");
                _fallbackCatalog = ScriptableObject.CreateInstance<CallerProfileCatalog>();
            }
            return _fallbackCatalog;
        }
    }
    /// <summary>Newest first. Shown by the Call app's history list.</summary>
    public IReadOnlyList<CallRecord> CallHistory => _history;
    /// <summary>Bumped on every state change so views can refresh only when needed.</summary>
    public int Version { get; private set; }

    private const int HistoryMax = 30;
    private readonly List<CallRecord> _history = new List<CallRecord>();
    private CallRecord _record;      // the record of the call in progress
    private int _nextRecordId = 1;
    private bool _goodbye;           // a client who never answered says their goodbye line, then the call ends
    private float _goodbyeLeft;      // REAL time
    private readonly HashSet<object> _robocalls = new HashSet<object>();
    private IReadOnlyList<CallerProfileCatalog.Line> _script;
    private float _pitchLeft;        // telemarketer, game time: the reply button stays grey until the pitch is over
    private float _firstLineLeft;    // client, REAL time: if the player never answers the first line the caller hangs up
    private float _hangUpLeft;       // last line, REAL time: a stalling player is hung up on
    private bool _ready;
    private bool _shaking;
    private Quaternion _baseRotation;

    protected override void Awake()
    {
        base.Awake();
        _baseRotation = transform.localRotation;
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Not Awake: GameManager/RngService's own Awake isn't guaranteed to run first.
        var cfg = GameManager.Instance.GameConfig;
        Config = PhoneConfigFrom(cfg);
        Power = new PhonePower(Config.bootSeconds);
        Banners = new BannerQueue(cfg.bannerLifetime);
        Calls = new CallScheduler(Config, RngService.Instance.Random)
        {
            CanRing = () => Power.IsUsable,
            ItrsEligible = () => ITRSService.Instance != null && ITRSService.Instance.HasPendingAssessment,
            IntervalScale = _robocalls.Count > 0 ? 0.5f : 1f,
        };

        Inbox.Added += HandleInboxAdded;
        Power.Changed += HandlePowerChanged;
        Calls.RingStarted += HandleRingStarted;
        Calls.RingStopped += HandleRingStopped;
        Calls.CallFinished += HandleCallFinished;
        _ready = true;
    }

    private static PhoneConfig PhoneConfigFrom(GameConfig g) => new PhoneConfig
    {
        weightTelemarketer = g.callWeightTelemarketer,
        weightClient = g.callWeightClient,
        weightUnknown = g.callWeightUnknown,
        weightITRS = g.callWeightITRS,
        unknownSpamChance = g.unknownSpamChance,
        ringIntervalMin = g.ringIntervalMin,
        ringIntervalMax = g.ringIntervalMax,
        maxRingDuration = g.maxRingDuration,
        callMinGap = g.annoyanceMinGap,
        ringClockMultiplier = g.ringEffectMultiplier,
        telemarketerPitch = g.telemarketerPitch,
        bootSeconds = g.phoneBootSeconds,
        beneficialCallTimeout = g.beneficialCallTimeout,
    };

    private void Update()
    {
        if (!_ready) return;
        if (GameManager.Instance.IsGameOver) { Shutdown(); return; }

        float g = ClockService.Instance.GameDelta;
        Power.Tick(g);
        Calls.Tick(g);
        Banners.Tick(Time.unscaledDeltaTime);
        TickActiveCall(g);
        Shake();
    }

    // The desk phone shakes while it rings.
    private void Shake()
    {
        bool ringing = IsRinging;
        if (!ringing && !_shaking) return;
        _shaking = ringing;
        float angle = ringing ? Mathf.Sin(Time.unscaledTime * 45f) * 6f : 0f;
        transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void TickActiveCall(float gameDelta)
    {
        if (ActiveCall == null || Dialogue == null) return;

        // None of these timers is shown to the player.
        if (_goodbye)
        {
            _goodbyeLeft -= Time.unscaledDeltaTime;
            if (_goodbyeLeft <= 0f) FinishCall();
        }
        else if (Dialogue.IsFinal)
        {
            _hangUpLeft -= Time.unscaledDeltaTime;          // real time: the clock may be paused
            if (_hangUpLeft <= 0f) FinishCall();
        }
        else if (ActiveCall.truth == CallTruth.Telemarketer)
        {
            if (_pitchLeft > 0f) _pitchLeft = Mathf.Max(0f, _pitchLeft - gameDelta);
        }
        else if (ActiveCall.truth == CallTruth.Client && Dialogue.Index == 0)
        {
            _firstLineLeft -= Time.unscaledDeltaTime;
            if (_firstLineLeft <= 0f) BeginGoodbye();
        }
    }

    /// <summary>The player never answered the client: they say "Hello? Are you there?" and hang up. No money.</summary>
    private void BeginGoodbye()
    {
        _goodbye = true;
        _goodbyeLeft = GameManager.Instance.GameConfig.callGoodbyeSeconds;
        CallerLine = Catalog.NoReplyLine;
        ReplyLabel = "";
        if (_record != null)
        {
            _record.transcript.Add($"{_record.name}: {CallerLine}");
            _record.result = "Hung up, no reply";
        }
        Version++;
    }

    /// <summary>The player's button on the current line: it moves the conversation on, and on the last line it hangs up.</summary>
    public void Reply()
    {
        if (ActiveCall == null || Dialogue == null || _goodbye) return;
        if (_record != null) _record.transcript.Add($"You: {ReplyLabel}");
        if (Dialogue.IsFinal) { FinishCall(); return; }
        if (!ReplyEnabled) return;

        Dialogue.Advance();
        ShowLine();
    }

    private void ShowLine()
    {
        var line = _script[Dialogue.Index];
        CallerLine = line.callerLine;
        ReplyLabel = line.replyLabel;
        if (Dialogue.IsFinal) Payoff();                      // may swap CallerLine (ITRS with nothing to extend)
        if (_record != null) _record.transcript.Add($"{_record.name}: {CallerLine}");
        Version++;
    }

    private string NameFor(CallerKind kind, int seed)
    {
        var p = Catalog.Pick(kind, seed);
        if (p != null && !string.IsNullOrEmpty(p.displayName)) return p.displayName;
        return kind == CallerKind.Unknown ? "Unknown number" : kind.ToString();
    }

    /// <summary>The caller's name for a call kind and picture seed (ringing screen, live call and history all agree).</summary>
    public string CallerName(CallerKind kind, int seed) => NameFor(kind, seed);

    private void AddRecord(CallRecord r)
    {
        r.id = _nextRecordId++;
        _history.Insert(0, r);
        if (_history.Count > HistoryMax) _history.RemoveAt(_history.Count - 1);
    }

    /// <summary>The last line is where the call pays off: the client pays, ITRS gives more time.</summary>
    private void Payoff()
    {
        var config = GameManager.Instance.GameConfig;
        _hangUpLeft = config.callHangUpDelay;

        if (ActiveCall.truth == CallTruth.Client)
        {
            MoneyService.Instance.Add(config.clientGigMoney, "client");
            if (_record != null) _record.result = $"+${config.clientGigMoney}";
        }
        else if (ActiveCall.truth == CallTruth.ITRS)
        {
            var itrs = ITRSService.Instance;
            if (itrs != null && itrs.HasPendingAssessment)
            {
                itrs.ExtendCurrentBill(config.itrsCallExtension);
                if (_record != null) _record.result = $"+{config.itrsCallExtension:0}s to pay";
            }
            else
            {
                // Already paid: the time carries over to the NEXT bill's deadline.
                if (itrs != null) itrs.GrantNextBillTime(config.itrsCallExtension);
                CallerLine = Catalog.ItrsNoBillLine;
                if (_record != null) _record.result = $"+{config.itrsCallExtension:0}s on next bill";
            }
        }
    }

    // ---- Calls ----

    private void HandleRingStarted(CallInfo call)
    {
        call.profileSeed = RngService.Instance.Random.Next(0, 1 << 20);   // which name/picture the caller shows
        ClockService.Instance.SetSpeed(this, Config.ringClockMultiplier);
        if (ringingVisual != null) ringingVisual.SetActive(true);
        PlayRingSound();
        if (HintService.Instance != null) HintService.Instance.ShowOnce(HintId.PhoneFirstRing);
        Version++;
    }

    private void HandleRingStopped(CallInfo call, CallEnd how)
    {
        ClockService.Instance.ClearSpeed(this);
        if (ringingVisual != null) ringingVisual.SetActive(false);
        StopRingSound();

        if (how == CallEnd.Answered) BeginCall(call);
        else
        {
            string what = how == CallEnd.Declined ? "Declined" : "Missed";
            AddRecord(new CallRecord { name = NameFor(call.shownAs, call.profileSeed), outcome = how });
            Inbox.Add(MessageKind.MissedCall, $"{what} call", call.shownAs.ToString());
        }
        Version++;
    }

    private void BeginCall(CallInfo call)
    {
        ActiveCall = call;
        _script = Catalog.Script(call.truth);
        Dialogue = new CallDialogue(_script.Count);
        _pitchLeft = call.truth == CallTruth.Telemarketer ? Config.telemarketerPitch : 0f;
        _firstLineLeft = call.truth == CallTruth.Client ? Config.beneficialCallTimeout : 0f;
        _goodbye = false;
        var kind = call.truth == CallTruth.Client ? CallerKind.Client : call.truth == CallTruth.ITRS ? CallerKind.ITRS : CallerKind.Telemarketer;
        _record = new CallRecord { name = NameFor(kind, call.profileSeed), outcome = CallEnd.Answered };
        AddRecord(_record);

        if (call.PausesClock)
        {
            ClockService.Instance.Pause(this);
            if (DeviceLayer.Instance != null)
            {
                DeviceLayer.Instance.LockLaptop(this);
                DeviceLayer.Instance.LockPhoneOpen(this);   // can't walk away from a paused call
            }
        }
        if (call.PausesClock && HintService.Instance != null) HintService.Instance.ShowOnce(HintId.PausedCall);
        ShowLine();                                  // the caller's first line (a one-line script pays off right away)
    }

    private void HandleCallFinished(CallInfo call)
    {
        ClockService.Instance.Resume(this);
        if (DeviceLayer.Instance != null)
        {
            DeviceLayer.Instance.UnlockLaptop(this);
            DeviceLayer.Instance.UnlockPhoneOpen(this);
        }
        ActiveCall = null;
        Dialogue = null;
        _script = null;
        _record = null;
        _goodbye = false;
        CallerLine = "";
        ReplyLabel = "";
        Version++;
    }

    /// <summary>Answer the ringing call (phone UI button).</summary>
    public void Answer()
    {
        if (!_ready || !Calls.Answer()) return;
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.OpenPhone();
    }

    /// <summary>Decline the ringing call. Free.</summary>
    public void Decline()
    {
        if (_ready) Calls.Decline();
    }

    private void FinishCall()
    {
        if (ActiveCall != null) Calls.EndCall();
    }

    // ---- Power / Cable Eater ----

    /// <summary>The phone's power button.</summary>
    public void PressPower()
    {
        if (_ready) Power.Press();
    }

    public void ForceOff(object holder)
    {
        if (_ready) Power.ForceOff(holder);
    }

    public void ReleaseForceOff(object holder)
    {
        if (_ready) Power.ReleaseForceOff(holder);
    }

    private void HandlePowerChanged(PhonePower.State state)
    {
        if (state != PhonePower.State.On)
        {
            Calls.Abort();
            Banners.Clear();
        }
        Version++;
    }

    private void HandleInboxAdded(InboxMessage m)
    {
        if (Power.IsUsable) Banners.Push(m);
        Version++;
    }

    // ---- Robocalls card ----

    /// <summary>Robocalls: calls come about twice as often while any holder is registered (the holder is the card).</summary>
    public void AddRobocalls(object holder)
    {
        _robocalls.Add(holder);
        if (Calls != null) Calls.IntervalScale = 0.5f;
    }

    public void RemoveRobocalls(object holder)
    {
        _robocalls.Remove(holder);
        if (Calls != null && _robocalls.Count == 0) Calls.IntervalScale = 1f;
    }

    // ---- Desk object ----

    protected override void OnClicked(Vector2 worldPos)
    {
        if (DeviceLayer.Instance != null) DeviceLayer.Instance.OpenPhone();
    }

    // ---- Audio / teardown ----

    private void PlayRingSound()
    {
        if (audioSource == null || ringSound == null) return;
        audioSource.clip = ringSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopRingSound()
    {
        if (audioSource != null) audioSource.Stop();
    }

    /// <summary>Game over: end any ring or call and let go of every pause, speed key and lock.</summary>
    private void Shutdown()
    {
        if (Calls != null) Calls.Abort();
        Release();
        StopRingSound();
    }

    private void Release()
    {
        if (ClockService.Instance != null)
        {
            ClockService.Instance.ClearSpeed(this);
            ClockService.Instance.Resume(this);
        }
        if (DeviceLayer.Instance != null)
        {
            DeviceLayer.Instance.UnlockLaptop(this);
            DeviceLayer.Instance.UnlockPhoneOpen(this);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Release();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
