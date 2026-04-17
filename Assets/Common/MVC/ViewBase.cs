using UnityEngine;

namespace MVC
{
    public abstract class ViewBase : MonoBehaviour
    {
        public abstract void UpdateInfo(ModelBase model);
    }
}

