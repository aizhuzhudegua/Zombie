using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;


abstract class IKObj<T>
{
    public T constraint;
    public Transform IKTarget;
    public float smoothSpeed = 10f;
    public MonoBehaviour Owner;

    public virtual void Update()
    {
        
    }

    public virtual void SetOwner(MonoBehaviour owner)
    {
        Owner = owner;
    }

}

[Serializable]
class LeftHandIK:IKObj<TwoBoneIKConstraint>
{
    public Transform IKFollow;

    public override void Update()
    {
        if(constraint)
        {
            ShootSystemIKManager mgr = Owner as ShootSystemIKManager;
            if (mgr.curState == ShootSystemState.Idle)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 0, smoothSpeed * Time.deltaTime);
            }
            else if (mgr.curState == ShootSystemState.Aim)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 1f, smoothSpeed * Time.deltaTime);
            }

            IKTarget.position = Vector3.Lerp(
                IKTarget.position, 
                IKFollow.position, 
                Time.deltaTime * smoothSpeed
            );

            IKTarget.rotation = Quaternion.Lerp(
                 IKTarget.rotation,
                 IKFollow.rotation,
                 Time.deltaTime * smoothSpeed
            );

        }
    }
}


[Serializable]
class BodyAim : IKObj<MultiAimConstraint>
{
    public override void Update()
    {
        ShootSystemIKManager mgr = Owner as ShootSystemIKManager;
        if (mgr.curState == ShootSystemState.Idle)
        {
            constraint.weight = Mathf.Lerp(constraint.weight, 0, smoothSpeed * Time.deltaTime);
        }
        else if(mgr.curState == ShootSystemState.Aim)
        {
            constraint.weight = Mathf.Lerp(constraint.weight, 0.763f, smoothSpeed * Time.deltaTime);
        }
    }
}

[Serializable]
class Aim : IKObj<MultiAimConstraint>
{
    public override void Update()
    {
        ShootSystemIKManager mgr = Owner as ShootSystemIKManager;
        if (mgr.curState == ShootSystemState.Idle)
        {
            constraint.weight = Mathf.Lerp(constraint.weight, 0, smoothSpeed * Time.deltaTime);
        }
        else if (mgr.curState == ShootSystemState.Aim)
        {
            constraint.weight = Mathf.Lerp(constraint.weight, 1, smoothSpeed * Time.deltaTime);
        }
    }
}

[Serializable]
class RightHand : IKObj<TwoBoneIKConstraint>
{
    // 序列化参数，Unity编辑器可调
    [Header("淡入时间(0→1)")]
    public float fadeInTime = 0.1f;
    [Header("淡出时间(1→0)")]
    public float fadeOutTime = 0.5f;

    // 动画状态
    private enum AnimState { Idle, FadeIn, FadeOut }
    private AnimState _currentState = AnimState.Idle;
    private float _timer = 0f;

    /// <summary>
    /// 调用后播放：0→1→0 的权重动画
    /// 多次调用会重新触发动画
    /// </summary>
    public void Shot()
    {
        // 重置状态，重新开始淡入
        _currentState = AnimState.FadeIn;
        _timer = 0f;
    }

    public override void Update()
    {
        // 约束为空直接返回
        if (constraint == null) return;

        switch (_currentState)
        {
            case AnimState.Idle:
                // 闲置状态保持权重为0
                constraint.weight = 0;
                break;

            case AnimState.FadeIn:
                // 淡入：0 → 1
                _timer += Time.deltaTime;
                // 平滑插值
                constraint.weight = Mathf.Lerp(0, 0.5f, _timer / fadeInTime);

                // 淡入完成，切换淡出
                if (_timer >= fadeInTime)
                {
                    _timer = 0f;
                    _currentState = AnimState.FadeOut;
                }
                break;

            case AnimState.FadeOut:
                // 淡出：1 → 0
                _timer += Time.deltaTime;
                // 平滑插值
                constraint.weight = Mathf.Lerp(0.5f, 0, _timer / fadeOutTime);

                // 淡出完成，切换闲置
                if (_timer >= fadeOutTime)
                {
                    _currentState = AnimState.Idle;
                    constraint.weight = 0;
                }
                break;
        }
    }
}

public enum ShootSystemState
{
    Idle,
    Aim
}



public class ShootSystemIKManager : MonoBehaviour
{
    [Header("LeftHand IK Settings")]
    [SerializeField] private LeftHandIK leftHandIK;

    [Header("Body IK Settings")]
    [SerializeField] private BodyAim BodyIK;

    [Header("Aim IK Settings")]
    [SerializeField] private Aim AimIK;

    [Header("Shot Settings")]
    [SerializeField] private RightHand rightHandIK;

    public ShootSystemState curState = ShootSystemState.Idle;

    public bool Locked = false;

    void Start()
    {
        leftHandIK.SetOwner(this);
        BodyIK.SetOwner(this);
        AimIK.SetOwner(this);
        rightHandIK.SetOwner(this);
    }

    public void Aim(Vector3 LookDir)
    {
        Vector3 aimDir = new Vector3(LookDir.x, 0 , LookDir.z).normalized;
        if (Vector3.Dot(aimDir, transform.forward) < 0)
        {
            // 防止超过180度ik旋转出现问题
            return;
        }
        curState = ShootSystemState.Aim;

    }

    public void Idle()
    {
        curState = ShootSystemState.Idle;
    }

    private void Awake()
    {
        RegisterStates();
    }

    
    private void RegisterStates()
    {
       
    }
 

    public void SetLeftHandIKTarget(Transform target)
    {
        leftHandIK.IKFollow = target;
    }
    
    private void Update()
    {
        if (Locked) return;
        leftHandIK.Update();
        BodyIK.Update();
        AimIK.Update();
        rightHandIK.Update();
    }

    public void Shot()
    {
        rightHandIK.Shot();
    }
    
    private void OnDrawGizmos()
    {
  
    }
}
