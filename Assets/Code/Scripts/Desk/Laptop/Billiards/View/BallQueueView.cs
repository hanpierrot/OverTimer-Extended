using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The "what do I pot next" strip: one ball image per ball still in the queue, head first, in a horizontal layout group
/// (the same idea as Blackjack's card rows). The head ball is drawn bigger and carries a pulsing halo, the last ball of
/// the queue has a gold frame, and a ball that has to go in a called pocket carries a badge with that pocket's name,
/// already mirrored to match what the player sees. The ball art comes from the same BallSpriteSet the table uses.
///
/// Apply() animates a change: balls that were legally potted slide up and fade out, the rest slide into their new
/// places, and a re-spotted ball shakes. Tweens are killed when the view is disabled, so closing the app mid-animation
/// leaves nothing running.
public class BallQueueView : MonoBehaviour
{
    [SerializeField] private RectTransform slotTemplate;         // inactive: Image + LayoutElement + a child TMP named "Badge"
    [SerializeField] private BallSpriteSet sprites;
    [SerializeField] private float headSize = 46f;
    [SerializeField] private float otherSize = 34f;
    [SerializeField] private GameObject hideWhenEmpty;           // the "Order:" label: only shown while there is a queue
    [SerializeField] private float slideSeconds = 0.25f;
    [SerializeField] private Color haloColor = new Color(1f, 0.93f, 0.45f, 1f);
    [SerializeField] private Color finalColor = new Color(1f, 0.78f, 0.15f, 1f);

    private readonly List<RectTransform> _slots = new List<RectTransform>();
    private readonly List<RectTransform> _finalFrames = new List<RectTransform>();
    private readonly List<string> _ids = new List<string>();         // what the slots currently show, in order
    private readonly List<RectTransform> _ghosts = new List<RectTransform>();
    private RectTransform _halo;

    /// A plain refresh with no animation (opening the app, a new table).
    public void Show(IReadOnlyList<string> queue, Func<string, string> pocketLabel) => Apply(queue, pocketLabel, null, null);

