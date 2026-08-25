using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One clickable app icon on the laptop's home screen (the Apps grid). A single
/// click selects it - default window-select highlight, like a desktop icon -
/// a second click within Unity's double-click window opens the app. Icons
/// without a built app yet (e.g. Blackjack, for now) can still be selected;
/// LaptopController.RequestOpen just no-ops if nothing matches the name.
/// </summary>
[RequireComponent(typeof(Image))]
public class DesktopIconView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string appName;
    [SerializeField] private Color selectedColor = new Color(0.68f, 0.85f, 1f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private Image _background;
    private static DesktopIconView _selected;
    
    private static readonly Dictionary<string, DesktopIconView> s_icons = new Dictionary<string, DesktopIconView>();
    private static readonly HashSet<string> s_pendingHidden = new HashSet<string>();

    public event Action<string> Opened;

    private void Awake()
    {
        _background = GetComponent<Image>();
        _background.color = normalColor;
        
        s_icons[appName] = this;
        if (s_pendingHidden.Contains(appName)) gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_selected == this) _selected = null;
    }

    private void OnDestroy()
    {
        if (s_icons.TryGetValue(appName, out var current) && current == this)
            s_icons.Remove(appName);
    }
    
    public static void SetVisible(string appName, bool visible)
    {
        if (visible) s_pendingHidden.Remove(appName);
        else s_pendingHidden.Add(appName);

        if (s_icons.TryGetValue(appName, out var icon) && icon != null)
            icon.gameObject.SetActive(visible);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2)
        {
            Opened?.Invoke(appName);
            return;
        }

        Select();
    }

    private void Select()
    {
        if (_selected != null && _selected != this) _selected.Deselect();
        _selected = this;
        _background.color = selectedColor;
    }

    private void Deselect() => _background.color = normalColor;
}
