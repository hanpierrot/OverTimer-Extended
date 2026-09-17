using UnityEngine;
using UnityEngine.InputSystem;

public class CheatMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private Key toggleKey = Key.F1;

    private void Awake()
    {
        if (cheatPanel != null) cheatPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);
    }
}
