using System;
using System.Collections;
using TMPro;
using UnityEngine;

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
    public bool IsRunning { get; private set; }
    public bool IsGameOver { get; private set; }

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
            //PauseCountdown();
        }
    }

    private void Update()
    {
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
        
        RemainingTime -= Time.deltaTime * TickMultiplier;

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

    /// <summary>ClockService.AddSeconds(s, reason) of DESIGN.md §7.2 - Meter and egg timer only.</summary>
    public void AddSeconds(float seconds, string reason) => AddTime(seconds);

    /// <summary>ClockService.Spend(s, reason) of DESIGN.md §7.2 - blackjack ante, penalties.</summary>
    public void Spend(float seconds, string reason) => SubstractTime(seconds);

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
