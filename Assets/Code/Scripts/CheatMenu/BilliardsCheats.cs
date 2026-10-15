using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Billiards test buttons for the cheat menu: the debug overlay and a forced table mirror. Adds its buttons to the
/// same list the cutscene buttons use, so CheatMenuController itself is not touched. State lives in BilliardsDebug.
public class BilliardsCheats : MonoBehaviour
{
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    private TMP_Text _overlayLabel;
    private TMP_Text _mirrorLabel;

    private void Start()
    {
        if (buttonContainer == null || buttonPrefab == null) return;

        _overlayLabel = AddButton(ToggleOverlay);
        _mirrorLabel = AddButton(CycleMirror);
        BilliardsDebug.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy() => BilliardsDebug.Changed -= Refresh;

    private TMP_Text AddButton(UnityEngine.Events.UnityAction onClick)
    {
        Button btn = Instantiate(buttonPrefab, buttonContainer);
        btn.onClick.AddListener(onClick);
        return btn.GetComponentInChildren<TMP_Text>();
    }

    private static void ToggleOverlay() => BilliardsDebug.Overlay = !BilliardsDebug.Overlay;

    private static void CycleMirror() =>
        BilliardsDebug.Mirror = (BilliardsDebug.MirrorMode)(((int)BilliardsDebug.Mirror + 1) % 5);

    private void Refresh()
    {
        if (_overlayLabel != null) _overlayLabel.text = "Billiards overlay: " + (BilliardsDebug.Overlay ? "ON" : "off");
        if (_mirrorLabel != null) _mirrorLabel.text = "Billiards mirror: " + BilliardsDebug.Mirror;
    }
}
