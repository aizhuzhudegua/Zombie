using UnityEngine;

namespace MVC
{

    public abstract class ControllerBase<T,M,V> : MonoBehaviour where T: MonoBehaviour where V : ViewBase where M : ModelBase
    {
        private static T controller = default(T);

        public static T Controller
        {
            get { return controller; }
        }

        protected V view; 
    
        public static void Show()
        {
            controller = UIManager.Instance.Show<T>();
        }

        public static void Close()
        {
            if (controller != null)
            {
                controller = UIManager.Instance.Close<T>();
            }
        }

        protected void Start()
        {
            // 三个固定步骤
            view = GetComponent<V>();
            InitializeView();
            SetupViewListeners();
            SetupDataListeners();
        }

        protected virtual void UpdateInfo(ModelBase model)
        {

            if (view != null)
            {
                view.UpdateInfo(model);
            }
        }

        public abstract void InitializeView();
        
        public abstract void SetupViewListeners();

        public abstract void SetupDataListeners();

    }
}
