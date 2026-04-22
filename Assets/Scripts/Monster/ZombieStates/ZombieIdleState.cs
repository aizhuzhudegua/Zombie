using UnityEngine;

public class ZombieIdleState : IState<ZombieController>
{
    private const string clipName = "Idel";

    public ZombieIdleState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Owner.GpuAnimator.PlayAnimationWithTransition(clipName);
        Owner.GetComponent<AgentNavigation>().enabled = false;
    }

    public override void Update()
    {
       
    }

    public override void Exit()
    {
    }

    private void OnAnimationEnd(string name)
    {

    }
}
