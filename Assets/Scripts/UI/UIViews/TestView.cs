using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MVC;
using UnityEngine.UI;

public class TestView : ViewBase
{
    public Text count;
    public Button addBtn;

    public override void UpdateInfo(ModelBase model)
    {
        count.text = (model as TestModel).Count.ToString();
    }

}
