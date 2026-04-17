using UnityEngine;

public class ZombieRunState : IState<ZombieController>
{
    public ZombieRunState(StateMachine<ZombieController> stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (Owner.navigation != null)
        {
            Owner.navigation.enabled = true;
            Owner.navigation.speed = 3.5f;
        }
    }

    public override void Update()
    {
        if (Owner.attackTarget != null)
        {
            // Owner.navigation?.SetDestination(Owner.attackTarget.transform.position);
        }

        //if (Owner.attackTarget != null && 
        //    Vector3.Distance(Owner.transform.position, Owner.attackTarget.transform.position) < 2f)
        //{
        //    stateMachine.ChangeState<ZombieAttackState>();
        //}
    }

    public override void Exit()
    {
    }
}
