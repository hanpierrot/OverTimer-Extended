using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPanel : MonoBehaviour
{
    public static CardPanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text rarityLabel;
    [SerializeField] private TMP_Text pawnValueLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (panel != null) panel.SetActive(false);
    }

    private bool _paused;   // ResumeGame is a plain counter: only release a pause this panel really took, never another panel's

    private void OnEnable()
    {
        if (_paused || GameManager.Instance == null) return;
        _paused = true;
        GameManager.Instance.PauseGame();
    }

    private void OnDisable()
    {
        if (!_paused) return;
        _paused = false;
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void Show(CardSO data, Sprite spriteOverride = null)
    {
        if (data == null) return;

        cardImage.sprite = spriteOverride != null ? spriteOverride : data.image;
        nameLabel.text = data.cardName;
        rarityLabel.text = "Rarity: " + data.rarity;
        pawnValueLabel.text = data.pawnValue + "$";
        descriptionLabel.text = data.description;

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
