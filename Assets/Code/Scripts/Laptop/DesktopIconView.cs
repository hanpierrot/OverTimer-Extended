using System;
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

    public event Action<string> Opened;

    private void Awake()
    {
        _background = GetComponent<Image>();
        _background.color = normalColor;
    }

    private void OnDisable()
    {
        if (_selected == this) _selected = null;
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
