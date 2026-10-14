using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The "what do I pot next" strip: one ball image per ball still in the queue, head first, in a horizontal layout group
/// (the same idea as Blackjack's card rows). The head ball is drawn bigger. A ball that has to go in a called pocket
/// carries a small badge with that pocket's name, already mirrored to match what the player sees.
/// The ball art comes from the same BallSpriteSet the table uses.
public class BallQueueView : MonoBehaviour
{
    [SerializeField] private RectTransform slotTemplate;         // inactive: Image + LayoutElement + a child TMP named "Badge"
    [SerializeField] private BallSpriteSet sprites;
    [SerializeField] private float headSize = 46f;
    [SerializeField] private float otherSize = 34f;

    private readonly List<RectTransform> _slots = new List<RectTransform>();

    /// pocketLabel returns the badge text for a ball id, or null when the ball has no called pocket.
    public void Show(IReadOnlyList<string> queue, Func<string, string> pocketLabel)
    {
        if (slotTemplate == null) return;
        slotTemplate.gameObject.SetActive(false);

        for (int i = 0; i < queue.Count; i++)
        {
            if (i >= _slots.Count) _slots.Add(Instantiate(slotTemplate, transform));
            RectTransform slot = _slots[i];
            slot.gameObject.name = "Queue " + queue[i];
            slot.gameObject.SetActive(true);

            float size = i == 0 ? headSize : otherSize;
            var element = slot.GetComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;

            var image = slot.GetComponent<Image>();
            image.sprite = sprites != null ? sprites.Get(queue[i]) : null;
            image.raycastTarget = false;

            var badge = slot.Find("Badge");
            if (badge != null)
            {
                string label = pocketLabel != null ? pocketLabel(queue[i]) : null;
                badge.gameObject.SetActive(!string.IsNullOrEmpty(label));
                var text = badge.GetComponentInChildren<TMP_Text>(true);
                if (text != null) text.text = label;
            }
        }
        for (int i = queue.Count; i < _slots.Count; i++) _slots[i].gameObject.SetActive(false);
    }
}
