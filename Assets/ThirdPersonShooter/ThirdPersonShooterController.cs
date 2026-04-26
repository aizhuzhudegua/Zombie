using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.Animations.Rigging;
using System;

public class ThirdPersonShooterController : MonoBehaviour {

    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawnBulletPosition;
    [SerializeField] private Transform vfxHitGreen;
    [SerializeField] private Transform vfxHitRed;
    [SerializeField] private PlayerGunSelector GunSelector;
    [SerializeField] private ShootSystemIKManager shootSystemIKManager;
    [SerializeField] private CinemachineImpulseSource source;
    // 旋转动画参数平滑缓存
    private float _currentRotation;
    private float _rotSmoothVel; // 平滑阻尼专用

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private Animator animator;
    [HideInInspector] public CinemachineBasicMultiChannelPerlin noise;

    private void Awake() {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        noise = aimVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>( );
    }

    Vector3 lastLookRotation;

    private void Update() {
        Vector3 mouseWorldPosition = Vector3.zero;
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        Transform hitTransform = null;
        
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
        {
            aimTarget.position = Vector3.Lerp(aimTarget.position, raycastHit.point, Time.deltaTime * 13f);
            mouseWorldPosition = raycastHit.point;
            hitTransform = raycastHit.transform;
        } 

        if (starterAssetsInputs.aim) {



            shootSystemIKManager.Aim(thirdPersonController.MainCamera.transform.forward);

            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetRotateOnMove(false);

            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 1f, Time.deltaTime * 13f));

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            // 身体转不过来才转脚
            if (Vector3.Dot(transform.forward, aimDirection) < 0.8)
            {
                // ============== 1. 固定角速度 0.78 rad/s 转向（替换原Lerp） ==============
                Vector3 flatAimDir = new Vector3(aimDirection.x, 0, aimDirection.z).normalized;
                Quaternion targetRot = Quaternion.LookRotation(flatAimDir);
                // 固定角速度旋转
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

                // ====================== 【修复：正确角速度计算】 ======================
                // 计算：角色当前方向 → 瞄准方向 的 带符号角度（左负右正）
                float turnAngle = Vector3.SignedAngle(transform.forward, flatAimDir, Vector3.up);
                // 归一化到 -1 ~ 1（适配脚部旋转动画）
                float targetRotation = Mathf.Clamp(turnAngle / 90f, -1f, 1f);
                // 平滑过渡
                _currentRotation = Mathf.SmoothDamp(_currentRotation, targetRotation, ref _rotSmoothVel, 0.1f);
            }
            else
            {
                // ============== 不满足条件时，平滑归零 ==============
                _currentRotation = Mathf.SmoothDamp(_currentRotation, 0f, ref _rotSmoothVel, 0.15f);
            }

            // 最终赋值给动画（无论是否满足条件，都用平滑后的值）
            animator.SetFloat("Rotation", _currentRotation);


            if (starterAssetsInputs.shoot)
            {
                source.GenerateImpulse(0.05f);
                shootSystemIKManager.Shot();
                // 射线从相机发射，加入spread
                Ray aimRay = ray;
                aimRay.direction += new Vector3(
                    UnityEngine.Random.Range(-GunSelector.ActiveGun.ShootConfig.spread.x, GunSelector.ActiveGun.ShootConfig.spread.x),
                    UnityEngine.Random.Range(-GunSelector.ActiveGun.ShootConfig.spread.y, GunSelector.ActiveGun.ShootConfig.spread.y),
                    UnityEngine.Random.Range(-GunSelector.ActiveGun.ShootConfig.spread.z, GunSelector.ActiveGun.ShootConfig.spread.z)
                );
                aimRay.direction.Normalize();
                
                GunSelector.ActiveGun.Shoot(aimRay, (RaycastHit hit)=> {
                    Transform other = hit.transform;
                    IBulletTarget target = other.GetComponent<IBulletTarget>();
                    if (other != null && target != null)
                    {
                        target.OnBulletHit(GunSelector.ActiveGun.damage , hit.point);
                        GameObject go = Instantiate(vfxHitGreen, hit.point, Quaternion.identity).gameObject ; 
                        go.transform.eulerAngles = new Vector3(-90,0,0);
                    }
                    else if (other != null)
                    {
                        GameObject go = Instantiate(vfxHitRed, hit.point, Quaternion.identity).gameObject;
                        go.transform.eulerAngles = new Vector3(-90, 0, 0);
                    }
                });
                starterAssetsInputs.shoot = false;
            }
        }
        else {


            shootSystemIKManager.Idle();
            starterAssetsInputs.shoot = false;
            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
            thirdPersonController.SetRotateOnMove(true);
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 13f));
        }
    }
}
