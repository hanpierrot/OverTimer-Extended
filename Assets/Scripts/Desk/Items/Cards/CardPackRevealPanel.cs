using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPackRevealPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject packView;
    [SerializeField] private Button packButton;
    [SerializeField] private GameObject cardsGridView;
    [SerializeField] private CardRevealSlot[] slots;

    private int _resolvedCount;
    
    private void Awake()
    {
        packButton.onClick.AddListener(OnPackClicked);
        foreach (var slot in slots)
        {
            slot.Resolved += OnSlotResolved;
            slot.CollectedToCollection += OnSlotCollected;
        }
    }

    public void Begin(CardSO[] cards)
    {
        panel.SetActive(true);
        packView.SetActive(true);
        cardsGridView.SetActive(false);
        _resolvedCount = 0;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(cards[i]);
    }
    
    private void OnPackClicked()
    {
        packView.SetActive(false);
        cardsGridView.SetActive(true);
    }

    private void OnSlotResolved(CardRevealSlot slot)
    {
        _resolvedCount++;
        if (_resolvedCount >= slots.Length)
            panel.SetActive(false);
    }
    
    private void OnSlotCollected(CardSO card)
    {
        foreach (var slot in slots)
            slot.HideCollectionButtonIfSameCard(card);
    }
    
    public bool TryPlaceCard(CardSO card)
    {
        CardItem instance = CardHandManager.Instance != null ? CardHandManager.Instance.TryGetFreeCard() : null;
        if (instance == null) return false;

        instance.gameObject.SetActive(true);
        instance.Setup(card);
        
        if (CardHandManager.Instance != null)
            instance.transform.position = CardHandManager.Instance.GetRandomPlacementPosition();
        
        return true;
    }
}
