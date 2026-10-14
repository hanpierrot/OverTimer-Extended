using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// Pull-back aiming on the felt: press anywhere, drag away from where the ball should go, release to shoot.
/// Direction is cue minus pointer, converted to TABLE space through the FeltMapper (never from normalized
/// coordinates, which are stretched 2:1). Right-click or a very short pull cancels.
/// Basic version for Phase 3: one feedback line, no cue stick, power bar or assist yet.
public class CueAimController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform felt;
    [SerializeField] private RectTransform line;                 // pivot (0, 0.5), a thin Image
    [SerializeField, Range(0.2f, 1f)] private float maxPullFraction = 0.6f;   // of the felt height
    [SerializeField, Range(0f, 0.3f)] private float minPower = 0.05f;

    public event Action<ShotInput> Fired;

    private FeltMapper _mapper;
    private Func<Vector2> _cueFeltPosition;
    private bool _active;
    private bool _aiming;

    public bool IsAiming => _aiming;

    /// cueFeltPosition returns the cue ball's current felt-local position.
    public void Configure(FeltMapper mapper, Func<Vector2> cueFeltPosition)
    {
        _mapper = mapper;
        _cueFeltPosition = cueFeltPosition;
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active) Cancel();
    }

    public void Cancel()
    {
        _aiming = false;
        if (line != null) line.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_active || _cueFeltPosition == null) return;
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
        bool ok = TryCompute(e, out Vector2 dirTable, out float power);
        if (line != null) line.gameObject.SetActive(false);
        if (!ok || power < minPower) return;
        Fired?.Invoke(ShotInput.Quantized(FeltMapper.AngleDeg(dirTable), power));
    }

    private bool TryCompute(PointerEventData e, out Vector2 dirTable, out float power)
    {
        dirTable = default; power = 0f;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(felt, e.position, e.pressEventCamera, out Vector2 local)) return false;

        Vector2 pull = _cueFeltPosition() - local;                // drag away from the target: the ball goes the other way
        float length = pull.magnitude;
        if (length < 1e-3f) return false;

        dirTable = _mapper.FeltDirToTable(pull);
        power = Mathf.Clamp01(length / (felt.rect.height * maxPullFraction));
        return true;
    }

    private void UpdateAim(PointerEventData e)
    {
        if (line == null) return;
        if (!TryCompute(e, out Vector2 dirTable, out float power)) { line.gameObject.SetActive(false); return; }

        Vector2 cue = _cueFeltPosition();
        Vector2 dirFelt = _mapper.TableDirToFelt(dirTable);
        line.gameObject.SetActive(true);
        UiSegment.Set(line, cue, cue + dirFelt * (power * felt.rect.width * 0.35f));   // length shows the power for now
    }
}
