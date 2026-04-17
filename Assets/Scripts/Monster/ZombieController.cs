using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieController : HealthObject
{
    const float dis = 10f;

    public INavigation navigation;
    public StateMachine<ZombieController> stateMachine { get; private set; }

    public AudioSource audioSource;
    // public Animator animator;
    public CapsuleCollider capsuleCollider;
    public Zombie_Weapon weapon;

    public float viewAngle = 120f;

    public AudioClip[] FootstepAudioClips;
    public AudioClip[] IdelAudioClips;
    public AudioClip[] HurtAudioClips;
    public AudioClip[] AttackAudioClips;

    public Character attackTarget;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // animator = GetComponent<Animator>();
        navigation = GetComponent<INavigation>();
        // weapon.Init(this);

        stateMachine = new StateMachine<ZombieController>(this);
        stateMachine.RegisterState<ZombieIdleState>(new ZombieIdleState(stateMachine));
        stateMachine.RegisterState<ZombieWalkState>(new ZombieWalkState(stateMachine));
        stateMachine.RegisterState<ZombieRunState>(new ZombieRunState(stateMachine));
        stateMachine.RegisterState<ZombieAttackState>(new ZombieAttackState(stateMachine));
        stateMachine.RegisterState<ZombieHurtState>(new ZombieHurtState(stateMachine));
        stateMachine.RegisterState<ZombieDeadState>(new ZombieDeadState(stateMachine));

        Init();

    }

    public void Init()
    {
        // animator.SetTrigger("Init");
        CurrentHealth = MaxHealth;
        stateMachine.ChangeState<ZombieRunState>();
    }

    void Update()
    {
        stateMachine.Update();
    }


    /// <summary>
    /// 索敌逻辑
    /// </summary>
    public void SetTarget()
    {
        foreach (var item in GameManager.CharacterManager.Characters)
        {
            Character character = item.Value as Character;
            float distance = Vector3.Distance(transform.position, character.transform.position);
            if (distance < dis)
            {
                Vector3 dir = character.transform.position - transform.position;
                dir.y = 0;
                dir.Normalize();

                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                float angle = Vector3.Angle(forward, dir);
                if (angle < viewAngle)
                {
                    Vector3 rayStart = transform.position + Vector3.up * 1.5f;
                    if (Physics.Raycast(rayStart, dir, out RaycastHit hit, dis))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            stateMachine.ChangeState<ZombieRunState>();
                            attackTarget = character;
                            return;
                        }
                    }
                }
            }
        }
    }

    public override void Hurt(int value)
    {
        CurrentHealth -= (int)Mathf.Round(value);
        if (CurrentHealth <= 0)
        {
            stateMachine.ChangeState<ZombieDeadState>();
        }
        else
        {
            stateMachine.ChangeState<ZombieHurtState>();
        }
    }

    void Destroy()
    {
        ZombieManager.Instance.ZombieDead(this);
    }

// #if UNITY_EDITOR
//     void OnDrawGizmos()
//     {
//         Gizmos.color = Color.red;
//         Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
//         Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

//         Gizmos.DrawLine(transform.position, transform.position + leftBoundary * 10f);
//         Gizmos.DrawLine(transform.position, transform.position + rightBoundary * 10f);

//         int segments = 20;
//         float angleStep = viewAngle / segments;
//         Vector3 previousPoint = transform.position + leftBoundary * 10f;

//         for (int i = 1; i <= segments; i++)
//         {
//             float angle = -viewAngle / 2 + angleStep * i;
//             Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
//             Vector3 currentPoint = transform.position + direction * 10f;
//             Gizmos.DrawLine(previousPoint, currentPoint);
//             previousPoint = currentPoint;
//         }
//     }
// #endif

    #region 动画事件
    void IdelAudio()
    {
        if (Random.Range(0, 4) == 1)
        {
            audioSource.PlayOneShot(IdelAudioClips[Random.Range(0, IdelAudioClips.Length)]);
        }
    }
    void FootStep()
    {
        audioSource.PlayOneShot(FootstepAudioClips[Random.Range(0, IdelAudioClips.Length)]);
    }
    private void HurtAudio()
    {
        audioSource.PlayOneShot(HurtAudioClips[Random.Range(0, HurtAudioClips.Length)]);
    }
    private void AttackAudio()
    {
        audioSource.PlayOneShot(AttackAudioClips[Random.Range(0, AttackAudioClips.Length)]);
    }
    public void StartAttack()
    {
        weapon.StartAttack();
    }
    public void EndAttack()
    {
        weapon.EndAttack();
    }
    #endregion
}
