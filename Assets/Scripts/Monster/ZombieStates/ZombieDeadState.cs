using UnityEngine;

public class ZombieDeadState : IState<ZombieController>
{
    public ZombieDeadState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (Owner.navigation != null)
            Owner.navigation.enabled = false;
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
       
    }
}
