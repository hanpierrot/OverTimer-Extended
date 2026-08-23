using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
public class DeskObjectFocus : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    
    private PointerReceiver receiver;

    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        if(panel != null) panel.SetActive(false);
    }
    
    private void OnEnable() => receiver.ClickDown += HandleClick;
    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if (panel != null) panel.SetActive(true);
    }
}
