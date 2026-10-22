using System;
using UnityEngine;

public static class InputGate
{
    private static int _count;

    public static bool IsBlocked => _count > 0;

    public static event Action<bool> BlockedChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _count = 0;
        BlockedChanged = null;
    }

    public static void Add()
    {
        if (_count++ == 0) BlockedChanged?.Invoke(true);
    }

    public static void Remove()
    {
        if (_count == 0) return;
        if (--_count == 0) BlockedChanged?.Invoke(false);
    }
}
