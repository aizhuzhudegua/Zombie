using Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MVC
{
    public enum UILayer
    {
        Background = 0,    // 背景层（游戏主界面）
        Normal = 1,        // 普通层（背包等）
        Top = 2,           // 顶层（对话框、确认框）
        Toast = 3          // 提示层（Toast、Notification）
    }

    public static partial class UIRegister
    {
        static partial void Register(UIManager uIManager);

        public static void RegisterAll(UIManager uIManager)
        {
            Register(uIManager);
        }
    }

    class UIElement
    {
        public string Resources;
        public bool Cache;
        public GameObject Instance;
        public UILayer Layer;
    }

    public class UIManager : Singleton<UIManager>
    {
        [SerializeField]
        private Dictionary<Type, UIElement> uiResources = new Dictionary<Type, UIElement>();

        [SerializeField]
        private Dictionary<UILayer, List<Type>> layerStack = new Dictionary<UILayer, List<Type>>();

        [SerializeField]
        private Dictionary<UILayer, Transform> layerParents = new Dictionary<UILayer, Transform>();

        public void Register(Type type, string path, bool cache = false, UILayer layer = UILayer.Normal)
        {
            uiResources.Add(type, new UIElement() 
            { 
                Resources = path, 
                Cache = cache,
                Layer = layer
            });
        }

        public void SetLayerParent(UILayer layer, Transform parent)
        {
            layerParents[layer] = parent;
        }

        public UIManager()
        {
            UIRegister.RegisterAll(this);
            
            for (int i = 0; i <= (int)UILayer.Toast; i++)
            {
                layerStack[(UILayer)i] = new List<Type>();
            }
        }

        public T Show<T>(UILayer layer = UILayer.Normal)
        {
            Type type = typeof(T);
            
            if (uiResources.ContainsKey(type))
            {
                UIElement info = uiResources[type];
                info.Layer = layer;
                
                HideLowerLayers(layer);
                
                layerStack[layer].Add(type);

                if (info.Instance != null)
                {
                    info.Instance.SetActive(true);
                }
                else
                {
                    UnityEngine.Object prefab = Resources.Load(info.Resources);
                    if (prefab == null)
                    {
                        return default(T);
                    }
                    
                    info.Instance = (GameObject)GameObject.Instantiate(prefab);
                    
                    if (layerParents.ContainsKey(layer))
                    {
                        info.Instance.transform.SetParent(layerParents[layer], false);
                    }
                }
                
                return info.Instance.GetComponent<T>();
            }
            return default(T);
        }

        private void HideLowerLayers(UILayer currentLayer)
        {
            int currentLayerIndex = (int)currentLayer;
            
            for (int i = 0; i < currentLayerIndex; i++)
            {
                UILayer lowerLayer = (UILayer)i;
                
                foreach (var t in layerStack[lowerLayer])
                {
                    if (uiResources.ContainsKey(t))
                    {
                        var uiInfo = uiResources[t];
                        if (uiInfo.Instance != null && uiInfo.Instance.activeSelf)
                        {
                            uiInfo.Instance.SetActive(false);
                        }
                    }
                }
            }
        }

        public void ShowLast()
        {
            for (int i = (int)UILayer.Toast; i >= 0; i--)
            {
                UILayer layer = (UILayer)i;
                var stack = layerStack[layer];
                
                if (stack.Count > 0)
                {
                    var lastType = stack[stack.Count - 1];
                    if (uiResources.ContainsKey(lastType))
                    {
                        var info = uiResources[lastType];
                        if (info.Instance != null)
                        {
                            info.Instance.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }

        public T Close<T>()
        {
            Type type = typeof(T);
            
            if (uiResources.ContainsKey(type))
            {
                UIElement info = uiResources[type];
                
                if (layerStack.ContainsKey(info.Layer))
                {
                    layerStack[info.Layer].Remove(type);
                }
                
                if (info.Cache)
                {
                    info.Instance.SetActive(false);
                }
                else
                {
                    GameObject.Destroy(info.Instance);
                    info.Instance = null;
                }
                
                ShowLast();
                
                return default(T);
            }
            return default(T);
        }
    }
}