    /// pocketLabel returns the badge text for a ball id, or null when the ball has no called pocket.
    /// popped: balls legally potted by the last shot (a prefix of the old queue). respot: balls put back on the table.
    public void Apply(IReadOnlyList<string> queue, Func<string, string> pocketLabel,
                      IReadOnlyList<string> popped, IReadOnlyList<string> respot)
    {
        if (slotTemplate == null) return;
        slotTemplate.gameObject.SetActive(false);
        KillAnimations();

        // Only an update that follows a shot animates. Opening the app, a new table or a forced table is a plain redraw,
        // otherwise a ball with the same id would slide in from where the previous table had it.
        bool animate = gameObject.activeInHierarchy && (popped != null || respot != null);
        var oldWorld = new Dictionary<string, Vector3>();
        if (animate)
            for (int i = 0; i < _ids.Count && i < _slots.Count; i++)
                if (_slots[i].gameObject.activeSelf) oldWorld[_ids[i]] = _slots[i].TransformPoint(_slots[i].rect.center);

        if (animate && popped != null)
            foreach (string id in popped)
                if (oldWorld.TryGetValue(id, out Vector3 at)) SpawnGhost(id, at);

        // fill the slots
        _ids.Clear();
        for (int i = 0; i < queue.Count; i++)
        {
            _ids.Add(queue[i]);
            if (i >= _slots.Count) { _slots.Add(Instantiate(slotTemplate, transform)); _finalFrames.Add(null); }
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

            SetFinalFrame(i, i == queue.Count - 1, size);
        }
        for (int i = queue.Count; i < _slots.Count; i++) _slots[i].gameObject.SetActive(false);

        PlaceHalo(queue.Count > 0 ? _slots[0] : null, headSize);
        var rect = (RectTransform)transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        if (rect.parent is RectTransform parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

        if (hideWhenEmpty != null)
        {
            bool sliding = animate && popped != null && popped.Count > 0;
            if (queue.Count > 0 || !sliding) hideWhenEmpty.SetActive(queue.Count > 0);
            else DOVirtual.DelayedCall(slideSeconds, () => { if (this != null && _ids.Count == 0) hideWhenEmpty.SetActive(false); }).SetId(this);
        }
        if (!animate) return;

        // slide the survivors from where they were, shake the re-spotted ones
        for (int i = 0; i < queue.Count; i++)
        {
            RectTransform slot = _slots[i];
            bool moves = oldWorld.TryGetValue(queue[i], out Vector3 was);
            bool shakes = respot != null && ContainsId(respot, queue[i]);
            if (!moves && !shakes) continue;

            Vector2 target = slot.anchoredPosition;
            float slideX = 0f;
            if (moves)
            {
                Vector3 now = slot.TransformPoint(slot.rect.center);
                float dx = slot.parent.InverseTransformVector(was - now).x;
                if (Mathf.Abs(dx) > 0.5f) slideX = dx;
            }
            if (slideX == 0f && !shakes) continue;

            var seq = DOTween.Sequence().SetId(slot);
            if (slideX != 0f)
            {
                slot.anchoredPosition = target + new Vector2(slideX, 0f);
                seq.Append(slot.DOAnchorPos(target, slideSeconds).SetEase(Ease.OutQuad));
            }
            if (shakes) seq.Append(slot.DOShakeAnchorPos(0.35f, new Vector2(5f, 0f), 20, 90f, false, true));
            seq.OnKill(() => { if (slot != null) slot.anchoredPosition = target; });
        }
    }

    private static bool ContainsId(IReadOnlyList<string> list, string id)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == id) return true;
        return false;
    }

    private void SpawnGhost(string id, Vector3 worldCentre)
    {
        var ghost = Instantiate(slotTemplate, transform);
        ghost.gameObject.name = "Popped " + id;
        ghost.gameObject.SetActive(true);
        var element = ghost.GetComponent<LayoutElement>();
        element.ignoreLayout = true;                                     // not part of the strip any more
        ghost.sizeDelta = new Vector2(otherSize, otherSize);
        ghost.position = worldCentre;
        var image = ghost.GetComponent<Image>();
        image.sprite = sprites != null ? sprites.Get(id) : null;
        image.raycastTarget = false;
        var badge = ghost.Find("Badge");
        if (badge != null) badge.gameObject.SetActive(false);
        _ghosts.Add(ghost);

        Vector2 from = ghost.anchoredPosition;
        DOTween.Sequence().SetId(ghost)
            .Join(ghost.DOAnchorPos(from + new Vector2(0f, 14f), slideSeconds).SetEase(Ease.OutQuad))
            .Join(image.DOFade(0f, slideSeconds))
            .OnComplete(() => { if (ghost != null) { _ghosts.Remove(ghost); Destroy(ghost.gameObject); } });
    }

    /// The gold frame on the final ball of the queue.
    private void SetFinalFrame(int index, bool isFinal, float size)
    {
        RectTransform frame = _finalFrames[index];
        if (frame == null)
        {
            if (!isFinal) return;
            var go = new GameObject("FinalFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = gameObject.layer };
            frame = (RectTransform)go.transform;
            frame.SetParent(_slots[index], false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = finalColor;
            Stretch(frame, 3f);
            _finalFrames[index] = frame;
        }
        frame.gameObject.SetActive(isFinal);
        if (isFinal) frame.GetComponent<Image>().sprite = BilliardsArt.Ring(Mathf.RoundToInt(size) + 6, 2);
    }

    /// The pulsing halo sits on the head slot; it is created once and moves as the head changes.
    private void PlaceHalo(RectTransform head, float size)
    {
        if (head == null) { if (_halo != null) _halo.gameObject.SetActive(false); return; }
        if (_halo == null)
        {
            var go = new GameObject("Halo", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = gameObject.layer };
            _halo = (RectTransform)go.transform;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = haloColor;
        }
        _halo.SetParent(head, false);
        _halo.SetAsFirstSibling();                                       // behind the ball image's children
        Stretch(_halo, 5f);
        var halo = _halo.GetComponent<Image>();
        halo.sprite = BilliardsArt.Ring(Mathf.RoundToInt(size) + 10, 2);
        _halo.gameObject.SetActive(true);
        _halo.localScale = Vector3.one;
        halo.color = haloColor;
        DOTween.Kill(_halo);
        _halo.DOScale(1.14f, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(_halo);
        halo.DOFade(0.45f, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(_halo);
    }

    private static void Stretch(RectTransform rt, float grow)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-grow, -grow); rt.offsetMax = new Vector2(grow, grow);
        rt.localScale = Vector3.one;
    }

    private void KillAnimations()
    {
        foreach (var slot in _slots) if (slot != null) DOTween.Kill(slot);
        if (_halo != null) DOTween.Kill(_halo);
        DOTween.Kill(this);
        for (int i = _ghosts.Count - 1; i >= 0; i--)
        {
            if (_ghosts[i] == null) continue;
            DOTween.Kill(_ghosts[i]);
            Destroy(_ghosts[i].gameObject);
        }
        _ghosts.Clear();
    }

    private void OnDisable() => KillAnimations();

    private void OnDestroy() => KillAnimations();
}
