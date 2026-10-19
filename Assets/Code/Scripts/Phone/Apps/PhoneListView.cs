using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row of a PhoneListView.</summary>
public struct PhoneListRowData
{
    public string title;
    public Sprite icon;      // small icon left of the title (null = none)
    public bool unread;      // shows the unread dot
    public string detail;    // short text on the right of the row (a price, a status); needs a Detail label on the row
}

/// <summary>
/// A scrolling list of rows (the Inbox's mail list, the Call app's history). Rows are cloned from a template on demand,
/// the clicked row is highlighted with the "selected" sprite, and a scroll bar with a small draggable dot sits beside it.
/// The mouse wheel scrolls too. The screen that owns it decides what a click means (it subscribes to Clicked).
/// The scroll bar is kept in sync by hand so the handle stays a fixed-size dot instead of stretching with the content.
/// </summary>
public class PhoneListView : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform content;
    [SerializeField] private Button rowTemplate;
    [SerializeField] private Scrollbar scrollbar;
    [Tooltip("Row sprites: normal / highlighted (selected).")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    public event Action<int> Clicked;

    private readonly List<Button> _rows = new List<Button>();
    private bool _syncing;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        scrollbar.onValueChanged.AddListener(OnBarMoved);
        scroll.onValueChanged.AddListener(OnScrolled);
    }

    /// <summary>Shows these rows; selectedIndex is highlighted (-1 = none). Keeps the scroll position.</summary>
    public void SetRows(IReadOnlyList<PhoneListRowData> data, int selectedIndex)
    {
        for (int i = 0; i < data.Count; i++)
        {
            if (i >= _rows.Count) _rows.Add(MakeRow(i));
            var row = _rows[i];
            PhoneUi.SetActive(row.gameObject, true);
            var parts = row.GetComponent<PhoneListRow>();
            parts.Set(data[i], i == selectedIndex ? selectedSprite : normalSprite);
        }
        for (int i = data.Count; i < _rows.Count; i++) PhoneUi.SetActive(_rows[i].gameObject, false);
    }

    private Button MakeRow(int index)
    {
        var button = Instantiate(rowTemplate, content);
        button.gameObject.name = "Row" + index;
        button.onClick.AddListener(() => Clicked?.Invoke(index));
        return button;
    }

    private void Update()
    {
        // The scroll dot only exists while the rows overflow the list (the dark track stays as decoration).
        var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        bool overflow = content.rect.height > viewport.rect.height + 0.5f;
        var handle = scrollbar.handleRect != null ? scrollbar.handleRect.GetComponent<Image>() : null;
        if (handle != null && handle.enabled != overflow) handle.enabled = overflow;
        if (scrollbar.interactable != overflow) scrollbar.interactable = overflow;
        if (!overflow && scroll.verticalNormalizedPosition < 1f) scroll.verticalNormalizedPosition = 1f;
    }

    private void OnScrolled(Vector2 pos)
    {
        if (_syncing) return;
        _syncing = true;
        scrollbar.value = pos.y;
        _syncing = false;
    }

    private void OnBarMoved(float value)
    {
        if (_syncing) return;
        _syncing = true;
        scroll.verticalNormalizedPosition = value;
        _syncing = false;
    }
}
