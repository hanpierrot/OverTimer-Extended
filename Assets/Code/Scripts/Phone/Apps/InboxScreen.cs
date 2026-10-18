using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Inbox app window: newest message first, tap one to read it (that marks it read). Messages with an actionId get a
/// button: "app:taxes" opens the Taxes app; "license:..." (Batch 2) is shown but does nothing yet. Shows the newest few
/// rows (no scrolling yet).
/// </summary>
public class InboxScreen : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Button[] rowButtons;
    [SerializeField] private TMP_Text[] rowLabels;
    [SerializeField] private GameObject[] unreadDots;

    [Header("Reading pane")]
    [SerializeField] private TMP_Text readTitle;
    [SerializeField] private TMP_Text readBody;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;

    [SerializeField] private PhoneHome home;

    private InboxMessage _selected;
    private int _signature = -1;

    private void Awake()
    {
        for (int i = 0; i < rowButtons.Length; i++)
        {
            int index = i;
            rowButtons[i].onClick.AddListener(() => Select(index));
        }
        actionButton.onClick.AddListener(RunAction);
    }

    private void OnEnable() => _signature = -1;

    private void Update()
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;

        var all = phone.Inbox.All;
        int sig = all.Count * 1000 + phone.Inbox.Unread * 10 + (_selected != null ? _selected.id * 100000 : 0);
        if (sig == _signature) return;
        _signature = sig;

        for (int i = 0; i < rowButtons.Length; i++)
        {
            bool has = i < all.Count;
            PhoneUi.SetActive(rowButtons[i].gameObject, has);
            if (!has) continue;
            PhoneUi.SetText(rowLabels[i], all[i].title);
            PhoneUi.SetActive(unreadDots[i], !all[i].read);
        }

        if (_selected == null)
        {
            PhoneUi.SetText(readTitle, all.Count == 0 ? "No messages." : "Pick a message.");
            PhoneUi.SetText(readBody, "");
            PhoneUi.SetActive(actionButton.gameObject, false);
        }
        else
        {
            PhoneUi.SetText(readTitle, _selected.title);
            PhoneUi.SetText(readBody, _selected.body);
            ShowAction();
        }
    }

    private void Select(int index)
    {
        var phone = PhoneController.Instance;
        if (phone == null || index >= phone.Inbox.All.Count) return;
        _selected = phone.Inbox.All[index];
        phone.Inbox.MarkRead(_selected);
        _signature = -1;
    }

    private void ShowAction()
    {
        string id = _selected.actionId;
        bool has = !string.IsNullOrEmpty(id);
        PhoneUi.SetActive(actionButton.gameObject, has);
        if (!has) return;
        PhoneUi.SetText(actionLabel, id.StartsWith("app:") ? "Open" : "Activate");
        actionButton.interactable = id.StartsWith("app:");          // license activation arrives with the Shop (Batch 2)
    }

    private void RunAction()
    {
        if (_selected == null || string.IsNullOrEmpty(_selected.actionId)) return;
        if (_selected.actionId == "app:taxes" && home != null) home.OpenApp("taxes");
    }
}
