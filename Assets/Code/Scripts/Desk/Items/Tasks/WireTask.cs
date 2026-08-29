using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PointerReceiver))]
public class WireTask : TaskBase, IPawnable
{
    [Header("Refs")]
    [SerializeField] private LaptopController laptopController;
    [SerializeField] private RectTransform wireArea;
    [SerializeField] private GameObject plugGameObject;
    
    [Header("Pairs")]
    [SerializeField] private WirePlug[] plugs;
    [SerializeField] private WireSocket[] sockets;
    [SerializeField] private WireLineView[] connectedLines;
    
    [SerializeField] private WireLineView dragPreviewLine;
    [SerializeField] private Color[] wireColors;
    
    [Header("Pawn")]
    [SerializeField] private int pawnValue = 20;
    public int PawnValue => pawnValue;

    private PointerReceiver _receiver;
    private bool[] _connected;
    private int _connectedCount;
    private WirePlug _activePlug;
    
    private void Awake()
    {
        _receiver = GetComponent<PointerReceiver>();
        
        plugGameObject.SetActive(false);
        _connected = new bool[plugs.Length];
        
        foreach (var p in plugs)
        {
            p.DragBegan += HandlePlugDragBegan;
            p.DragMoved += HandlePlugDragMoved;
            p.DragEnded += HandlePlugDragEnded;
        }

        foreach (var s in sockets)
            s.Dropped += HandleSocketDropped;
    }

    public void BeginPuzzle()
    {
        ResetCompletion();
        _connectedCount = 0;
        Array.Clear(_connected, 0, _connected.Length);

        foreach (var line in connectedLines) line.gameObject.SetActive(false);
        dragPreviewLine.gameObject.SetActive(false);
        _activePlug = null;
        
        if(laptopController != null) laptopController.Disabled =  true;

        ShuffleSockets();
    }

    private void ShuffleSockets()
    {
        int n = plugs.Length;
        var order = new int[n];
        for(int i = 0; i < n; i++) order[i] = i;
        
        var rng = RngService.Instance.Random;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (int i = 0; i < n; i++)
        {
            plugs[i].Setup(i, wireColors[i]);
            sockets[i].Setup(order[i], wireColors[order[i]]);
        }
    }

    private void HandlePlugDragBegan(WirePlug plug)
    {
        if (_connected[plug.PairIndex]) return;
        _activePlug = plug;
        dragPreviewLine.gameObject.SetActive(true);
        dragPreviewLine.SetLine(plug.AnchoredPosition, plug.AnchoredPosition, plug.WireColor);
    }
    
    private void HandlePlugDragMoved(PointerEventData e)
    {
        if (_activePlug == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(wireArea, e.position, e.pressEventCamera, out Vector2 local))
            return;

        dragPreviewLine.SetLine(_activePlug.AnchoredPosition, local, _activePlug.WireColor);
    }

    private void HandlePlugDragEnded()
    {
        dragPreviewLine.gameObject.SetActive(false);
        _activePlug = null;
    }

    private void HandleSocketDropped(WireSocket socket)
    {
        if (_activePlug == null) return;
        if (socket.PairIndex != _activePlug.PairIndex) { _activePlug = null; return; }
        
        int pairIndex = _activePlug.PairIndex;
        _connected[pairIndex] = true;
        _connectedCount++;

        connectedLines[pairIndex].gameObject.SetActive(true);
        connectedLines[pairIndex].SetLine(_activePlug.AnchoredPosition, socket.AnchoredPosition, _activePlug.WireColor);

        _activePlug = null;

        if (_connectedCount >= plugs.Length) CompleteTask();
    }
    
    protected override void OnTaskCompleted()
    {
        if (laptopController != null) laptopController.Disabled = false;
        if(plugGameObject != null) plugGameObject.SetActive(true);
        
        _receiver.SetInteractable(false);
    }

    public void OnPawned()
    {
        if (laptopController != null) laptopController.Disabled = true;

        Destroy(gameObject);
    }
}
