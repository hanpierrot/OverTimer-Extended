using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The pieces of one PhoneListView row: background, icon, title, unread dot.</summary>
public class PhoneListRow : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private GameObject unreadDot;

    public void Set(PhoneListRowData d, Sprite backgroundSprite)
    {
        if (background.sprite != backgroundSprite) background.sprite = backgroundSprite;
        icon.sprite = d.icon;
        icon.enabled = d.icon != null;
        PhoneUi.SetText(title, d.title);
        PhoneUi.SetActive(unreadDot, d.unread);
    }
}
