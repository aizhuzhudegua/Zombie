using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ZombieStateMachine : StateMachine<ZombieController>
{
    public ZombieStateMachine(ZombieController owner) : base(owner)
    {
        
    }

    public void OnAnimationEnd(string name)
    {
        foreach (var item in states)
        {
            if(item.Value is IGPUAnimationEndEventHandler)
            {
                (item.Value as IGPUAnimationEndEventHandler).OnAnimationEnd(name);
            }
        }
    }
}
