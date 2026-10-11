using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneController : MonoBehaviour
{
    public static CutsceneController Instance { get; private set; }
    [Serializable]
    private struct EndCutscene
    {
        public EndReason reason;
        public PlayableDirector director;
    }
    
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private EndCutscene[] endCutscenes;
    [SerializeField] private PlayableDirector finalCountdownDirector;
    
    [Header("Clock End")]
    [SerializeField] private GameObject clockEndRoot;
    [SerializeField] private TimerTicker clockTicker;
    [SerializeField] private int clockExtraSeconds = 3;

    private readonly Dictionary<EndReason, PlayableDirector> _endDirectors = new();
    private PlayableDirector _current;
    private Action _onFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var c in endCutscenes)
        {
            if (c.director == null) continue;
            _endDirectors[c.reason] = c.director;
            Configure(c.director);
        }
        if (finalCountdownDirector != null) Configure(finalCountdownDirector);
    }

    private void Configure(PlayableDirector director)
    {
        director.playOnAwake = false;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
    }
    
    public bool PlayEnding(EndReason reason, Action onFinished)
    {
        if (reason == EndReason.ClockWin || reason == EndReason.ClockLoss)
            return PlayClockEnding(reason, onFinished);
        
        if (!_endDirectors.TryGetValue(reason, out var director)) return false;
        PlayAndHold(director, onFinished);
        return true;
    }

    public void StopAll()
    {
        var callback = _current != null ? _onFinished : null;
        
        StopAllCoroutines();
        if(_current != null) _current.stopped -= HandleStopped;
        _current = null;
        _onFinished = null;

        foreach (var d in _endDirectors.Values)
            ResetDirector(d);
        if (finalCountdownDirector != null) ResetDirector(finalCountdownDirector);

        if (cutsceneRoot != null) cutsceneRoot.SetActive(false);
        if (clockEndRoot != null) clockEndRoot.SetActive(false);
        
        callback?.Invoke();
    }

    private static void ResetDirector(PlayableDirector director)
    {
        director.Stop();
        director.time = 0;
    }

    private bool PlayClockEnding(EndReason reason, Action onFinished)
    {
        if (clockEndRoot == null || clockTicker == null) return false;
        
        var config = GameManager.Instance.GameConfig;
        bool win = reason == EndReason.ClockWin;
        int end = Mathf.RoundToInt(win ? config.winClock : config.loseClock);
        int start = win ? end - clockExtraSeconds : end + clockExtraSeconds;
        
        clockEndRoot.SetActive(true);
        clockTicker.Play(start, end, () => onFinished?.Invoke());
        return true;
    }
    
    public void PlayFinalCountdownIntro()
    {
        if (finalCountdownDirector == null) return;

        ClockService.Instance.PauseCountdown();
        Play(finalCountdownDirector, () =>
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
                ClockService.Instance.ResumeCountdown();
        });
    }
    
    private void Play(PlayableDirector director, Action onFinished)
    {
        if (_current != null)
        {
            _current.stopped -= HandleStopped;
            _current.Stop();
        }

        _current = director;
        _onFinished = onFinished;

        if (cutsceneRoot != null) cutsceneRoot.SetActive(true);
        director.stopped += HandleStopped;
        director.time = 0;
        director.Play();
    }

    private void PlayAndHold(PlayableDirector director, Action onFinished)
    {
        if (_current != null)
        {
            _current.stopped -= HandleStopped;
            _current.Stop();
            _current = null;
        }
        
        if(cutsceneRoot != null) cutsceneRoot.SetActive(true);
        
        director.extrapolationMode = DirectorWrapMode.Hold;
        director.time = 0;
        director.Play();
        StartCoroutine(WaitForEnd(director, onFinished));
    }

    private IEnumerator WaitForEnd(PlayableDirector director, Action onFinished)
    {
        yield return new WaitUntil(() => director.time >= director.duration - 0.02f);
        onFinished?.Invoke();
    }

    private void HandleStopped(PlayableDirector director)
    {
        director.stopped -= HandleStopped;
        _current = null;
        if (cutsceneRoot != null) cutsceneRoot.SetActive(false);

        var callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }
}
