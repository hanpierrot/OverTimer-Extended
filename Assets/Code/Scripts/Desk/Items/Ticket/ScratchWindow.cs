using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScratchWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private Image symbolImage;
    [SerializeField] private RawImage foilImage;
    [SerializeField] private Texture2D foilArt;

    [Header("Mask")]
    [SerializeField] private int maskResolution = 96;
    [SerializeField] private Color foilColor = new Color(0.72f, 0.74f, 0.76f, 1f);
    [SerializeField] private bool circularWindow = true;

    [Header("Brush")]
    [SerializeField] private float brushRadius = 9f;
    [SerializeField, Range(0f, 1f)] private float edgeSoftness = 0.35f;
    [SerializeField] private byte clearedAlphaThreshold = 40;

    [Header("Reveal")]
    [SerializeField, Range(0f, 1f)] private float revealThreshold = 0.6f;

    public event Action<ScratchWindow> OnRevealed;

    public bool IsRevealed { get; private set; }
    public float ScratchedPercent => _totalPlayablePixels == 0 ? 0f : (float)_clearedCount / _totalPlayablePixels;

    private Texture2D _maskTexture;
    private Color32[] _pixels;
    private bool[] _cleared;
    private bool[] _insideCircle;
    private int _clearedCount;
    private int _totalPixels;
    private int _totalPlayablePixels;

    private bool _hasLastPoint;
    private Vector2Int _lastPixel;
    private bool _interactable = true;
    private bool _initialized;

    private void Awake() => EnsureInitialized();
    
    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        
        _totalPixels = maskResolution * maskResolution;
        _pixels = new Color32[_totalPixels];
        _cleared = new bool[_totalPixels];
        _insideCircle = new bool[_totalPixels];
        _totalPlayablePixels = 0;
        
        if (circularWindow)
        {
            Vector2 center = new Vector2((maskResolution - 1) * 0.5f, (maskResolution - 1) * 0.5f);
            float windowRadius = maskResolution * 0.5f;
            for (int y = 0; y < maskResolution; y++)
            {
                for (int x = 0; x < maskResolution; x++)
                {
                    int idx = y * maskResolution + x;
                    bool inside = Vector2.Distance(new Vector2(x, y), center) <= windowRadius;
                    _insideCircle[idx] = inside;
                    if (inside) _totalPlayablePixels++;
                }
            }
        }
        else
        {
            for (int i = 0; i < _insideCircle.Length; i++) _insideCircle[i] = true;
            _totalPlayablePixels = _totalPixels;
        }
        
        ResetPixelsToInitialFoil();
        
        _maskTexture = new Texture2D(maskResolution, maskResolution, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        _maskTexture.SetPixels32(_pixels);
        _maskTexture.Apply(false);
        foilImage.texture = _maskTexture;
    }
    
    private void OnDestroy()
    {
        if (_maskTexture != null) Destroy(_maskTexture);
    }

    public void Populate(Sprite symbol)
    {
        EnsureInitialized();
        
        symbolImage.sprite = symbol;
        IsRevealed = false;
        _interactable = true;
        _hasLastPoint = false;
        _clearedCount = 0;
        Array.Clear(_cleared, 0, _cleared.Length);

        ResetPixelsToInitialFoil();
        _maskTexture.SetPixels32(_pixels);
        _maskTexture.Apply(false);

        foilImage.raycastTarget = true;
    }
    
    private void ResetPixelsToInitialFoil()
    {
        if (foilArt != null)
        {
            for (int y = 0; y < maskResolution; y++)
            {
                for (int x = 0; x < maskResolution; x++)
                {
                    float u = x / (float)(maskResolution - 1);
                    float v = y / (float)(maskResolution - 1);
                    Color c = foilArt.GetPixelBilinear(u, v);
                    c.a = 1f;
                    _pixels[y * maskResolution + x] = c;
                }
            }
        }
        else
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = foilColor;
        }
        
        if (circularWindow)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (_insideCircle[i]) continue;
                Color32 c = _pixels[i];
                c.a = 0;
                _pixels[i] = c;
            }
        }
    }
    
    public void SetInteractable(bool value) => _interactable = value;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable || IsRevealed) return;
        if (TryGetPixel(eventData, out Vector2Int pixel))
        {
            _lastPixel = pixel;
            _hasLastPoint = true;
            StampLine(pixel, pixel);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData){}

    public void OnDrag(PointerEventData eventData)
    {
        if (!_interactable || IsRevealed) return;
        if (!TryGetPixel(eventData, out Vector2Int pixel)) return;

        if (!_hasLastPoint)
        {
            _lastPixel = pixel;
            _hasLastPoint = true;
        }
        
        StampLine(_lastPixel, pixel);
        _lastPixel = pixel;
    }
    
    public void OnEndDrag(PointerEventData eventData) => _hasLastPoint = false;

    public bool TryGetPixel(PointerEventData eventData, out Vector2Int pixel)
    {
        pixel = default;
        RectTransform rt = foilImage.rectTransform;
        
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return false;

        Rect rect = rt.rect;
        float u = Mathf.Clamp01((local.x - rect.x) / rect.width);
        float v = Mathf.Clamp01((local.y - rect.y) / rect.height);

        pixel = new Vector2Int(
            Mathf.RoundToInt(u * (maskResolution - 1)),
            Mathf.RoundToInt(v * (maskResolution - 1)));
        return true;
    }

    private void StampLine(Vector2Int start, Vector2Int end)
    {
        float dist = Vector2Int.Distance(start, end);
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / Mathf.Max(1f, brushRadius * 0.5f)));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(start, end, steps == 0 ? 0f : (float)i / steps);
            StampCircle(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }
        
        _maskTexture.SetPixels32(_pixels);
        _maskTexture.Apply(false);

        if (!IsRevealed && ScratchedPercent >= revealThreshold)
            Reveal();
    }

    private void StampCircle(int cx, int cy)
    {
        int r = Mathf.CeilToInt(brushRadius);

        int minX = Mathf.Max(0, cx - r), maxX = Mathf.Min(maskResolution - 1, cx + r);
        int minY = Mathf.Max(0, cy - r), maxY = Mathf.Min(maskResolution - 1, cy + r);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int idx = y * maskResolution + x;
                if (!_insideCircle[idx]) continue;
                
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (dist > brushRadius) continue;

                float edgeStart = brushRadius * (1f - edgeSoftness);
                float falloff = edgeSoftness <= 0f ? 0f : Mathf.Clamp01((dist - edgeStart) / (brushRadius - edgeStart));
                byte newAlpha = (byte)(255 * falloff);
                
                Color32 c = _pixels[idx];

                if (newAlpha < c.a)
                {
                    c.a = newAlpha;
                    _pixels[idx] = c;
                }

                if (!_cleared[idx] && c.a <= clearedAlphaThreshold)
                {
                    _cleared[idx] = true;
                    _clearedCount++;
                }
            }
        }
    }

    public void Reveal()
    {
        IsRevealed = true;
        for (int i = 0; i < _pixels.Length; i++)
        {
            Color32 c = _pixels[i];
            c.a = 0;
            _pixels[i] = c;
        }
        _clearedCount = _totalPlayablePixels;
        _maskTexture.SetPixels32(_pixels);
        _maskTexture.Apply(false);

        foilImage.raycastTarget = false;
        OnRevealed?.Invoke(this);
    }
}
