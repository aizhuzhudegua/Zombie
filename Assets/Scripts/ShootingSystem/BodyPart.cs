using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPart : MonoBehaviour, IBulletTarget
{

    public float Weight;
    public HealthObject Owner;

    public void OnBulletHit(int damage)
    {
        Owner.Hurt((int)(Weight * damage));

    }


}
