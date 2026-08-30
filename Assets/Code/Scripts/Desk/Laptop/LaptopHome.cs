using UnityEngine;

/// <summary>
/// Sits on the Apps grid. Wires every DesktopIconView underneath it to the
/// laptop's app switcher, so dropping a new icon in the grid doesn't need any
/// extra code - just add the icon and set its appName.
/// </summary>
public class LaptopHome : MonoBehaviour
{
    [SerializeField] private LaptopController controller;

    private void Awake()
    {
        foreach (var icon in GetComponentsInChildren<DesktopIconView>(includeInactive: true))
            icon.Opened += controller.RequestOpen;
    }
}
