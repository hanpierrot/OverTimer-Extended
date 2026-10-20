using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The click-through tutorial panel. At the start of a run it shows the intro (once, stored in PlayerPrefs); after that
/// TutorialService reuses it for the first-time Phone / Shop / Delivery panels through Present(). The intro freezes the
/// whole game (GameManager.PauseGame, as before); a service panel pauses the clock only (Q5).
/// </summary>
public class TutorialPanel : MonoBehaviour, IPointerClickHandler
{
    private const string SeenKey = "TutorialSeen";

    [Serializable]
    public struct Step
    {
        [TextArea(2, 5)] public string text;
        public Sprite image;
    }

    [SerializeField] private Image stepImage;
    [SerializeField] private TMP_Text stepText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button skipButton;

    [SerializeField] private Step[] steps =
    {
        new Step { text = "<color=#FFD166>You have 2 minutes to live.</color>\nGet the clock to <color=#FFD166>10:00</color> before it hits <color=#FFD166>0:00</color>." },
        new Step { text = "<color=#FFD166>Click</color> things to use them. <color=#FFD166>Drag</color> things to move them.\nIf something doesn't respond, it may be blocked, so look for a hint." },
        new Step { text = "Everything on the desk can earn <color=#FFD166>money</color>.\nMoney alone doesn't save you." },
        new Step { text = "Click the <color=#FFD166>Meter</color> to turn money into time: <color=#FFD166>$1 = 1 second.</color>\nIt spends all your money at once." },
        new Step { text = "Click the <color=#FFD166>desk</color> to zoom in and work. Click the <color=#FFD166>door</color> when the bell rings.\nNow try your luck." },
    };

    private Step[] _active;
    private Action _onClosed;
    private bool _isIntro;
    private bool _clockHeld;
    private int _index;
    private bool _open;

    /// <summary>True while any panel (the intro or a service panel) is on screen.</summary>
    public bool IsOpen => _open;

    private void Awake()
    {
        if (skipButton != null) skipButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(SeenKey, 0) == 1 || steps.Length == 0)
        {
            if (!_open) gameObject.SetActive(false);
            return;
        }

        if (!_open) Open();
    }

    private void OnDisable()
    {
        ReleaseHolds();
    }

    private void Open()
    {
        _isIntro = true;
        _active = steps;
        _onClosed = null;
        _open = true;
        _index = 0;
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
        ShowStep();
    }

    /// <summary>A first-time tutorial from TutorialService: shows these steps, pauses the clock, calls onClosed when done.</summary>
    public void Present(Step[] panelSteps, Action onClosed)
    {
        if (_open || panelSteps == null || panelSteps.Length == 0) return;

        _isIntro = false;
        _active = panelSteps;
        _onClosed = onClosed;
        _open = true;
        _index = 0;
        gameObject.SetActive(true);
        if (ClockService.Instance != null) { ClockService.Instance.Pause(this); _clockHeld = true; }
        ShowStep();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_open) return;

        _index++;
        if (_index >= _active.Length) Close();
        else ShowStep();
    }

    private void ShowStep()
    {
        Step step = _active[_index];
        stepText.text = step.text;

        bool hasImage = step.image != null;
        stepImage.gameObject.SetActive(hasImage);
        if (hasImage) stepImage.sprite = step.image;

        if (progressText != null)
        {
            bool last = _index == _active.Length - 1;
            string end = _isIntro ? "Click to start" : "Click to close";
            progressText.text = $"{_index + 1}/{_active.Length}  -  {(last ? end : "Click to continue")}";
        }
    }

    private void Close()
    {
        if (!_open) return;

        _open = false;
        if (_isIntro)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }
        ReleaseHolds();

        var done = _onClosed;
        _onClosed = null;
        gameObject.SetActive(false);
        if (done != null) done();
    }

    private void ReleaseHolds()
    {
        if (_clockHeld && ClockService.Instance != null) ClockService.Instance.Resume(this);
        _clockHeld = false;
    }
}
