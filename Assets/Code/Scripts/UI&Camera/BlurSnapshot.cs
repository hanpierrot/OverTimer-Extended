using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen blur behind the devices, from ONE snapshot taken when the first device opens (not per frame).
/// Cheap blur without a shader: capture the frame, blit it into a 1/8-size bilinear RenderTexture, stretch it back up.
/// If the capture fails (some WebGL browsers) or GameConfig.deviceBlurEnabled is off, it stays a plain dark tint.
/// The RawImage has raycastTarget on so it also blocks the Meter's Feed button and the camera arrows (plan P4).
/// </summary>
public class BlurSnapshot : MonoBehaviour
{
    [SerializeField] private RawImage target;
    [Tooltip("Hidden for the one frame of the capture so the devices don't end up inside their own blur.")]
    [SerializeField] private CanvasGroup[] hideDuringCapture;
    [SerializeField] private Color tint = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private int downsample = 8;

    private RenderTexture _rt;
    private Coroutine _capture;

    public void Show()
    {
        if (target == null) return;
        target.gameObject.SetActive(true);
        target.enabled = true;
        target.texture = null;
        target.color = tint;                       // tint only until (or unless) the capture works

        var config = GameManager.Instance != null ? GameManager.Instance.GameConfig : null;
        if (config != null && !config.deviceBlurEnabled) return;
        if (_capture != null) StopCoroutine(_capture);
        _capture = StartCoroutine(Capture());
    }

    public void Hide()
    {
        if (_capture != null) { StopCoroutine(_capture); _capture = null; }
        SetHidden(false);
        if (target != null)
        {
            target.texture = null;
            target.gameObject.SetActive(false);
        }
        Release();
    }

    private IEnumerator Capture()
    {
        SetHidden(true);
        target.enabled = false;
        yield return new WaitForEndOfFrame();

        Texture2D shot = null;
        try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
        catch (Exception e) { Debug.LogWarning($"[Blur] capture failed, tint only: {e.Message}"); }

        SetHidden(false);
        target.enabled = true;
        _capture = null;
        if (shot == null) yield break;

        Release();
        int d = Mathf.Max(1, downsample);
        _rt = new RenderTexture(Mathf.Max(1, shot.width / d), Mathf.Max(1, shot.height / d), 0) { filterMode = FilterMode.Bilinear };
        Graphics.Blit(shot, _rt);                  // the downsample + bilinear upscale IS the blur
        Destroy(shot);
        target.texture = _rt;
        var shade = Color.Lerp(Color.white, tint, tint.a);   // darken the picture by the tint, but keep it fully opaque
        shade.a = 1f;
        target.color = shade;
    }

    private void SetHidden(bool hidden)
    {
        if (hideDuringCapture == null) return;
        foreach (var g in hideDuringCapture)
            if (g != null) g.alpha = hidden ? 0f : 1f;
    }

    private void Release()
    {
        if (_rt == null) return;
        _rt.Release();
        Destroy(_rt);
        _rt = null;
    }

    private void OnDestroy() => Release();
}
