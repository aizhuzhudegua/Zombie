using UnityEngine;

public class ZombieDeadState : IState<ZombieController>,IGPUAnimationEndEventHandler
{
    private const string clipName = "Death";
    public ZombieDeadState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {

        Debug.Log(clipName);
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
            Owner.Die();
        }
    }
}
