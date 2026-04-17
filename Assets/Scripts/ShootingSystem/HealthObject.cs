using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HealthObject :MonoBehaviour,IDamageable
{
    public int MaxHealth;
    public int CurrentHealth;

    public virtual void Hurt(int damage)
    {
        CurrentHealth -= damage;
        if(CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        
    }

}
