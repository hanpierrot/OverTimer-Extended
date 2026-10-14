using System.Collections.Generic;
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

    private static readonly Pocket[] Order = { Pocket.TL, Pocket.TM, Pocket.TR, Pocket.BL, Pocket.BM, Pocket.BR };

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
