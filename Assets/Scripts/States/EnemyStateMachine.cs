using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
    private IEnemyState currentState;
    private Dictionary<System.Type, IEnemyState> states = new Dictionary<System.Type, IEnemyState>();

    public void AddState<T>(T state) where T : IEnemyState
    {
        states[typeof(T)] = state;
    }

    public void SetState<T>() where T : IEnemyState
    {
        var newState = states[typeof(T)];
        if (currentState != newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }

    public void UpdateState()
    {
        currentState?.Update();
    }
}

