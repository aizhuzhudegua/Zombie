using Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例实例（私有字段+公共只读属性，比直接公开字段更规范）
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("GameManager 实例为空！请确保场景中存在且唯一的 GameManager 对象");
            }
            return _instance;
        }
    }

    // 角色管理器（改为属性，增加空值检查）
    private static CharacterManager _characterManager;
    public static CharacterManager CharacterManager
    {
        get
        {
            if (_characterManager == null)
            {
                Debug.LogWarning("CharacterManager 未初始化，自动创建并初始化");
                _characterManager = new CharacterManager();
                _characterManager.Init();
            }
            return _characterManager;
        }
    }

    // 生成点数组（加Tooltip提示，方便编辑器赋值）
    [Tooltip("游戏生成点数组，至少需要1个点")]
    public Transform[] Points;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"检测到重复的 GameManager 实例，销毁重复对象（InstanceID：{GetInstanceID()}）");
            DestroyImmediate(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _instance = this;

        if (_characterManager == null)
        {
            Debug.Log("初始化 CharacterManager");
            _characterManager = new CharacterManager();
            _characterManager.Init();
        }

        if (Points == null || Points.Length == 0)
        {
            Debug.LogError("GameManager 的 Points 数组为空！请在编辑器中赋值至少1个生成点");
        }
    }

    void Start()
    {

    }

 
    public Vector3 GetPoints()
    {
        if (Points == null || Points.Length == 0)
        {
            Debug.LogError("无法获取生成点：Points 数组为空");
            return Vector3.zero;
        }

        int randomIndex = Random.Range(0, Points.Length);
        // 额外检查该Transform是否为空（防止赋值了空对象）
        if (Points[randomIndex] == null)
        {
            Debug.LogWarning($"索引 {randomIndex} 的生成点为空，返回Vector3.zero");
            return Vector3.zero;
        }

        return Points[randomIndex].position;
    }

}