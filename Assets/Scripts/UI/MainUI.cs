using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MVC
{
    public static partial class UIRegister
    {

        static partial void Register(UIManager uIManager)
        {
            uIManager.Register(typeof(TestController), "UI/TestUI", true);
        }

    }
}


public class MainUI : MonoBehaviour
{

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void ShowTest()
    {
        TestController.Show();
    }

    public void CloseTest()
    {
        TestController.Close();
    }
}
