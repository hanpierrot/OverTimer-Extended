using UnityEngine;
using UnityEngine.InputSystem;

public class CheatMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private Key toggleKey = Key.F1;
    [SerializeField] private Key stopCutsceneKey = Key.F2;

    private void Awake()
    {
        if (cheatPanel != null) cheatPanel.SetActive(false);
        if (!Debug.isDebugBuild) enabled = false;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);
        
        if (Keyboard.current[stopCutsceneKey].wasPressedThisFrame)
            CutsceneController.Instance?.StopAll();
    }
}
