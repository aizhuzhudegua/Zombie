using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MVC;
using System;

public class TestController : ControllerBase<TestController, TestModel, TestView>
{
    public override void InitializeView()
    {
        view.UpdateInfo(TestModel.Instance);
    }

    public override void SetupDataListeners()
    {
        TestModel.Instance.updateEvent.AddListener(UpdateInfo);
    }


    private void OnDestroy()
    {
        TestModel.Instance.updateEvent.RemoveListener(UpdateInfo);
    }

    public override void SetupViewListeners()
    {
        view.addBtn.onClick.AddListener(() =>
        {
            TestModel.Instance.Count++;
        });
    }
    
}
