using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private GameConfig config;
    
    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text endGameText;
    
    [Header("Input Blockers")]
    [SerializeField] private GameObject[] inputBlockers;
    
    [Header("Camera")]
    [SerializeField] private CameraVerticalScroller cameraScroller;

    public GameConfig GameConfig => config;
    public bool IsGameOver { get; private set; }
    public EndReason LastEndReason { get; private set; }
    
    public event Action<EndReason> OnGameOver;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ClockService.Instance.OnTimeChanged += HandleTimeChanged;
        ClockService.Instance.OnCountdownFinished += HandleCountdownFinished;
    }

    private void Update()
    {
        if (IsGameOver) return;
        
        bool anyBlockerActive = false;
        foreach (var go in inputBlockers)
        {
            if (go != null && go.activeInHierarchy)
            {
                anyBlockerActive = true;
                break;
            }
        }

        InputManager.Instance.IsInputEnabled = !anyBlockerActive;
        if (cameraScroller != null) cameraScroller.SetButtonsEnabled(!anyBlockerActive);
    }

    private void HandleTimeChanged(float currentSeconds)
    {
        if (IsGameOver) return;

        if (currentSeconds >= config.winClock)
            GameOver(EndReason.ClockWin);
    }

    private void HandleCountdownFinished()
    {
        if (IsGameOver) return;

        GameOver(EndReason.ClockLoss);
    }
    
    public void GameOver(EndReason reason)
    {
        IsGameOver = true;
        LastEndReason = reason;
        InputManager.Instance.IsInputEnabled = false;
        ClockService.Instance.PauseCountdown();

        ShowEndGame(reason);
        OnGameOver?.Invoke(reason);
    }
    
    private void ShowEndGame(EndReason reason)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);
        if (endGameText != null) endGameText.text = GetEndGameText(reason);
    }
    
    private string GetEndGameText(EndReason reason)
    {
        switch (reason)
        {
            case EndReason.ClockWin: return "The Timer reaches 10 minutes.";
            case EndReason.SetCompletionWin: return "The MASCH make you win!";
            case EndReason.FinalCountdownWin: return "You beat the Final Countdown!";
            case EndReason.ClockLoss: return "Time out.";
            case EndReason.FinalCountdownLoss: return "The countdown betrayed you.";
            case EndReason.CreepyJesterLoss: return "The Pierrot found you...";
            default: return "";
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
