using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on the root of a scratch panel (Triple Match or Lucky Nine) and gives a ScratchTicket everything it needs from the
/// panel through one reference: the root object, the result label, the Cash Out button and the scratch windows. The windows
/// are collected from the children, so adding or removing one never needs a re-wire.
/// </summary>
public class ScratchPanelView : MonoBehaviour
{
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private Button closeButton;

    private ScratchWindow[] _windows;

    public TMP_Text ResultLabel => resultLabel;
    public Button CloseButton => closeButton;

    /// <summary>Every ScratchWindow under the panel, inactive ones included (the panel is saved inactive).</summary>
    public ScratchWindow[] Windows
    {
        get
        {
            if (_windows == null) _windows = GetComponentsInChildren<ScratchWindow>(true);
            return _windows;
        }
    }
}
