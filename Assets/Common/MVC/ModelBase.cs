using Common;
using System;
using UnityEngine.Events;

namespace MVC
{
    public abstract class ModelBase
    {
        public UnityEvent<ModelBase> updateEvent = new UnityEvent<ModelBase>();
        public abstract void Init();
        public abstract void SaveData();
        public abstract void UpdateInfo();
    }

    public abstract class SingletonModel<T> : ModelBase where T : new()
    {
        private static T instance;
        public static T Instance
        {
            get => Equals(instance, default(T)) ? (instance = new T()) : instance;
        }
    }
}


