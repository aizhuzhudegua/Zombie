using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<K> where K :MonoBehaviour
{

    public IState<K> currentState = null;

    public Dictionary<Type, IState<K>> states = new Dictionary<Type, IState<K>>();

    public K Owner;
    
    public StateMachine(K owner)
    {
        Owner = owner;
    }

    public void RegisterState<T>(IState<K> state) where T : IState<K>
    {
        if (states.ContainsKey(typeof(T)))
        {
            Debug.LogErrorFormat("State {0} already exited.", typeof(T));
            return;
        }
        states.Add(typeof(T), state);
    }

    public void RemoveState<T>() where T : IState<K>
    {
        if (!states.ContainsKey(typeof(T)))
        {
            Debug.LogErrorFormat("State {0} not exited.", typeof(T));
            return;
        }
        states.Remove(typeof(T));
    }

    public void ChangeState<T>() where T : IState<K>
    {
        IState<K> state = null;
        if (states.TryGetValue(typeof(T),out state))
        {

            if (currentState != state)
            {
                if (currentState != null)
                {
                    currentState.Exit();
                }
                state.Enter();
                currentState = state;

                // Debug.LogFormat("Current State: {0}", typeof(T));
            }
        }
        else
            Debug.LogErrorFormat("State {0} not exited.", typeof(T).ToString());
        
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
}
