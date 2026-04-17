using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerGunSelector : MonoBehaviour
{
    [SerializeField]
    private GunType Gun;
    [SerializeField]
    private Transform GunParent;
    [SerializeField]
    private List<GunScriptableObject> Guns;
    [SerializeField]
    private ShootSystemIKManager shootSystemIKManager;

    [Space]
    [Header("Runtime Filled")]
    public GunScriptableObject ActiveGun;

    private void Start()
    {
        GunScriptableObject gun = Guns.Find(gun => gun.Type == Gun);
        if (gun == null)
        {
            Debug.LogError($"No GunScriptableObject found for GunType: {Gun}");
        }

        ActiveGun = gun;
        Weapon weapon = gun.Spawn(GunParent, this);
        
        if (shootSystemIKManager != null)
        {
            shootSystemIKManager.SetLeftHandIKTarget(weapon.LeftHandIKGoal);
        }
    }
}
