using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class ClockService : MonoBehaviour
{
    public static ClockService Instance{get; private set;}

    [Header("Settings")]
    [SerializeField] private float startingTime;
    [SerializeField] private bool startOnAwake = true;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private string timeFormat = "mm\\:ss";

    public float RemainingTime { get; private set; }
    public bool IsGameOver { get; private set; }

    private readonly PauseLock _pause = new PauseLock();
    private readonly SpeedModifiers _speed = new SpeedModifiers();
    private bool _started;
    private static readonly object LegacyHolder = new object();

    /// <summary>True while the clock ticks: started, no pause holder, not over.</summary>
    public bool IsRunning => _started && !_pause.IsPaused && !IsGameOver;

    /// <summary>Seconds of RUNNING clock this frame: Time.deltaTime while running, else 0. Not multiplied by TickMultiplier.
    /// Every gameplay timer (rings, bills, deliveries) counts with this.</summary>
    public float GameDelta { get; private set; }

    /// <summary>Running sum of GameDelta since scene load.</summary>
    public float GameTime { get; private set; }

    public float CurrentSeconds => RemainingTime;
    
    public bool HasFinalCountdownIntercept { get; private set; }
    private float _finalCountdownResetSeconds;
    
    public event Action OnFinalCountdownTriggered;

    /// <summary>Read-only product of every keyed speed modifier. Write through SetSpeed/ClearSpeed.</summary>
    public float TickMultiplier => _speed.Effective;

    public void Pause(object holder) => _pause.Acquire(holder);
    public void Resume(object holder) => _pause.Release(holder);
    public void SetSpeed(object key, float multiplier) => _speed.Set(key, multiplier);
    public void ClearSpeed(object key) => _speed.Remove(key);

    public event Action OnCountdownFinished;
    public event Action<float> OnTimeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        startingTime = GameManager.Instance.GameConfig.startClock;
        RemainingTime = startingTime;
        UpdateDisplay();

        if (startOnAwake)
            StartCountdown();
    }

    private void Update()
    {
        GameDelta = IsRunning ? Time.deltaTime : 0f;
        GameTime += GameDelta;
        RunTimer();
    }

    public void StartCountdown()
    {
        if(IsGameOver)  return;
        _started = true;
    }

    public void PauseCountdown() => Pause(LegacyHolder);

    public void ResumeCountdown() => Resume(LegacyHolder);

    private void RunTimer()
    {
        if (GameDelta <= 0f) return;

        RemainingTime -= GameDelta * TickMultiplier;

        if (RemainingTime <= 0f)
        {
            if (HasFinalCountdownIntercept)
            {
                HasFinalCountdownIntercept = false;
                RemainingTime = _finalCountdownResetSeconds;
                OnFinalCountdownTriggered?.Invoke();
            }
            else
            {
                RemainingTime = 0f;
                EndCountdown();
            }
            
        }
        
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();
    }

    /// <summary>ClockService.AddSeconds(s, reason) of DESIGN.md §7.2 - Meter and egg timer only.</summary>
    public void AddSeconds(float seconds, string reason)
    {
        if(IsGameOver || seconds <= 0f) return;
        
        RemainingTime += seconds;
        
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();
    }

    /// <summary>ClockService.Spend(s, reason) of DESIGN.md §7.2 - blackjack ante, penalties.</summary>
    public void Spend(float seconds, string reason)
    {
        if (IsGameOver || seconds <= 0f) return;
        
        RemainingTime = Mathf.Max(0f, RemainingTime - seconds);
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();

        if (RemainingTime <= GameManager.Instance.GameConfig.loseClock)
            EndCountdown();
    }

    private void EndCountdown()
    {
        IsGameOver = true;
        OnCountdownFinished?.Invoke();
    }

    public void ArmFinalCountdownIntercept(float resetSeconds)
    {
        HasFinalCountdownIntercept = true;
        _finalCountdownResetSeconds = resetSeconds;
    }
    
    public void DisarmFinalCountdownIntercept() => HasFinalCountdownIntercept = false;

    private void UpdateDisplay()
    {
        if (countdownText == null) return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, RemainingTime));
        countdownText.text = timeSpan.ToString(timeFormat);
    }
}
