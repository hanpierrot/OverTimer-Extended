using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Inbox app window: a scrolling list of messages (newest first, an icon and an unread dot per row) with the opened
/// message on the right. Clicking a row highlights it, marks it read and shows it. Messages with an actionId get a
/// button: "app:taxes" opens the Taxes app; "license:..." (Batch 2) is shown but does nothing yet.
/// </summary>
public class InboxScreen : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private PhoneListView list;
    [SerializeField] private Sprite mailIcon;
    [Tooltip("Used for tax bills and missed calls.")]
    [SerializeField] private Sprite alertIcon;

    [Header("Reading pane")]
    [SerializeField] private TMP_Text fromLabel;
    [SerializeField] private TMP_Text readBody;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;

    [SerializeField] private PhoneHome home;

    private readonly List<PhoneListRowData> _rows = new List<PhoneListRowData>();
    private int _selectedId = -1;       // message id (indices shift when a new message lands)
    private int _signature = -1;

    private void Awake()
    {
        list.Clicked += Select;
        actionButton.onClick.AddListener(RunAction);
    }

    private void OnEnable() => _signature = -1;

    private void Update()
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;

        var all = phone.Inbox.All;
        int sig = all.Count * 1000 + phone.Inbox.Unread * 10 + _selectedId * 100000 + (ShopService.Instance != null ? ShopService.Instance.Version * 7919 : 0);
        if (sig == _signature) return;
        _signature = sig;

        _rows.Clear();
        int selectedIndex = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var m = all[i];
            if (m.id == _selectedId) selectedIndex = i;
            bool alert = m.kind == MessageKind.Tax || m.kind == MessageKind.MissedCall;
            _rows.Add(new PhoneListRowData { title = m.title, icon = alert ? alertIcon : mailIcon, unread = !m.read });
        }
        list.SetRows(_rows, selectedIndex);

        if (selectedIndex < 0)
        {
            PhoneUi.SetText(fromLabel, "");
            PhoneUi.SetText(readBody, all.Count == 0 ? "No messages." : "Pick a message.");
            PhoneUi.SetActive(actionButton.gameObject, false);
        }
        else
        {
            var m = all[selectedIndex];
            PhoneUi.SetText(fromLabel, Sender(m.kind));
            PhoneUi.SetText(readBody, m.title + "\n\n" + m.body);
            ShowAction(m);
        }
    }

    private static string Sender(MessageKind kind)
    {
        switch (kind)
        {
            case MessageKind.Tax: return "ITRS";
            case MessageKind.MissedCall: return "PHONE";
            case MessageKind.License:
            case MessageKind.Sale: return "SHOP";
            case MessageKind.Delivery: return "COURIER";
            case MessageKind.Junk: return "???";
            default: return "SYSTEM";
        }
    }

    private void Select(int index)
    {
        var phone = PhoneController.Instance;
        if (phone == null || index >= phone.Inbox.All.Count) return;
        var m = phone.Inbox.All[index];
        _selectedId = m.id;
        phone.Inbox.MarkRead(m);
        _signature = -1;
    }

    private void ShowAction(InboxMessage m)
    {
        string id = m.actionId;
        bool has = !string.IsNullOrEmpty(id);
        PhoneUi.SetActive(actionButton.gameObject, has);
        if (!has) return;
        if (id.StartsWith("license:"))
        {
            PhoneUi.SetText(actionLabel, m.actionDone ? "Activated" : "Activate");
            actionButton.interactable = !m.actionDone;
            return;
        }
        PhoneUi.SetText(actionLabel, "Open");
        actionButton.interactable = id.StartsWith("app:");
    }

    private void RunAction()
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;
        foreach (var m in phone.Inbox.All)
        {
            if (m.id != _selectedId) continue;
            if (m.actionId == "app:taxes" && home != null) home.OpenApp("taxes");
            else if (m.actionId != null && m.actionId.StartsWith("license:") && !m.actionDone && ShopService.Instance != null)
            {
                if (ShopService.Instance.ActivateLicense(m.actionId.Substring("license:".Length))) m.actionDone = true;
                _signature = -1;
            }
            return;
        }
    }
}
