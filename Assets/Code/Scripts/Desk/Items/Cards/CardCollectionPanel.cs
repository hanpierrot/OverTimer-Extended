using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionPanel : MonoBehaviour
{
    [SerializeField] private CardCatalogSO catalog;
    [SerializeField] private int cardsPerPage = 6;
    
    [SerializeField] private CardCollectionSlot[] leftPageSlots;
    [SerializeField] private CardCollectionSlot[] rightPageSlots;
    
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int _currentSpread;

    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)catalog.allCards.Length / cardsPerPage));
    private int TotalSpreads => Mathf.CeilToInt(TotalPages / 2f);

    private void Awake()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevSpread);
        if (nextButton != null) nextButton.onClick.AddListener(NextSpread);
    }

    private void OnEnable()
    {
        _currentSpread = 0;
        RefreshSpread();
    }

    private void RefreshSpread()
    {
        int leftPageIndex = _currentSpread * 2;
        int rightPageIndex = leftPageIndex + 1;
        
        FillPage(leftPageSlots, leftPageIndex);
        FillPage(rightPageSlots, rightPageIndex);

        bool showNav = TotalPages > 2;
        if (prevButton != null) prevButton.gameObject.SetActive(showNav && _currentSpread > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(showNav && _currentSpread < TotalSpreads - 1);
    }

    private void FillPage(CardCollectionSlot[] slots, int index)
    {
        int start = index * cardsPerPage;
        bool pageExists = index < TotalPages;

        for (int i = 0; i < slots.Length; i++)
        {
            int cardIndex = start + i;
            if (pageExists && cardIndex < catalog.allCards.Length)
            {
                CardSO card =  catalog.allCards[cardIndex];
                bool collected = CardCollectionManager.Instance != null && CardCollectionManager.Instance.IsCollected(card);
                slots[i].Setup(card, collected);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }
    
    public void NextSpread()
    {
        if (_currentSpread >= TotalSpreads - 1) return;
        _currentSpread++;
        RefreshSpread();
    }

    public void PrevSpread()
    {
        if (_currentSpread <= 0) return;
        _currentSpread--;
        RefreshSpread();
    }
}
