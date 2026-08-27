using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private GameConfig config;
    
    [Header("Win/Lose UI")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    
    [Header("Input Blockers")]
    [SerializeField] private GameObject[] inputBlockers;

    public GameConfig GameConfig => config;
    public bool IsGameOver { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CountdownTimer.Instance.OnTimeChanged += HandleTimeChanged;
        CountdownTimer.Instance.OnCountdownFinished += HandleCountdownFinished;
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
    }

    private void HandleTimeChanged(float currentSeconds)
    {
        if (IsGameOver) return;

        if (currentSeconds >= config.winClock)
        {
            Win();
        }
    }

    private void HandleCountdownFinished()
    {
        if (IsGameOver) return;

        Lose();
    }
    
    private void Win()
    {
        IsGameOver = true;
        InputManager.Instance.IsInputEnabled = false;

        if (winScreen != null) winScreen.SetActive(true);
    }

    private void Lose()
    {
        IsGameOver = true;
        InputManager.Instance.IsInputEnabled = false;

        if (loseScreen != null) loseScreen.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
