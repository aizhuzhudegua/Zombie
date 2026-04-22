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

    public ShootSystemState curState = ShootSystemState.Idle;

    public bool Locked = false;

    void Start()
    {
        leftHandIK.SetOwner(this);
        BodyIK.SetOwner(this);
        AimIK.SetOwner(this);
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
    }
    
    private void OnDrawGizmos()
    {
  
    }
}
