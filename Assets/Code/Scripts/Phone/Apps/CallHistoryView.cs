using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The Call app's history: a scrolling list of past calls (an icon for how it ended) with the selected call's
/// transcript on the right. It is shown by CallScreen whenever no call is ringing or active.
/// </summary>
public class CallHistoryView : MonoBehaviour
{
    [SerializeField] private PhoneListView list;
    [SerializeField] private Sprite answeredIcon;
    [Tooltip("Missed and declined calls.")]
    [SerializeField] private Sprite missedIcon;
    [SerializeField] private TMP_Text fromLabel;
    [SerializeField] private TMP_Text body;

    private readonly List<PhoneListRowData> _rows = new List<PhoneListRowData>();
    private readonly StringBuilder _text = new StringBuilder();
    private int _selectedId = -1;
    private int _signature = -1;

    private void Awake() => list.Clicked += Select;
    private void OnEnable() => _signature = -1;

    private void Update()
    {
        var phone = PhoneController.Instance;
        if (phone == null) return;

        var history = phone.CallHistory;
        int sig = phone.Version * 1000 + history.Count + _selectedId * 100000;
        if (sig == _signature) return;
        _signature = sig;

        _rows.Clear();
        int selectedIndex = -1;
        for (int i = 0; i < history.Count; i++)
        {
            var r = history[i];
            if (r.id == _selectedId) selectedIndex = i;
            _rows.Add(new PhoneListRowData { title = r.name, icon = r.outcome == CallEnd.Answered ? answeredIcon : missedIcon });
        }
        list.SetRows(_rows, selectedIndex);

        if (selectedIndex < 0)
        {
            PhoneUi.SetText(fromLabel, "");
            PhoneUi.SetText(body, history.Count == 0 ? "No calls yet." : "Pick a call.");
            return;
        }

        var rec = history[selectedIndex];
        _text.Clear();
        _text.Append(rec.outcome == CallEnd.Answered ? "Answered" : rec.outcome == CallEnd.Declined ? "Declined" : "Missed");
        if (!string.IsNullOrEmpty(rec.result)) _text.Append("  ").Append(rec.result);
        if (rec.transcript.Count > 0) _text.Append('\n');
        foreach (var line in rec.transcript) _text.Append('\n').Append(line);
        PhoneUi.SetText(fromLabel, rec.name);
        PhoneUi.SetText(body, _text.ToString());
    }

    private void Select(int index)
    {
        var phone = PhoneController.Instance;
        if (phone == null || index >= phone.CallHistory.Count) return;
        _selectedId = phone.CallHistory[index].id;
        _signature = -1;
    }
}
