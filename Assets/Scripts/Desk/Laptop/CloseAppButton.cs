using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One shared close-button script for the whole laptop UI. Set
/// closeWholeLaptop on the Laptop panel's own CloseButton to shut the whole
/// thing down and go back to the desk; leave it off on each app's CloseButton
/// (Mining, Blackjack, ...) to just drop back to the home screen.
/// </summary>
[RequireComponent(typeof(Button))]
public class CloseAppButton : MonoBehaviour
{
    [SerializeField] private LaptopController controller;
    [SerializeField] private bool closeWholeLaptop;

    private void Awake()
    {
        if (controller == null)
        {
            Debug.LogError($"{nameof(CloseAppButton)} on {name} has no Controller assigned - this button won't do anything.", this);
            return;
        }

        var button = GetComponent<Button>();
        button.onClick.AddListener(closeWholeLaptop ? controller.CloseLaptop : controller.CloseCurrentApp);
    }
}
