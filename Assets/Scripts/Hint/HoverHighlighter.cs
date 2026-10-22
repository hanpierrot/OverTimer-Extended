using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;

public class HoverHighlighter : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Glow")]
    [SerializeField] private Material glowMaterial; 
    [SerializeField] private int maxQuads = 8; 

    private static readonly int RectId = Shader.PropertyToID("_SpriteRect");
    private static readonly int PadId = Shader.PropertyToID("_Pad");
    private static readonly int FlipId = Shader.PropertyToID("_Flip");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int ColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int AmountId = Shader.PropertyToID("_Amount");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private class GlowQuad
    {
        public Transform Transform;
        public MeshRenderer Renderer;
    }
    
    private readonly List<GlowQuad> _quads = new List<GlowQuad>();
    private readonly List<SpriteRenderer> _targets = new List<SpriteRenderer>();
    private MaterialPropertyBlock _block;
    private Mesh _quadMesh; 
    private Camera _camera;   
    private GameConfig _config;  

    private Collider2D _current; 
    private Collider2D _shown; 
    private float _amount;  
    private float _timer;
    
    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _quadMesh = BuildQuad();
    }

    private void Start()
    {
        _config = GameManager.Instance.GameConfig; 
        _camera = Camera.main;
    }

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f)
        {
            _timer = checkInterval;
            _current = Pick();
        }

        bool want = _current != null && _current.gameObject.activeInHierarchy && !GameManager.Instance.IsGameOver;
        if (want && _current != _shown) SetShown(_current);

        float seconds = want ? _config.hoverGlowFadeInSeconds : _config.hoverGlowFadeOutSeconds;
        float step = seconds > 0f ? Time.unscaledDeltaTime / seconds : 1f;
        _amount = Mathf.MoveTowards(_amount, want ? 1f : 0f, step);

        if (_amount <= 0f && !want && _shown != null) SetShown(null);
    }
    
    private void LateUpdate() => Render(); 
    
    private Collider2D Pick()
    {
        if (InputManager.Instance == null) return null;

        Collider2D c = InputManager.Instance.PeekTarget();
        if (c == null) return null;

        if (c.TryGetComponent(out PointerReceiver receiver) && !receiver.IsInteractable) return null;
        if (c.TryGetComponent(out CameraViewButton button) && button.Target == CameraView.Room) return null;
        return c;
    }
    
    private void SetShown(Collider2D target)
    {
        _shown = target;
        _targets.Clear();
        HideAll();
        if (target == null) return;

        target.GetComponentsInChildren(false, _targets);
    }
    
    private void Render()
    {
        if (_shown == null || _amount <= 0f || _camera == null) { HideAll(); return; }

        float worldWidth = _config.hoverGlowWidthPixels * 2f * _camera.orthographicSize / Screen.height;

        int used = 0;
        foreach (SpriteRenderer sr in _targets)
        {
            if (sr == null || !sr.enabled || sr.sprite == null || used >= maxQuads) continue;

            Sprite sprite = sr.sprite;
            GlowQuad q = GetQuad(used++);
            
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            Vector2 min = -sprite.pivot / sprite.pixelsPerUnit;
            Vector2 center = min + size * 0.5f;

            Vector3 scale = sr.transform.lossyScale;
            Vector2 radius = new Vector2(worldWidth / Mathf.Abs(scale.x) / size.x,
                worldWidth / Mathf.Abs(scale.y) / size.y);
            Vector2 pad = radius * 1.2f; 

            float cx = sr.flipX ? -center.x : center.x;
            float cy = sr.flipY ? -center.y : center.y;

            q.Transform.SetParent(sr.transform, false);
            q.Transform.localRotation = Quaternion.identity;
            q.Transform.localScale = new Vector3(size.x * (1f + 2f * pad.x), size.y * (1f + 2f * pad.y), 1f);
            q.Transform.localPosition = new Vector3(cx - size.x * (0.5f + pad.x), cy - size.y * (0.5f + pad.y), 0f);

            q.Renderer.sortingLayerID = sr.sortingLayerID;
            q.Renderer.sortingOrder = sr.sortingOrder + 1;

            Texture2D tex = sprite.texture;
            Vector2 cellMin = sprite.textureRect.min - sprite.textureRectOffset;
            Vector2 cellMax = cellMin + sprite.rect.size;
            Vector4 uv = new Vector4(cellMin.x / tex.width, cellMin.y / tex.height,
                cellMax.x / tex.width, cellMax.y / tex.height);

            _block.SetTexture(MainTexId, tex);
            _block.SetVector(RectId, uv);
            _block.SetVector(PadId, pad);
            _block.SetVector(FlipId, new Vector2(sr.flipX ? 1f : 0f, sr.flipY ? 1f : 0f));
            _block.SetVector(RadiusId, radius);
            _block.SetColor(ColorId, _config.hoverGlowColor);
            _block.SetFloat(AmountId, _amount);
            q.Renderer.SetPropertyBlock(_block);
            q.Renderer.enabled = true;
        }

        for (int i = used; i < _quads.Count; i++) _quads[i].Renderer.enabled = false;
    }   
    
    private void HideAll()
    {
        foreach (GlowQuad q in _quads) 
            if(q.Renderer != null)
                q.Renderer.enabled = false;
    }
    
    private GlowQuad GetQuad(int index)
    {
        while (_quads.Count <= index)
        {
            var go = new GameObject("HoverGlowQuad");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quadMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = glowMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;
            _quads.Add(new GlowQuad { Transform = go.transform, Renderer = mr });
        }
        return _quads[index];
    }
    
    private static Mesh BuildQuad()
    {
        var m = new Mesh { name = "HoverGlowQuad" };
        m.vertices = new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        return m;
    }
    
    private void OnDisable() => HideAll();
}
