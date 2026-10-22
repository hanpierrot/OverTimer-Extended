using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LaptopLock
{
    private static readonly HashSet<object> Keys = new();

    public static bool IsLocked => Keys.Count > 0;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        Reset();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Reset();
    
    private static void Reset()
    {
        Keys.Clear();
        Changed = null;
    }

    public static void Add(object key)
    {
        if (Keys.Add(key)) Changed?.Invoke();
    }

    public static void Remove(object key)
    {
        if (Keys.Remove(key)) Changed?.Invoke();
    }
}
