using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A soft contact shadow under a desk object (DESIGN §5.18). Put it on the object that carries the main
/// <see cref="SpriteRenderer"/>; it builds a child "Shadow" sprite itself, so a prefab only needs this component.
/// The fake light comes from the upper left, so every shadow falls down and to the right.
/// The shadow sits behind the object (same sorting order as its lowest renderer, pushed back in z), follows the
/// sprite's size and, while the player drags the object, grows, drops further away and fades: the object looks lifted.
/// Purely visual: it never touches input, the clock or money.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow : MonoBehaviour
{
    public enum Style
    {
        /// <summary>Lies flat on the desk (cards, tickets, letters): a soft blob offset down and to the right.</summary>
        Flat,
        /// <summary>Stands on the desk or floor (laptop, boxes, toys): a flat ellipse under the base.</summary>
        Standing,
        /// <summary>Hangs on a wall (picture, egg timer): a darker copy of the sprite pushed away from the light (upper left), like a thin shadow cast on the wall.</summary>
        Wall,
    }

    [SerializeField] private Style style = Style.Flat;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;
    [Tooltip("Alpha while the object is being dragged (it looks lifted, so the shadow is fainter).")]
    [SerializeField, Range(0f, 1f)] private float liftedAlpha = 0.22f;
    [Tooltip("Size and place the shadow from the visible part of the sprite (its tight mesh) instead of the whole cell. Use it for animated sprites whose frames have empty space under the feet (the Cat). Needs the sprite imported with Mesh Type = Tight.")]
    [SerializeField] private bool useOpaqueBounds;
    [Tooltip("Wall style: how far the shadow falls on the wall, as a share of the sprite's longest side. The light comes from the upper left, so it falls down and to the right.")]
    [SerializeField, Range(0f, 0.15f)] private float wallDepth = 0.04f;

    private const float LiftSeconds = 0.12f;
    private const float BehindZ = 0.05f;     // pushes the shadow behind the object when the sorting order ties

    // One shared soft-edged disc, built on first use. HideAndDontSave: it must survive scene reloads (Restart)
    // and is never saved. Documented static exception: it is an asset-like cache, not game state.
    private static Sprite _blob;
    // Visible bounds per sprite (from its tight mesh), filled on first use. Derived data only: safe to lose, rebuilt on demand.
    private static readonly Dictionary<Sprite, Bounds> OpaqueBounds = new Dictionary<Sprite, Bounds>();

    private SpriteRenderer _owner;
    private SpriteRenderer _shadow;
    private PointerReceiver _receiver;
    private int _orderBelowOwner;            // owner order minus the lowest order among its renderers
    private float _lift;                     // 0 = on the desk, 1 = fully lifted
    private bool _dragging;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _blob = null;
        OpaqueBounds.Clear();
    }

    private void Awake()
    {
        _owner = GetComponent<SpriteRenderer>();
        _receiver = GetComponent<PointerReceiver>();

        var go = new GameObject("Shadow");
        go.transform.SetParent(transform, false);
        go.AddComponent<HoverGlowIgnore>();          // the hover outline must not trace the shadow
        _shadow = go.AddComponent<SpriteRenderer>();
        _shadow.sprite = Blob();
        _shadow.sharedMaterial = _owner.sharedMaterial;   // the same unlit sprite material the object uses
        _shadow.sortingLayerID = _owner.sortingLayerID;

        int lowest = _owner.sortingOrder;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == _shadow || sr.GetComponent<HoverGlowIgnore>() != null) continue;   // skip glows and the shadow
            if (sr.sortingOrder < lowest) lowest = sr.sortingOrder;
        }
        _orderBelowOwner = _owner.sortingOrder - lowest;
    }

    private void OnEnable()
    {
        if (_receiver == null) return;
        _receiver.DragStart += HandleDragStart;
        _receiver.DragEnd += HandleDragEnd;
    }

    private void OnDisable()
    {
        if (_receiver != null)
        {
            _receiver.DragStart -= HandleDragStart;
            _receiver.DragEnd -= HandleDragEnd;
        }
        _dragging = false;       // pawned or hidden mid-drag: it comes back on the desk
        _lift = 0f;
    }

    private void HandleDragStart(Vector2 worldPos) => _dragging = true;
    private void HandleDragEnd(Vector2 worldPos) => _dragging = false;

    // LateUpdate: the owner's sprite, order or scale may change in Update (frames, flips, letters getting an order).
    private void LateUpdate()
    {
        Sprite s = _owner.sprite;
        bool show = s != null && _owner.enabled;
        _shadow.enabled = show;
        if (!show) return;

        _lift = Mathf.MoveTowards(_lift, _dragging ? 1f : 0f, Time.unscaledDeltaTime / LiftSeconds);

        Bounds b = useOpaqueBounds ? BoundsOf(s) : s.bounds;   // in the owner's local space, so parent scale applies by itself
        float cx = _owner.flipX ? -b.center.x : b.center.x;
        Vector2 size;
        Vector2 pos;
        float a;
        if (style == Style.Wall)
        {
            // The sprite itself, tinted dark and shifted away from the light: a hard-edged silhouette. It follows the
            // owner's frames (egg timer) and flips, and needs no lift (wall objects are not dragged).
            if (_shadow.sprite != s) _shadow.sprite = s;
            _shadow.flipX = _owner.flipX;
            _shadow.flipY = _owner.flipY;
            float depth = Mathf.Max(b.size.x, b.size.y) * wallDepth;
            size = Vector2.one;
            pos = new Vector2(depth, -depth);
            a = alpha;
        }
        else if (style == Style.Standing)
        {
            float w = b.size.x * (0.95f + 0.1f * _lift);
            float h = Mathf.Min(b.size.x * 0.22f, b.size.y * 0.4f);
            size = new Vector2(w, h);
            pos = new Vector2(cx, b.min.y + 0.02f * b.size.y - 0.1f * b.size.y * _lift);
            a = Mathf.Lerp(alpha + 0.05f, liftedAlpha, _lift);
        }
        else
        {
            size = new Vector2(b.size.x, b.size.y) * (1.3f + 0.08f * _lift);
            Vector2 offset = new Vector2(0.05f * b.size.x, -0.07f * b.size.y) * (1f + 1.2f * _lift);
            pos = new Vector2(cx, b.center.y) + offset;
            a = Mathf.Lerp(alpha, liftedAlpha, _lift);
        }

        Transform t = _shadow.transform;
        t.localPosition = new Vector3(pos.x, pos.y, BehindZ);
        t.localScale = new Vector3(size.x, size.y, 1f);
        _shadow.color = new Color(0f, 0f, 0f, a);
        _shadow.sortingOrder = _owner.sortingOrder - _orderBelowOwner;
    }

    // The bounds of the sprite's tight mesh: the outline of the visible pixels, without the empty margin of the cell.
    private static Bounds BoundsOf(Sprite s)
    {
        Bounds b;
        if (OpaqueBounds.TryGetValue(s, out b)) return b;

        b = s.bounds;                                   // fallback: a full-rect sprite has no tighter outline
        Vector2[] v = s.vertices;                       // allocates, hence the cache
        if (v.Length > 0)
        {
            Vector2 min = v[0];
            Vector2 max = v[0];
            for (int i = 1; i < v.Length; i++)
            {
                min = Vector2.Min(min, v[i]);
                max = Vector2.Max(max, v[i]);
            }
            b = new Bounds((min + max) * 0.5f, max - min);
        }
        OpaqueBounds[s] = b;
        return b;
    }

    private static Sprite Blob()
    {
        if (_blob != null) return _blob;

        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        var pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f - n * 0.5f) / (n * 0.5f);
                float dy = (y + 0.5f - n * 0.5f) / (n * 0.5f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float k = Mathf.Clamp01(1f - r);
                k = k * k * (3f - 2f * k);                      // smoothstep: soft edge, nothing left at the border
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(k * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        _blob = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 unit wide
        _blob.hideFlags = HideFlags.HideAndDontSave;
        return _blob;
    }
}
