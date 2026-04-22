using UnityEngine;

public class ZombieRunState : IState<ZombieController>
{
    public ZombieRunState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Owner.GpuAnimator.PlayAnimationWithTransition("Run");
        Owner.GetComponent<AgentNavigation>().enabled = true;
    }

    public override void Update()
    {

        
    }

    public override void Exit()
    {
    }
}
