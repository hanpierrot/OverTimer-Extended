using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class DeskObjectFocus : MonoBehaviour
{
    [Header("Focus Settings")]
    [SerializeField] private Vector3 focusedPosition = Vector3.zero;
    [SerializeField] private Vector3 focusedScale = Vector3.one * 2f;
    [SerializeField] private int focusedSortingOrder = 100;
    [SerializeField] private Behaviour[] componentsToEnableOnFocus;
    [SerializeField] private GameObject gameObjectToEnableOnFocus;
    
    private PointerReceiver receiver;
    private SpriteRenderer[] renderers;
    private Collider2D ownCollider;
    private Collider2D[] childColliders;
    
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private int[] originalSortingOrders;

    private bool isFocused;

    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
        ownCollider = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        childColliders = GetComponentsInChildren<Collider2D>(true).Where(c => c != ownCollider).ToArray();
        
        originalPosition = transform.position;
        originalScale = transform.localScale;
        
        originalSortingOrders = new int[renderers.Length];
        for(int i = 0; i < renderers.Length; i++)
            originalSortingOrders[i] = renderers[i].sortingOrder;
        
        SetInteractable(false);
        SetChildColliders(false);
    }
    
    private void OnEnable() => receiver.ClickDown += HandleClick;
    private void OnDisable() => receiver.ClickDown -= HandleClick;

    private void HandleClick(Vector2 worldPos)
    {
        if (!isFocused) Focus();
    }

    private void Focus()
    {
        CountdownTimer.Instance.PauseCountdown();
        
        if(isFocused)  return;
        isFocused = true;
        
        transform.position = focusedPosition;
        transform.localScale = focusedScale;
        
        int baseOffset = focusedSortingOrder - originalSortingOrders[0];

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = originalSortingOrders[i] + baseOffset;
        
        ownCollider.enabled = false;
        SetInteractable(true);
        SetChildColliders(true);
        
        if(gameObjectToEnableOnFocus != null) gameObjectToEnableOnFocus.SetActive(true);
        
        InputManager.Instance.FocusRoot = transform;
    }
    
    public void Unfocus()
    {
        if (!isFocused) return;
        isFocused = false;

        transform.position = originalPosition;
        transform.localScale = originalScale;

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = originalSortingOrders[i];

        ownCollider.enabled = true;
        SetInteractable(false);
        SetChildColliders(false);
        
        if(gameObjectToEnableOnFocus != null) gameObjectToEnableOnFocus.SetActive(false);
        
        if (InputManager.Instance.FocusRoot == transform)
            InputManager.Instance.FocusRoot = null;
        
        CountdownTimer.Instance.ResumeCountdown();
    }

    public void ReleaseFocusLock()
    {
        if (InputManager.Instance.FocusRoot == transform)
            InputManager.Instance.FocusRoot = null;
    }
    
    private void SetInteractable(bool value)
    {
        foreach (var comp in componentsToEnableOnFocus)
        {
            comp.enabled = value;
        }
    }
    
    private void SetChildColliders(bool value)
    {
        foreach (var col in childColliders)
        {
            if (col != null) col.enabled = value;
        }
    }
}
