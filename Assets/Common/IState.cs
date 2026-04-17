using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IState<T> where T :MonoBehaviour
{
    public StateMachine<T> stateMachine;

    public T Owner
    {
        get
        {
            return stateMachine.Owner;
        }
    }

    public IState(StateMachine<T> stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
