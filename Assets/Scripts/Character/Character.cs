using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : HealthObject
{

    private void Awake()
    {
        GameManager.CharacterManager.AddCharacter(this);
    }

}
