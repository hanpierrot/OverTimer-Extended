using System;
using UnityEngine;
using UnityEngine.UI;

public class CardRevealSlot : MonoBehaviour
{
    [SerializeField] private CardPackRevealPanel panel;
    [SerializeField] private Image cardImage;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button placeButton;
    [SerializeField] private Button collectionButton;
    [SerializeField] private int sellUnrevealedValue = 10;
    
    public event Action<CardRevealSlot> Resolved;
    public event Action<CardSO> CollectedToCollection;

    private CardSO _card;
    private bool _revealed;
    private bool _resolved;

    private void Awake()
    {
        cardButton.onClick.AddListener(OnCardClicked);
        sellButton.onClick.AddListener(OnSellClicked);
        placeButton.onClick.AddListener(OnPlaceClicked);
        collectionButton.onClick.AddListener(OnCollectionClicked);
    }
    
    public void Setup(CardSO card)
    {
        _card = card;
        _revealed = false;
        _resolved = false;

        cardImage.sprite = card.backSprite != null ? card.backSprite : card.image;
        placeButton.gameObject.SetActive(false);
        collectionButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(true);

        gameObject.SetActive(true);
    }

    private void OnCardClicked()
    {
        if (_resolved) return;

        if (!_revealed) { Reveal(); return; }

        CardPanel.Instance?.Show(_card);
    }
    
    private void Reveal()
    {
        _revealed = true;
        cardImage.sprite = _card.image;

        bool isDebuff = _card is EffectCardSO effect && effect.isDebuff;
        bool alreadyCollected = CardCollectionManager.Instance != null && CardCollectionManager.Instance.IsCollected(_card);
        
        placeButton.gameObject.SetActive(true);
        collectionButton.gameObject.SetActive(!isDebuff && !alreadyCollected);
        sellButton.gameObject.SetActive(!isDebuff);

        bool hasRoom = isDebuff || CardHandManager.Instance == null || CardHandManager.Instance.HasRoom;
        placeButton.interactable = hasRoom;
    }
    
    private void OnSellClicked()
    {
        int value = _revealed ? _card.pawnValue : sellUnrevealedValue;
        MoneyService.Instance.Add(value, "card sale");
        Resolve();
    }

    private void OnPlaceClicked()
    {
        if (panel != null && panel.TryPlaceCard(_card)) Resolve();
    }

    private void OnCollectionClicked()
    {
        if (CardCollectionManager.Instance != null) CardCollectionManager.Instance.Register(_card);
        CollectedToCollection?.Invoke(_card);
        Resolve();
    }
    
    public void HideCollectionButtonIfSameCard(CardSO card)
    {
        if (_resolved || !_revealed) return;
        if (_card != card) return;

        collectionButton.gameObject.SetActive(false);
    }
    
    private void Resolve()
    {
        _resolved = true;
        gameObject.SetActive(false);
        Resolved?.Invoke(this);
    }
}
