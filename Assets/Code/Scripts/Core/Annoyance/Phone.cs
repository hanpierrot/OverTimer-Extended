using System;
using UnityEngine;

/// <summary>
/// Interruption desk object (DESIGN.md §5.7). Rings on a random real-time interval;
/// while ringing the clock ticks at ringEffectMultiplier via CountdownTimer.TickMultiplier
/// (the one field phone/cards are allowed to write). Clicking starts "answering" - the
/// penalty keeps running for answerTime, then the ring stops. Routes through AnnoyanceManager
/// so it never lands within annoyanceMinGap of another interruption (I8).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Phone : DeskObject, IPawnable
{
    public static Phone Instance { get; private set; }

    [SerializeField] private GameObject ringingVisual;
    [SerializeField] private AudioClip ringSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    
    public int PawnValue => pawnValue;

    public bool IsRinging { get; private set; }
    public event Action OnRingStart;
    public event Action OnRingEnd;

    private float nextRingTimer;
    private float ringElapsed;
    private float answeringElapsed = -1f;

    private bool silenced;
    private float silenceTimer;

    private bool robocallsActive;
    private float robocallsTimer;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Not Awake: GameManager/RngService's own Awake isn't guaranteed to run first
        // (see HANDOFF.md §3). Start() only runs after every object's Awake completes.
        ScheduleNextRing();
    }

    private void ScheduleNextRing()
    {
        float min = GameManager.Instance.GameConfig.ringIntervalMin;
        float max = GameManager.Instance.GameConfig.ringIntervalMax;
        if (robocallsActive) { min *= 0.5f; max *= 0.5f; }

        nextRingTimer = min + (float)RngService.Instance.Random.NextDouble() * (max - min);
    }

    private void Update()
    {
        if (!ClockService.Instance.IsRunning || ClockService.Instance.IsGameOver) return;

        if (silenced)
        {
            silenceTimer -= Time.deltaTime;
            if (silenceTimer <= 0f) silenced = false;
        }

        if (robocallsActive)
        {
            robocallsTimer -= Time.deltaTime;
            if (robocallsTimer <= 0f) robocallsActive = false;
        }

        if (!IsRinging)
        {
            if (silenced) return;

            nextRingTimer -= Time.deltaTime;
            if (nextRingTimer <= 0f) TryStartRing();
            return;
        }

        ringElapsed += Time.deltaTime;

        GameConfig cfg = GameManager.Instance.GameConfig;
        if (answeringElapsed >= 0f)
        {
            answeringElapsed += Time.deltaTime;
            if (answeringElapsed >= GameManager.Instance.GameConfig.answerTime) StopRing();
        }
        else if (ringElapsed >= GameManager.Instance.GameConfig.maxRingDuration)
        {
            StopRing();
        }
    }

    private void TryStartRing()
    {
        if (AnnoyanceManager.Instance != null && !AnnoyanceManager.Instance.TryBegin("phone"))
        {
            nextRingTimer = 1f; // retry shortly rather than skipping this ring
            return;
        }

        IsRinging = true;
        ringElapsed = 0f;
        answeringElapsed = -1f;
        ClockService.Instance.TickMultiplier = GameManager.Instance.GameConfig.ringEffectMultiplier;

        if (ringingVisual) ringingVisual.SetActive(true);
        PlayRingSound();
        OnRingStart?.Invoke();
    }

    private void StopRing()
    {
        IsRinging = false;
        ClockService.Instance.TickMultiplier = 1f;

        if (ringingVisual) ringingVisual.SetActive(false);
        StopRingSound();
        AnnoyanceManager.Instance?.End("phone");

        ScheduleNextRing();
        OnRingEnd?.Invoke();
    }

    private void PlayRingSound()
    {
        if (audioSource == null || ringSound == null) return;

        audioSource.clip = ringSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopRingSound()
    {
        if (audioSource == null) return;
        audioSource.Stop();
    }

    protected override void OnClicked(Vector2 worldPos)
    {
        if (IsRinging && answeringElapsed < 0f)
            answeringElapsed = 0f;
    }

    /// <summary>Do Not Disturb buff - silences the phone for duration, cutting off an active ring immediately.</summary>
    public void Silence(float duration)
    {
        silenced = true;
        silenceTimer = duration;
        if (IsRinging) StopRing();
    }

    public void Unsilence()
    {
        silenced = false;
        silenceTimer = 0f;
    }

    /// <summary>Robocalls debuff - rings roughly twice as often for duration.</summary>
    public void StartRobocalls(float duration)
    {
        robocallsActive = true;
        robocallsTimer = duration;
    }

    public void StopRobocalls()
    {
        robocallsActive = false;
        robocallsTimer = 0f;
    }
    
    public void OnPawned() => Destroy(gameObject);
}
