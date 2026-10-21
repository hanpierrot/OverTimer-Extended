using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The click-through tutorial shown once at the start of the first run (stored in PlayerPrefs). It freezes the whole game
/// (GameManager.PauseGame) until it is closed. Everything the Phone, Shop and Delivery add is explained with hints instead.
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

    private int _index;
    private bool _open;

    /// <summary>Cheat menu: the intro will show again on the next run.</summary>
    public static void ResetSeen()
    {
        PlayerPrefs.DeleteKey(SeenKey);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (skipButton != null) skipButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(SeenKey, 0) == 1 || steps.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        Open();
    }

    private void OnDisable()
    {
        // Never leave the game paused if the panel is switched off while it is open.
        if (!_open) return;
        _open = false;
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    private void Open()
    {
        _open = true;
        _index = 0;
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
        ShowStep();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_open) return;

        _index++;
        if (_index >= steps.Length) Close();
        else ShowStep();
    }

    private void ShowStep()
    {
        Step step = steps[_index];
        stepText.text = step.text;

        bool hasImage = step.image != null;
        stepImage.gameObject.SetActive(hasImage);
        if (hasImage) stepImage.sprite = step.image;

        if (progressText != null)
        {
            bool last = _index == steps.Length - 1;
            progressText.text = $"{_index + 1}/{steps.Length}  -  {(last ? "Click to start" : "Click to continue")}";
        }
    }

    private void Close()
    {
        if (!_open) return;

        _open = false;
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        gameObject.SetActive(false);
    }
}
