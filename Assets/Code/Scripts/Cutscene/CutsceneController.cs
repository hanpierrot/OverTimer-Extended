using System;
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
        if (!_endDirectors.TryGetValue(reason, out var director)) return false;
        Play(director, onFinished);
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
