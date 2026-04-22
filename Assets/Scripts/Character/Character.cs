using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : HealthObject ,IDamageable
{
    public void Hurt(int damage)
    {
        
    }

    private void Awake()
    {
        GameManager.CharacterManager.AddCharacter(this);
    }

}
