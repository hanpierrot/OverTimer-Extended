using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Pull-back aiming on the felt: press anywhere, drag away from where the ball should go, release to shoot.
/// Direction is cue minus pointer, converted to TABLE space through the FeltMapper (never from normalized
/// coordinates, which are stretched 2:1). Right-click or a very short pull cancels.
///
/// While the pointer is down it draws the cue stick, the power bar and the aim assist for the round's difficulty.
/// Everything shown comes from the QUANTIZED shot (the same one that will be fired), so what you see is exactly what
/// the simulation will receive. The lines and the ghost ball are drawn at runtime under `aimLayer`.
public class CueAimController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform felt;
    [SerializeField] private RectTransform aimLayer;             // stretched over the felt; the stick, lines and ghost live here
    [SerializeField] private PowerBarView powerBar;
    [SerializeField] private Sprite ghostSprite;                 // the cue ball sprite, drawn at low alpha
    [SerializeField] private BallSpriteSet ballSprites;          // for the Trick Shot preview (one faint ball per ball)
    [SerializeField] private Sprite stickSprite;                 // optional; a plain placeholder is generated when empty
    [SerializeField, Range(0.2f, 1f)] private float maxPullFraction = 0.6f;   // of the felt height
    [SerializeField, Range(0f, 0.3f)] private float minPower = 0.05f;

    [Header("Cue stick (canvas units)")]
    [SerializeField] private float stickLength = 150f;
    [SerializeField] private float stickThickness = 6f;
    [SerializeField] private float stickGap = 4f;                // between the stick tip and the ball when idle
    [SerializeField] private float stickPullBack = 40f;          // extra distance at full power

    [Header("Trick Shot card")]
    [SerializeField] private float previewInterval = 0.1f;       // seconds between simulations while the pointer moves
    [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.5f;

    [Header("Assist (canvas units)")]
    [SerializeField] private float predictionLength = 90f;       // object ball line at a full hit / cue line at the thinnest cut
    [SerializeField] private float reflectLength = 45f;
    [SerializeField] private Color pathColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color objectColor = new Color(1f, 0.85f, 0.3f, 0.95f);
    [SerializeField] private Color deflectColor = new Color(0.6f, 0.85f, 1f, 0.95f);
    [SerializeField] private Color stubColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.35f;

    public event Action<ShotInput> Fired;

    private FeltMapper _mapper;
    private Func<BilliardsSim> _sim;
    private float _ballDiameter;                                 // felt units
    private AimAssistLevel _assist = AimAssistLevel.DirectionOnly;
    private bool _active;
    private bool _aiming;

    private RectTransform _stick;
    private Image _ghost;
    private readonly RectTransform[] _lines = new RectTransform[4];     // path, object ball, cue deflection, cushion bounce
    private Sprite _dotSprite, _stickGenerated;

    public bool IsAiming => _aiming;

    /// sim returns the sim currently on the table. ballDiameter: the drawn ball size in felt units.
    public void Configure(FeltMapper mapper, Func<BilliardsSim> sim, float ballDiameter)
    {
        _mapper = mapper;
        _sim = sim;
        _ballDiameter = ballDiameter;
        if (_ghost != null) _ghost.rectTransform.sizeDelta = Vector2.one * ballDiameter;
    }

    public void SetAssist(AimAssistLevel level) => _assist = level;

    public void SetActive(bool active)
    {
        _active = active;
        if (!active) Cancel();
    }

    public void Cancel()
    {
        _aiming = false;
        HideAim();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_active || _sim == null) return;
        if (e.button == PointerEventData.InputButton.Right) { Cancel(); return; }
        if (e.button != PointerEventData.InputButton.Left) return;
        _aiming = true;
        UpdateAim(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_aiming) UpdateAim(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_aiming || e.button != PointerEventData.InputButton.Left) return;
        _aiming = false;
        bool ok = TryCompute(e, out ShotInput shot, out _);
        HideAim();
        if (!ok || shot.power < minPower) return;
        Fired?.Invoke(shot);
    }

    /// The shot as the simulation will get it: quantized angle and power. cueFelt is the cue ball's felt position.
    private bool TryCompute(PointerEventData e, out ShotInput shot, out Vector2 cueFelt)
    {
        shot = null; cueFelt = default;
        var sim = _sim != null ? _sim() : null;
        if (sim == null) return false;
        int cue = sim.IndexOf("cue");
        if (cue < 0 || sim.IsPotted(cue)) return false;
        cueFelt = _mapper.TableToFelt(sim.Position(cue));

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(felt, e.position, e.pressEventCamera, out Vector2 local)) return false;
        Vector2 pull = cueFelt - local;                           // drag away from the target: the ball goes the other way
        float length = pull.magnitude;
        if (length < 1e-3f) return false;

        Vector2 dirTable = _mapper.FeltDirToTable(pull);
        float power = Mathf.Clamp01(length / (felt.rect.height * maxPullFraction));
        shot = ShotInput.Quantized(FeltMapper.AngleDeg(dirTable), power);
        return true;
    }

    // ================= drawing =================

    private void UpdateAim(PointerEventData e)
    {
        if (!TryCompute(e, out ShotInput shot, out Vector2 cueFelt)) { HideAim(); return; }
        EnsureParts();

        Vector2 dirTable = shot.Direction;
        Vector2 dirFelt = _mapper.TableDirToFelt(dirTable);
        float radius = _ballDiameter * 0.5f;

        // stick: tip behind the ball along -dir, pulled back as the power grows
        _stick.gameObject.SetActive(true);
        _stick.anchoredPosition = cueFelt - dirFelt * (radius + stickGap + shot.power * stickPullBack);
        _stick.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dirFelt.y, dirFelt.x) * Mathf.Rad2Deg);

        if (powerBar != null) powerBar.Set(shot.power, minPower);
        DrawAssist(dirTable, dirFelt, cueFelt, radius);

        if (BilliardsPendingEffects.TrickShotPending) DrawTrickPreview(shot);
        else HidePreviews();
    }

    // ================= Trick Shot card =================

    private readonly List<Image> _previews = new List<Image>();
    private ShotInput _previewShot;
    private float _previewAt = -1f;

    /// Runs this exact shot on a COPY of the simulation and shows every ball where it ends up (a ball that drops is shown
    /// in its pocket). At most previewInterval apart, and not at all again for an unchanged shot.
    private void DrawTrickPreview(ShotInput shot)
    {
        if (ballSprites == null) return;
        bool same = _previewShot != null && _previewShot.angle == shot.angle && _previewShot.power == shot.power;
        if (same || Time.unscaledTime - _previewAt < previewInterval) return;

        var sim = _sim();
        if (sim == null) return;
        var copy = sim.Clone();
        copy.Shoot(shot);
        copy.RunToRest();

        int used = 0;
        for (int i = 0; i < copy.BallCount; i++)
        {
            if (sim.IsPotted(i)) continue;                        // already off the table before this shot
            string id = copy.IdOf(i);
            Vector2 where = copy.Position(i);
            bool dropped = copy.IsPotted(i);
            if (dropped)
                foreach (var pot in copy.Report.pots)
                    if (pot.ball == id) where = BilliardsSim.PocketCentre(pot.pocket);

            Image img = Preview(used++);
            img.sprite = ballSprites.Get(id);
            img.color = new Color(dropped ? 0.65f : 1f, dropped ? 0.65f : 1f, dropped ? 0.65f : 1f, previewAlpha);
            img.rectTransform.sizeDelta = Vector2.one * _ballDiameter;
            img.rectTransform.anchoredPosition = _mapper.TableToFelt(where);
            img.gameObject.SetActive(true);
        }
        for (int i = used; i < _previews.Count; i++) _previews[i].gameObject.SetActive(false);

        _previewShot = shot;
        _previewAt = Time.unscaledTime;
    }

    private Image Preview(int index)
    {
        while (_previews.Count <= index)
        {
            var go = NewImage("TrickShotBall" + _previews.Count, aimLayer != null ? aimLayer : felt);
            go.transform.SetAsFirstSibling();                     // under the assist lines, the ghost ball and the stick
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            _previews.Add(go.GetComponent<Image>());
        }
        return _previews[index];
    }

    private void HidePreviews()
    {
        foreach (var img in _previews) if (img != null) img.gameObject.SetActive(false);
        _previewShot = null;
    }

    private void DrawAssist(Vector2 dirTable, Vector2 dirFelt, Vector2 cueFelt, float radius)
    {
        for (int i = 0; i < _lines.Length; i++) _lines[i].gameObject.SetActive(false);
        _ghost.gameObject.SetActive(false);

        if (_assist == AimAssistLevel.DirectionOnly)
        {
            Line(0, cueFelt + dirFelt * radius, cueFelt + dirFelt * (radius + 1.5f * _ballDiameter), stubColor);
            return;
        }

        var sim = _sim();
        if (!AimPredictor.Predict(sim, dirTable, out AimPrediction p)) return;
        Vector2 endFelt = _mapper.TableToFelt(p.end);
        Line(0, cueFelt + dirFelt * radius, endFelt, pathColor);                 // the dotted path starts at the ball's edge

        if (p.hitsBall)
        {
            _ghost.gameObject.SetActive(true);
            _ghost.rectTransform.anchoredPosition = endFelt;
            if (_assist == AimAssistLevel.FullGhost)
            {
                Vector2 objFelt = _mapper.TableDirToFelt(p.objectDir);
                Vector2 cueDeflect = _mapper.TableDirToFelt(p.cueDir);
                Vector2 ballFelt = _mapper.TableToFelt(sim.Position(p.ballIndex));
                Line(1, ballFelt, ballFelt + objFelt * (p.cut * predictionLength), objectColor);
                float deflectLen = (1f - p.cut) * predictionLength;
                if (deflectLen > 2f) Line(2, endFelt, endFelt + cueDeflect * deflectLen, deflectColor);
            }
        }
        else if (_assist == AimAssistLevel.FullGhost)
        {
            Vector2 bounce = _mapper.TableDirToFelt(p.reflectedDir);
            Line(3, endFelt, endFelt + bounce * reflectLength, deflectColor);
        }
    }

    private void Line(int slot, Vector2 a, Vector2 b, Color color)
    {
        var rt = _lines[slot];
        rt.gameObject.SetActive(true);
        rt.GetComponent<Image>().color = color;
        UiSegment.Set(rt, a, b);
    }

    private void HideAim()
    {
        if (_stick != null) _stick.gameObject.SetActive(false);
        if (_ghost != null) _ghost.gameObject.SetActive(false);
        for (int i = 0; i < _lines.Length; i++) if (_lines[i] != null) _lines[i].gameObject.SetActive(false);
        HidePreviews();
        if (powerBar != null) powerBar.Set(0f, minPower);
    }

    private void EnsureParts()
    {
        if (_stick != null) return;
        Transform parent = aimLayer != null ? aimLayer : felt;

        for (int i = 0; i < _lines.Length; i++)
        {
            var go = NewImage("AssistLine" + i, parent);
            var img = go.GetComponent<Image>();
            img.sprite = DotSprite();
            img.type = Image.Type.Tiled;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(1f, 2f);
            _lines[i] = rt;
        }

        var ghost = NewImage("GhostBall", parent);
        _ghost = ghost.GetComponent<Image>();
        _ghost.sprite = ghostSprite;
        _ghost.color = new Color(1f, 1f, 1f, ghostAlpha);
        var grt = _ghost.rectTransform;
        grt.anchorMin = grt.anchorMax = Vector2.zero;
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = Vector2.one * _ballDiameter;

        var stick = NewImage("CueStick", parent);
        var simg = stick.GetComponent<Image>();
        simg.sprite = stickSprite != null ? stickSprite : (_stickGenerated = MakeStickSprite());
        _stick = simg.rectTransform;
        _stick.anchorMin = _stick.anchorMax = Vector2.zero;
        _stick.pivot = new Vector2(1f, 0.5f);                    // the tip is the right end
        _stick.sizeDelta = new Vector2(stickLength, stickThickness);
        HideAim();
    }

    private GameObject NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = gameObject.layer };
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    /// 4x2 texels: a 2x2 dot then a 2x2 gap, repeated along the line (1 texel = 1 canvas unit).
    private Sprite DotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        var tex = new Texture2D(4, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "AimDot" };
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, x < 2 ? Color.white : Color.clear);
        tex.Apply(false, true);
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, 4, 2), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return _dotSprite;
    }

    /// Placeholder stick: pale ferrule, wooden shaft, dark butt. One texel per canvas unit, tip on the right.
    private Sprite MakeStickSprite()
    {
        int w = Mathf.Max(8, Mathf.RoundToInt(stickLength)), h = Mathf.Max(2, Mathf.RoundToInt(stickThickness));
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "CueStick" };
        Color ferrule = new Color32(236, 232, 214, 255), shaft = new Color32(204, 154, 82, 255), butt = new Color32(70, 46, 30, 255), edge = new Color32(120, 82, 40, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int fromTip = w - 1 - x;
                Color c = fromTip < 4 ? ferrule : x < w * 0.25f ? butt : shaft;
                if (c == shaft && (y == 0 || y == h - 1)) c = edge;
                tex.SetPixel(x, y, c);
            }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(1f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private void OnDestroy()
    {
        if (_dotSprite != null) { Destroy(_dotSprite.texture); Destroy(_dotSprite); }
        if (_stickGenerated != null) { Destroy(_stickGenerated.texture); Destroy(_stickGenerated); }
    }
}
