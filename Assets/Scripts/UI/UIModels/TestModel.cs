using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MVC;

public class TestModel : SingletonModel<TestModel>
{

    private int count = 0;

    public int Count {

        set
        {
            count = value;
            updateEvent?.Invoke(this);
        }
        get
        {
            return count;
        }

    }

    public override void Init()
    {
    }

    public override void SaveData()
    {
    }

    public override void UpdateInfo()
    {
        
    }

}
