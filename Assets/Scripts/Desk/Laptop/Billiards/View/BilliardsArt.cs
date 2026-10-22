using System.Collections.Generic;
using UnityEngine;

/// Small placeholder sprites made in code (no assets needed, work in a build): hard-edged rings for the head-ball halo,
/// the final-ball frame and the called-pocket marker. One texel per art pixel; the caller sets the Image size.
public static class BilliardsArt
{
    private static readonly Dictionary<long, Sprite> Rings = new Dictionary<long, Sprite>();

    /// A white ring, `pixels` wide, `thickness` texels thick. Tint it with Image.color.
    public static Sprite Ring(int pixels, int thickness)
    {
        pixels = Mathf.Max(4, pixels);
        thickness = Mathf.Clamp(thickness, 1, pixels / 2);
        long key = ((long)pixels << 16) | (uint)thickness;
        if (Rings.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        var tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            name = $"Ring{pixels}x{thickness}", hideFlags = HideFlags.HideAndDontSave,
        };
        float outer = pixels * 0.5f, inner = outer - thickness;
        for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                float dx = x + 0.5f - outer, dy = y + 0.5f - outer;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= outer && d > inner ? Color.white : Color.clear);
            }
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, pixels, pixels), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        Rings[key] = sprite;
        return sprite;
    }
}
