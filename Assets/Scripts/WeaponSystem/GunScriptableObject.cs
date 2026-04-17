using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
public class GunScriptableObject : ScriptableObject
{
   // public ImpackType ImpackType;
    public GunType Type;
    public string Name;
    public GameObject ModelPrefab;

    public int damage = 10;
   

    public ShootConfigurationScriptableObject ShootConfig;
    public TrailConfigScriptableObject TrailConfig;
    public Vector3 SpawnPoint;
    public Vector3 SpawnRotation;

    private MonoBehaviour ActiveMonoBehaviour;
    private GameObject Model;
    private float LastShootTime;
    private ParticleSystem ShootSystem;
    private ObjectPool<TrailRenderer> TrailPool;


    public Weapon Spawn(Transform Parent, MonoBehaviour ActiveMonoBehaviour)
    {
        
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        LastShootTime = 0;
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);

        Model = Instantiate(ModelPrefab);
        Model.transform.SetParent(Parent, false);
        Model.transform.localPosition = SpawnPoint;
        Model.transform.localRotation = Quaternion.Euler(SpawnRotation);
        

        ShootSystem = Model.GetComponentInChildren<ParticleSystem>();
        return Model.GetComponent<Weapon>();
    }

    public void Shoot(Ray aimRay, Action<RaycastHit> hitCallback)
    {
        if (Time.time > ShootConfig.FireRate + LastShootTime)
        {
            ShootSystem.Play();
            if(Physics.Raycast(
                aimRay.origin,
                aimRay.direction,
                out RaycastHit hit,
                float.MaxValue,
                ShootConfig.HitMask
            ))
            {
                ActiveMonoBehaviour.StartCoroutine(
                    PlayTrail(
                        ShootSystem.transform.position,
                        hit.point,
                        hit,
                        hitCallback
                        )
                );
            }
            else
            {
                ActiveMonoBehaviour.StartCoroutine(
                    PlayTrail(
                        ShootSystem.transform.position,
                        aimRay.origin + aimRay.direction * TrailConfig.MissDistance,
                        new RaycastHit(),
                        hitCallback
                        )
                );
            }
            LastShootTime = Time.time;

        }
    }

    private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 EndPoint, RaycastHit hit, Action<RaycastHit> hitCallback)
    {
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = StartPoint;
        yield return null;

        instance.emitting = true;

        float distance = Vector3.Distance(StartPoint, EndPoint);
        float remainingDistance = distance;
        while(remainingDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(
                StartPoint,
                EndPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
                );
            remainingDistance -= TrailConfig.SimualtionSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = EndPoint;
        if (hit.transform != null)
        {
            hitCallback?.Invoke(hit);
        }


        yield return new WaitForSeconds(TrailConfig.Duration);
        yield return null;

        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.Material;
        trail.widthCurve = TrailConfig.WidthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return trail;
    }
}
