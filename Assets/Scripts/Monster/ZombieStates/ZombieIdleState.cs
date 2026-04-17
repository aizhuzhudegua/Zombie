using UnityEngine;

public class ZombieIdleState : IState<ZombieController>
{
    public ZombieIdleState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
     
        if (Owner.navigation != null)
            Owner.navigation.enabled = false;
    }

    public override void Update()
    {
        if (Owner.navigation != null && Owner.navigation.remainingDistance <= 1f)
        {
            stateMachine.ChangeState<ZombieWalkState>();
        }
    }

    public override void Exit()
    {
    }
}
