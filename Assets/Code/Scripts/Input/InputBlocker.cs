using UnityEngine;

public class InputBlocker : MonoBehaviour
{
    private void OnEnable() => InputGate.Add();

    private void OnDisable() => InputGate.Remove();
}
