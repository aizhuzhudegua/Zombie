using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshNavigation : MonoBehaviour, INavigation
{
    private NavMeshAgent agent;

    public Transform transform => agent.transform;
    
    public bool enabled
    {
        get => agent.enabled;
        set => agent.enabled = value;
    }
    
    public float speed
    {
        get => agent.speed;
        set => agent.speed = value;
    }
    
    public float remainingDistance => agent.remainingDistance;
    
    public bool hasPath => agent.hasPath;
    
    public Vector3 Destination
    {
        get => agent.destination;
        set => agent.destination = value;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetDestination(Vector3 target)
    {
        agent.SetDestination(target);
    }

    public void Stop()
    {
        agent.Stop();
    }

    public bool CalculatePath(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(targetPosition, path);
    }
}
