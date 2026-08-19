using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    public static CountdownTimer Instance{get; private set;}

    [Header("Settings")]
    [SerializeField] private float startingTime;
    [SerializeField] private bool startOnAwake = true;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private string timeFormat = "mm\\:ss";

    [Header("Timer Speed")] 
    [SerializeField] private float[] risingThresholds = { 1.5f, 3f };
    [SerializeField] private float[] fallingThresholds = { 1.2f, 2.5f };

    private int speedMult = 1;
    private bool isSpeedOverridden;
    private float overrideTimeRemaining;

    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>Alias for RemainingTime - this is the "ClockService.CurrentSeconds" of DESIGN.md §7.2.</summary>
    public float CurrentSeconds => RemainingTime;

    /// <summary>
    /// Multiplies tick speed on top of the existing mouse-speed multiplier.
    /// Phone/cards (Slow Hand, Rush, Robocalls) write this. Nothing else should.
    /// </summary>
    public float TickMultiplier { get; set; } = 1f;

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
        {
            StartCountdown();
            PauseCountdown();
        }
    }

    private void Update()
    {
        if (isSpeedOverridden)
        {
            overrideTimeRemaining -= Time.unscaledDeltaTime;
            if(overrideTimeRemaining <= 0f) isSpeedOverridden = false;
        }
        
        RunTimer();
    }

    public void StartCountdown()
    {
        if(IsGameOver)  return;
        IsRunning = true;
    }
    
    public void PauseCountdown() => IsRunning = false;
    
    public void ResumeCountdown()
    { 
        if(IsGameOver) return;
        IsRunning = true;
    }
    
    private void RunTimer()
    {
        if (!IsRunning || IsGameOver) return;
        
        RemainingTime -= Time.deltaTime * speedMult * TickMultiplier;

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            EndCountdown();
        }
        
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();
    }

    public void AddTime(float time)
    {
        if(IsGameOver || time <= 0f) return;
        
        RemainingTime += time;
        
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();
    }

    public void SubstractTime(float time)
    {
        if (IsGameOver || time <= 0f) return;
        
        RemainingTime = Mathf.Max(0f, RemainingTime - time);
        OnTimeChanged?.Invoke(RemainingTime);
        UpdateDisplay();

        if (RemainingTime <= GameManager.Instance.GameConfig.loseClock)
            EndCountdown();
    }
    
    public void OverrideSpeedMult(int forcedMult, float duration)
    {
        speedMult = forcedMult;
        isSpeedOverridden = true;
        overrideTimeRemaining = duration;
    }

    /// <summary>ClockService.AddSeconds(s, reason) of DESIGN.md §7.2 - Meter and egg timer only.</summary>
    public void AddSeconds(float seconds, string reason) => AddTime(seconds);

    /// <summary>ClockService.Spend(s, reason) of DESIGN.md §7.2 - blackjack ante, penalties.</summary>
    public void Spend(float seconds, string reason) => SubstractTime(seconds);

    public void SetSpeedMult(float rawSpeed)
    {
        if (isSpeedOverridden) return;
        
        if (speedMult < risingThresholds.Length && rawSpeed >= risingThresholds[speedMult])
            speedMult++;
        else if (speedMult > 1 && rawSpeed < fallingThresholds[speedMult - 1])
            speedMult--;
    }

    private void EndCountdown()
    {
        IsRunning = false;
        IsGameOver = true;
        OnCountdownFinished?.Invoke();
    }

    private void UpdateDisplay()
    {
        if (countdownText == null) return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, RemainingTime));
        countdownText.text = timeSpan.ToString(timeFormat);
    }
}
