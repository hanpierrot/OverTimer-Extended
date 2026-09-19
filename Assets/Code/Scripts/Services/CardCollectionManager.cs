using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardCollectionManager : MonoBehaviour
{
    public static CardCollectionManager Instance { get; private set; }
    
    [SerializeField] private string cardResourcesPath = "CardSO";
    
    private const string SaveKey = "CardCollection_UnlockedIds"; 

    private readonly HashSet<CardSO> _collected = new HashSet<CardSO>();
    private Dictionary<string, CardSO> _cardById;
    public event Action<CardSO> NewCardRegistered;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    private void Start()
    {
        BuildLookup();
        LoadPersisted();
    }
    
    private void BuildLookup()
    {
        _cardById = new Dictionary<string, CardSO>();
        foreach (var card in Resources.LoadAll<CardSO>(cardResourcesPath))
            _cardById[card.name] = card;
    }

    private void LoadPersisted()
    {
        string saved = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(saved)) return;

        foreach (var id in saved.Split(','))
            if (_cardById.TryGetValue(id, out var card))
                _collected.Add(card);
    }
    
    private void SavePersisted()
    {
        string joined = string.Join(",", _collected.Select(c => c.name));
        PlayerPrefs.SetString(SaveKey, joined);
        PlayerPrefs.Save();
    }

    public bool IsCollected(CardSO card) => _collected.Contains(card);
    
    public bool Register(CardSO card)
    {
        bool isNew = _collected.Add(card);
        if (isNew)
        {
            SavePersisted();
            NewCardRegistered?.Invoke(card);
        }
        return isNew;
    }
}
