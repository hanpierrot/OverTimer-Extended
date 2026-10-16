using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// The look of the table. The wooden rails, the felt and the cushion shadow are built in the prefab from the
/// Billiard Kit (the kit's reference table is drawn in 3x3 blocks, so the table art is 3 canvas units per art pixel
/// while the balls are 1). This component draws the six pocket holes: block-pixel discs sized from the SAME radii
/// the simulation uses, so a ball disappears exactly where the hole is drawn. If the art is ever replaced, keep the
/// felt rectangle and these radii and nothing else has to move.
public class TableView : MonoBehaviour
{
    [SerializeField] private RectTransform[] pocketMarkers;      // TL, TM, TR, BL, BM, BR, each with an Image
    [SerializeField] private float artPixel = 3f;                // canvas units per table-art pixel
    [SerializeField] private int rimPixels = 1;
    [SerializeField] private Color holeColor = new Color32(0, 16, 8, 255);
    [SerializeField] private Color rimColor = new Color32(138, 138, 138, 255);
    [SerializeField] private Color ringColor = new Color32(255, 214, 64, 255);   // the ring on the called pocket

    private static readonly Pocket[] Order = { Pocket.TL, Pocket.TM, Pocket.TR, Pocket.BL, Pocket.BM, Pocket.BR };

    private RectTransform _ring;
    private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

    /// feltWidth: the felt RectTransform's width in canvas units. The pockets form the same set of six under every
    /// mirror, so no mirror is needed here.
    public void Layout(BilliardsSimParams p, float feltWidth)
    {
        if (pocketMarkers == null) return;
        var mapper = new FeltMapper(feltWidth, default);
        float scale = feltWidth / FeltMapper.TableW;
        for (int i = 0; i < pocketMarkers.Length && i < Order.Length; i++)
        {
            var marker = pocketMarkers[i];
            if (marker == null) continue;
            bool side = Order[i] == Pocket.TM || Order[i] == Pocket.BM;
            float radius = (side ? p.sidePocketRadius : p.cornerPocketRadius) * scale;

            int holePixels = Mathf.Max(2, Mathf.RoundToInt(2f * radius / artPixel));      // dark part = the capture area, to the nearest art pixel
            int totalPixels = holePixels + 2 * rimPixels;
            marker.anchoredPosition = mapper.TableToFelt(BilliardsSim.PocketCentre(Order[i]));
            marker.sizeDelta = Vector2.one * (totalPixels * artPixel);

            var image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = DiscSprite(totalPixels);
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }
    }

    /// Rings the pocket the head ball has to go in (pass the pocket as the PLAYER sees it, already mirrored), or hides the
    /// ring when pocket is null. The ring pulses; it is hidden again whenever the head ball has no called pocket.
    public void ShowCalledPocket(Pocket? pocket)
    {
        int index = pocket.HasValue ? System.Array.IndexOf(Order, pocket.Value) : -1;
        if (index < 0 || pocketMarkers == null || index >= pocketMarkers.Length || pocketMarkers[index] == null)
        {
            if (_ring != null) { DOTween.Kill(_ring); _ring.gameObject.SetActive(false); }
            return;
        }

        RectTransform marker = pocketMarkers[index];
        if (_ring == null)
        {
            var go = new GameObject("CalledPocketRing", typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
            _ring = (RectTransform)go.transform;
            _ring.SetParent(marker.parent, false);
            _ring.anchorMin = _ring.anchorMax = Vector2.zero;
            _ring.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = ringColor;
        }

        int pixels = Mathf.RoundToInt(marker.sizeDelta.x / artPixel) + 4;        // two art pixels clear of the hole's rim
        var image = _ring.GetComponent<Image>();
        image.sprite = BilliardsArt.Ring(pixels, 1);
        image.color = ringColor;
        _ring.sizeDelta = Vector2.one * (pixels * artPixel);
        _ring.anchoredPosition = marker.anchoredPosition;
        _ring.SetSiblingIndex(marker.GetSiblingIndex() + 1);                    // above the holes, below the balls
        _ring.localScale = Vector3.one;
        _ring.gameObject.SetActive(true);

        DOTween.Kill(_ring);
        _ring.DOScale(1.1f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(_ring);
        image.DOFade(0.5f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(_ring);
    }

    /// A hard-edged disc, rimPixels of grey around a dark centre, one texel per art pixel.
    private Sprite DiscSprite(int pixels)
    {
        if (_sprites.TryGetValue(pixels, out Sprite cached) && cached != null) return cached;

        var tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "PocketDisc" + pixels };
        float outer = pixels * 0.5f, inner = outer - rimPixels;
        for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                float dx = x + 0.5f - outer, dy = y + 0.5f - outer;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= inner ? holeColor : d <= outer ? rimColor : Color.clear);
            }
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, pixels, pixels), new Vector2(0.5f, 0.5f), 100f);
        _sprites[pixels] = sprite;
        return sprite;
    }

    private void OnDisable() { if (_ring != null) { DOTween.Kill(_ring); _ring.gameObject.SetActive(false); } }

    private void OnDestroy()
    {
        foreach (var kv in _sprites)
        {
            if (kv.Value == null) continue;
            Destroy(kv.Value.texture);
            Destroy(kv.Value);
        }
        _sprites.Clear();
    }
}
