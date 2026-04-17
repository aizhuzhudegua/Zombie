using UnityEngine;

public class ZombieWalkState : IState<ZombieController>
{
    public ZombieWalkState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (Owner.navigation != null)
        {
            Owner.navigation.enabled = true;
            Owner.navigation.speed = 0.3f;
        }
    }

    public override void Update()
    {

        
    }

    public override void Exit()
    {
    }
}
