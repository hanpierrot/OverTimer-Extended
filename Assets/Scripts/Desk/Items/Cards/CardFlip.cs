using System;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class CardFlip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image image;
    [SerializeField] private Material uiFlipMaterial;
    [SerializeField] private Sprite backSprite;
    [SerializeField, Range(0f, 1f)] private float edgeDarken = 0.35f;

    private static readonly int BackTexId = Shader.PropertyToID("_BackTex");
    private static readonly int FrontRectId = Shader.PropertyToID("_FrontRect");
    private static readonly int BackRectId = Shader.PropertyToID("_BackRect");
    private static readonly int ShadeId = Shader.PropertyToID("_Shade");

    private MaterialPropertyBlock _block;
    private Material _uiInstance;
    private Sprite _syncedFront;
    private Sprite _syncedBack;
    private float _lastAngle = -1f;
    private Tween _tween;
    
    private Sprite Front => spriteRenderer != null ? spriteRenderer.sprite : (image != null ? image.sprite : null);

    public bool IsShowingBack => Mathf.Cos(transform.localEulerAngles.y * Mathf.Deg2Rad) < -0.001f;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();

        if (image != null && uiFlipMaterial != null)
        {
            _uiInstance = new Material(uiFlipMaterial);
            image.material = _uiInstance;
        }
    }

    private void LateUpdate()
    {
        if (Front != _syncedFront || backSprite != _syncedBack) SyncSprites();
        
        float y = transform.localEulerAngles.y;
        if (Mathf.Approximately(y, _lastAngle)) return;
        _lastAngle = y;
        
        float c = Mathf.Abs(Mathf.Cos(y * Mathf.Deg2Rad));
        ApplyShade(Mathf.Lerp(1f - edgeDarken, 1f, c));
    }

    public void SetBack(Sprite back)
    {
        backSprite = back;
        SyncSprites();
    }

    public Tween FlipTo(bool showBack, float duration = 0.4f)
    {
        _tween?.Kill();
        _tween = transform.DOLocalRotate(new Vector3(0f, showBack ? 180f : 0f, 0f), duration)
            .SetEase(Ease.InOutSine).SetUpdate(true);
        return _tween;
    }
    
    public void ResetFlip() => SetFaceDown(false);

    private void SyncSprites()
    {
        Sprite front = Front;
        _syncedFront = front;
        _syncedBack = backSprite;
        if (front == null || backSprite == null) return;
        
        Vector4 frontRect = ToRect(front);
        Vector4 backRect = ToRect(backSprite);

        if (_uiInstance != null)
        {
            _uiInstance.SetTexture(BackTexId, backSprite.texture);
            _uiInstance.SetVector(FrontRectId, frontRect);
            _uiInstance.SetVector(BackRectId, backRect);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(_block);
            _block.SetTexture(BackTexId, backSprite.texture);
            _block.SetVector(FrontRectId, frontRect);
            _block.SetVector(BackRectId, backRect);
            spriteRenderer.SetPropertyBlock(_block);
        }
    }

    private void ApplyShade(float shade)
    {
        if (_uiInstance != null)
        {
            _uiInstance.SetFloat(ShadeId, shade);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(_block);
            _block.SetFloat(ShadeId, shade);
            spriteRenderer.SetPropertyBlock(_block);
        }
    }
    
    public void SetFaceDown(bool faceDown)
    {
        _tween?.Kill();
        transform.localRotation = Quaternion.Euler(0f, faceDown ? 180f : 0f, 0f);
        _lastAngle = -1f;
    }
    
    private static Vector4 ToRect(Sprite s)
    {
        Rect r = s.textureRect;
        Texture2D t = s.texture;
        return new Vector4(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height);
    }

    private void OnDestroy()
    {
        _tween?.Kill();
        if (_uiInstance != null) Destroy(_uiInstance);
    }
}
