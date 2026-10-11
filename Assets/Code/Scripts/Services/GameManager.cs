using System;
using System.Collections;
using DG.Tweening;
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
    [SerializeField] private float endPanelDelay = 1f;
    [SerializeField] private float endPanelFadeDuration = 0.6f;
    
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
        Time.timeScale = 1f;
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
        Time.timeScale = 0f;
        
        OnGameOver?.Invoke(reason);
        bool playing = CutsceneController.Instance != null
                       && CutsceneController.Instance.PlayEnding(reason, () => StartCoroutine(ShowEndGameDelayed(reason)));;

        if(!playing) ShowEndGame(reason);
    }
    
    private IEnumerator ShowEndGameDelayed(EndReason reason)
    {
        yield return new WaitForSecondsRealtime(endPanelDelay);
        ShowEndGame(reason);
    }
    
    private void ShowEndGame(EndReason reason)
    {
        if(endGameText != null) endGameText.text = GetEndGameText(reason);
        if (endGamePanel == null) return;
        
        if(!endGamePanel.TryGetComponent(out CanvasGroup group))
            group = endGamePanel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        if(endGameText != null) endGameText.transform.localScale = Vector3.one * 0.85f;
        endGamePanel.SetActive(true);
        
        Sequence fade = DOTween.Sequence().SetUpdate(true);
        fade.Join(group.DOFade(1f, endPanelFadeDuration));
        if (endGameText != null)
        {
            fade.Join(endGameText.transform.DOScale(1f, endPanelFadeDuration).SetEase(Ease.OutBack));
            fade.OnComplete(() =>
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            });
        }
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
