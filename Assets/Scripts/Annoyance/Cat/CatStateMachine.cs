using System;
using System.Collections.Generic;
using UnityEngine;

public enum CatState
{
    Wandering,
    SittingIdle,
    SittingBlocking,
    Dragged
}

public class CatStateMachine
{
    public struct StateDefinition
    {
        public Action Enter;
        public Action Tick;
        public Action Exit;
    }
    
    public CatState CurrentState { get; private set; }
    public event Action<CatState, CatState> StateChanged;
    
    private readonly Dictionary<CatState, StateDefinition> _states = new();
    private bool _hasEntered;

    public void AddState(CatState id, StateDefinition definition) => _states[id] = definition;

    public void ChangeState(CatState next)
    {
        if (_hasEntered && CurrentState == next) return;

        CatState prev = CurrentState;
        if(_hasEntered) _states[CurrentState].Exit?.Invoke();
        
        CurrentState = next;
        _hasEntered = true;
        _states[next].Enter?.Invoke();
        
        StateChanged?.Invoke(CurrentState, next);
    }
    
    public void Tick() => _states[CurrentState].Tick?.Invoke();
}
