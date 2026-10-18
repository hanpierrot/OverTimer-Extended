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
    /// <summary>Telemarketer: seconds of pitch left (game time). 0 = the player may hang up.</summary>
    public float PitchLeft { get; private set; }
    /// <summary>Client: real seconds left to take the job before the caller hangs up.</summary>
    public float ClientLeft { get; private set; }
    public bool ClientAccepted { get; private set; }
    /// <summary>ITRS: the payment plan was taken on this call (the call ends a moment later).</summary>
    public bool PaymentPlanTaken { get; private set; }
    public float ItrsExtension => GameManager.Instance.GameConfig.itrsCallExtension;
    public int ClientMoney => GameManager.Instance.GameConfig.clientGigMoney;
    /// <summary>Newest first, e.g. "Missed: Unknown". Shown by the Call app.</summary>
    public IReadOnlyList<string> CallLog => _callLog;
    /// <summary>Bumped on every state change so views can refresh only when needed.</summary>
    public int Version { get; private set; }

    private readonly List<string> _callLog = new List<string>();
    private readonly HashSet<object> _robocalls = new HashSet<object>();
    private const float ClientThanksSeconds = 1.2f;
    private float _planThanksLeft;
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
        if (ActiveCall == null) return;

        if (ActiveCall.truth == CallTruth.Telemarketer)
        {
            if (PitchLeft > 0f) { PitchLeft = Mathf.Max(0f, PitchLeft - gameDelta); Version++; }
        }
        else if (ActiveCall.truth == CallTruth.Client)
        {
            // Real time: the clock is paused during this call.
            ClientLeft -= Time.unscaledDeltaTime;
            Version++;
            if (ClientLeft <= 0f) FinishCall();          // took the job (thanks beat over) or the caller hung up
        }
        else if (PaymentPlanTaken)
        {
            _planThanksLeft -= Time.unscaledDeltaTime;   // real time: the clock is paused during an ITRS call
            if (_planThanksLeft <= 0f) FinishCall();
        }
    }

    /// <summary>ITRS call: take the payment plan, +itrsCallExtension seconds to pay the open bill. Once per call.</summary>
    public void AcceptPaymentPlan()
    {
        if (ActiveCall == null || ActiveCall.truth != CallTruth.ITRS || PaymentPlanTaken) return;
        var itrs = ITRSService.Instance;
        if (itrs == null || !itrs.HasPendingAssessment) return;

        itrs.ExtendCurrentBill(ItrsExtension);
        PaymentPlanTaken = true;
        _planThanksLeft = ClientThanksSeconds;
        Log($"Payment plan: +{ItrsExtension:0}s");
        Version++;
    }

    // ---- Calls ----

    private void HandleRingStarted(CallInfo call)
    {
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
            Log($"{what}: {call.shownAs}");
            Inbox.Add(MessageKind.MissedCall, $"{what} call", call.shownAs.ToString());
        }
        Version++;
    }

    private void BeginCall(CallInfo call)
    {
        ActiveCall = call;
        PitchLeft = call.truth == CallTruth.Telemarketer ? Config.telemarketerPitch : 0f;
        ClientLeft = call.truth == CallTruth.Client ? Config.beneficialCallTimeout : 0f;
        ClientAccepted = false;
        PaymentPlanTaken = false;

        if (call.PausesClock)
        {
            ClockService.Instance.Pause(this);
            if (DeviceLayer.Instance != null)
            {
                DeviceLayer.Instance.LockLaptop(this);
                DeviceLayer.Instance.LockPhoneOpen(this);   // can't walk away from a paused call
            }
        }
        Log($"Answered: {call.shownAs}");
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

    /// <summary>Hang up the active call (telemarketer after the pitch, ITRS any time).</summary>
    public void HangUp()
    {
        if (ActiveCall == null) return;
        if (ActiveCall.truth == CallTruth.Telemarketer && PitchLeft > 0f) return;
        FinishCall();
    }

    /// <summary>Client call: take the +$ job. The call ends a moment later so the screen can say thanks.</summary>
    public void AcceptClientJob()
    {
        if (ActiveCall == null || ActiveCall.truth != CallTruth.Client || ClientAccepted || ClientLeft <= 0f) return;
        ClientAccepted = true;
        MoneyService.Instance.Add(ClientMoney, "client");
        ClientLeft = ClientThanksSeconds;
        Version++;
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

    private void Log(string line)
    {
        _callLog.Insert(0, line);
        if (_callLog.Count > 20) _callLog.RemoveAt(_callLog.Count - 1);
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
