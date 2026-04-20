using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieManager : MonoBehaviour
{
    const int MaxNum = 1000;

    public static ZombieManager Instance;
    public GameObject prefab_Zombie;
    public List<ZombieController> zombies;

    private Queue<ZombieController> zombiePool = new Queue<ZombieController>();
    public Transform Pool;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        StartCoroutine(CheckZombie());
    }

    

    // 检查僵尸
    IEnumerator CheckZombie()
    {
        while (true)
        {
            // Debug.Log($"zombie数量：{zombies.Count}");
            yield return null;
            if (zombies.Count< MaxNum)
            {
                if (zombiePool.Count>0)
                {
                    ZombieController zb = zombiePool.Dequeue();
                    zb.transform.SetParent(transform);
                    zb.transform.position = GameManager.Instance.GetPoints();
                    zombies.Add(zb);
                    zb.gameObject.SetActive(true);
                    FlowFieldNavigation nav = zb.gameObject.AddComponent<FlowFieldNavigation> ();
                    zb.navigation = nav;
                    // zb.Init();
                }
                else
                {
                    GameObject zb = Instantiate(prefab_Zombie, GameManager.Instance.GetPoints(), Quaternion.identity, transform);
                    FlowFieldNavigation nav = zb.gameObject.AddComponent<FlowFieldNavigation> ();
                    ZombieController zombieController = zb.GetComponent<ZombieController>();
                    zombieController.navigation = nav;
                    zombies.Add(zombieController);
                }
            }
        }
    }

    private float interval = 1f;
    private float lastSetTime = 0f;

    public Transform target;

    private void Update()
    {
        //if (Time.time - lastSetTime >= interval)
        //{
        //    FlowFieldManager.Instance.SetTarget(target.position);
        //    lastSetTime = Time.time;
        //}
    }


    public void ZombieDead(ZombieController zombie)
    {
        zombies.Remove(zombie);
        zombiePool.Enqueue(zombie);
        zombie.gameObject.SetActive(false);
        zombie.transform.SetParent(Pool);

    }
}
