using UnityEngine;

public interface INavigation
{
    Transform transform { get; }
    
    bool enabled { get; set; }
    float speed { get; set; }
    float remainingDistance { get; }
    bool hasPath { get; }
    Vector3 Destination { get; set; }
    
    void SetDestination(Vector3 target);
    void Stop();
    bool CalculatePath(Vector3 targetPosition);
}
