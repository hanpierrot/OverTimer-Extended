using UnityEngine;
using UnityEngine.UI;

public class RunHistoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private RunHistoryEntryView entryPrefab;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void Open()
    {
        Refresh();
        if (panel != null) panel.SetActive(true);
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        if (RunStatsService.Instance == null) return;

        foreach (var record in RunStatsService.Instance.LoadHistory())
        {
            var entry = Instantiate(entryPrefab, entryContainer);
            entry.Setup(record);
        }
    }
}
