using UnityEngine;

public class ZombieAttackState : IState<ZombieController> ,IGPUAnimationEndEventHandler
{
    private const string clipName = "Attack";
    public ZombieAttackState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

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


    void IGPUAnimationEndEventHandler.OnAnimationEnd(string name)
    {
        if (name == clipName)
        {
            Owner.stateMachine.ChangeState<ZombieRunState>();
        }
    }
}
