using UnityEngine;

/// <summary>Base class for anything that lives on the laptop screen (Mining, Blackjack, ...).</summary>
public abstract class LaptopApp : MonoBehaviour
{
    [SerializeField] private string appName;
    public string AppName => appName;

    public virtual void OnAppOpened() { }
    public virtual void OnAppClosed() { }
}
