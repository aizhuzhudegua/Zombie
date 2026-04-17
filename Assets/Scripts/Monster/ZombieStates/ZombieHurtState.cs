using UnityEngine;

public class ZombieHurtState : IState<ZombieController>
{
    private float hurtStartTime;
    private const float hurtDuration = 0.5f;
    private bool hasTransitioned;

    public ZombieHurtState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (Owner.navigation != null)
            Owner.navigation.enabled = false;
        
        hurtStartTime = Time.time;
        hasTransitioned = false;
    }

    public override void Update()
    {
        if (!hasTransitioned && Time.time - hurtStartTime >= hurtDuration)
        {
            if (Owner.stateMachine.currentState.GetType() != typeof(ZombieDeadState))
            {
                stateMachine.ChangeState<ZombieRunState>();
                hasTransitioned = true;
            }
        }
    }

    public override void Exit()
    {
    }
}
