using UnityEngine;

public class ZombieHurtState : IState<ZombieController>, IGPUAnimationEndEventHandler
{
    private float hurtStartTime;
    private const float hurtDuration = 0.5f;
    private bool hasTransitioned;
    private const string clipName = "Hurt";

    public ZombieHurtState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

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
        Owner.GetComponent<AgentNavigation>().enabled = true;
    }

    void IGPUAnimationEndEventHandler.OnAnimationEnd(string name)
    {
        if (name == clipName)
        {
            Owner.stateMachine.ChangeState<ZombieRunState>();
        }
    }
}
