using UnityEngine;

public class WirePanelInitializer : MonoBehaviour
{
    [SerializeField] private WireTask task;

    private void OnEnable() => task.BeginPuzzle();
}
