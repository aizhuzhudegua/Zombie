using UnityEngine;

public interface IBulletTarget
{
    public void OnBulletHit(int damage,Vector3 hitPoint);
}
