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
            shootSystemIKManager.Aim();

            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetRotateOnMove(false);

            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 1f, Time.deltaTime * 13f));

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

            if (starterAssetsInputs.shoot)
            {
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
                        target.OnBulletHit(GunSelector.ActiveGun.damage);
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
