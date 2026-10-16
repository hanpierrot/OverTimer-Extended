using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Debug overlay: draws what the SIMULATION sees on top of the art, so art and physics can be lined up by eye.
///   red discs     pocket capture areas (a ball whose centre enters one is potted)
///   yellow lines  cushion segments (the felt edge, where a ball's edge bounces)
///   magenta dots  jaw points (the segment ends)
///   blue discs    every ball at its physical radius, at its exact (unsnapped) simulated position
/// The art must be moved to match these, never the other way round: changing the simulation invalidates the stored
/// solutions. Toggled from the cheat menu or Tools/OverTimer/Billiards/Toggle Debug Overlay.
public class BilliardsDebugOverlay : MonoBehaviour
{
    private readonly List<RectTransform> _ballDiscs = new List<RectTransform>();
    private RectTransform _balls;
    private Sprite _disc;
    private float _ballDiameter;
    private bool _built;

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }

    /// Builds the static parts (pockets, cushions, jaws). Safe to call again after the constants change.
    public void Build(BilliardsSimParams p, float feltWidth)
    {
        if (_disc == null) _disc = MakeDisc();
        _ballDiameter = p.ballRadius * feltWidth;                 // 2r table units x (feltWidth / 2) canvas units per table unit
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        _ballDiscs.Clear();

        float scale = feltWidth / FeltMapper.TableW;
        var mapper = new FeltMapper(feltWidth, default);          // the table is symmetric: no mirror needed for the static parts
        foreach (Pocket pk in new[] { Pocket.TL, Pocket.TM, Pocket.TR, Pocket.BL, Pocket.BM, Pocket.BR })
        {
            bool side = pk == Pocket.TM || pk == Pocket.BM;
            float radius = (side ? p.sidePocketRadius : p.cornerPocketRadius) * scale;
            Disc("Pocket " + pk, transform, mapper.TableToFelt(BilliardsSim.PocketCentre(pk)), radius * 2f, new Color(1f, 0.1f, 0.1f, 0.30f));
        }

        var cushions = new List<(Vector2 a, Vector2 b)>();
        var jaws = new List<Vector2>();
        BilliardsSim.GetTableGeometry(p, cushions, jaws);
        foreach (var c in cushions) Line("Cushion", mapper.TableToFelt(c.a), mapper.TableToFelt(c.b), new Color(1f, 0.9f, 0.1f, 0.95f), 2f);
        foreach (var j in jaws) Disc("Jaw", transform, mapper.TableToFelt(j), 6f, new Color(1f, 0f, 1f, 1f));

        var balls = new GameObject("Balls", typeof(RectTransform)) { layer = gameObject.layer };
        _balls = (RectTransform)balls.transform;
        _balls.SetParent(transform, false);
        _balls.anchorMin = Vector2.zero; _balls.anchorMax = Vector2.one;
        _balls.offsetMin = Vector2.zero; _balls.offsetMax = Vector2.zero;
        _built = true;
    }

    /// One blue disc on each unpotted ball, at the physical ball size and the exact simulated position.
    public void Refresh(BilliardsSim sim, FeltMapper mapper)
    {
        if (!_built || sim == null) return;
        int shown = 0;
        for (int i = 0; i < sim.BallCount; i++)
        {
            if (sim.IsPotted(i)) continue;
            if (shown >= _ballDiscs.Count) _ballDiscs.Add(NewDisc("Ball", _balls, _ballDiameter, new Color(0.1f, 0.4f, 1f, 0.35f)));
            var rt = _ballDiscs[shown++];
            rt.gameObject.SetActive(true);
            rt.anchoredPosition = mapper.TableToFelt(sim.Position(i));
        }
        for (int i = shown; i < _ballDiscs.Count; i++) _ballDiscs[i].gameObject.SetActive(false);
    }

    private void Disc(string name, Transform parent, Vector2 centre, float diameter, Color color) =>
        NewDisc(name, parent, diameter, color).anchoredPosition = centre;

    private RectTransform NewDisc(string name, Transform parent, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * diameter;
        var img = go.GetComponent<Image>();
        img.sprite = _disc; img.color = color; img.raycastTarget = false;
        return rt;
    }

    private void Line(string name, Vector2 a, Vector2 b, Color color, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(1f, thickness);
        var img = go.GetComponent<Image>();
        img.color = color; img.raycastTarget = false;
        UiSegment.Set(rt, a, b);
    }

    /// A filled circle made in code, so the overlay needs no asset and works in a build.
    private static Sprite MakeDisc()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "OverlayDisc" };
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(c + 0.5f - d)));
            }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDestroy()
    {
        if (_disc == null) return;
        Destroy(_disc.texture);
        Destroy(_disc);
    }
}
