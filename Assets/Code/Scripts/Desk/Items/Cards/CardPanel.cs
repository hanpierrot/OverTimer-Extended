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
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (panel != null) panel.SetActive(false);
    }
    
    public void Show(CardSO data)
    {
        if (data == null) return;

        cardImage.sprite = data.image;
        nameLabel.text = data.cardName;
        rarityLabel.text = "Rarity: " + data.rarity;
        descriptionLabel.text = data.description;

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
