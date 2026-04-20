using UnityEngine;

public class FlowFieldNavigation : MonoBehaviour, INavigation
{
    public Transform transform => base.transform;

    [SerializeField] internal float moveSpeed        = 0.5f;
    [SerializeField] internal float separationRadius = 1f;
    [SerializeField] internal float smoothTime       = 0.1f;

    // Job 读取 / 主线程 Apply
    internal Vector3 lastDirection;
    internal bool    isEnabled = true;

    // ── INavigation ───────────────────────────────────────────
    public bool enabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    public float speed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float remainingDistance
    {
        get
        {
            var sys = FlowFieldNavigationSystem.Instance;
            if (sys == null) return float.MaxValue;
            int cost = sys.GetCost(transform.position);
            return cost == int.MaxValue ? float.MaxValue : cost * sys.CellSize;
        }
    }

    public bool hasPath
    {
        get
        {
            var sys = FlowFieldNavigationSystem.Instance;
            if (sys == null) return false;
            int cost = sys.GetCost(transform.position);
            return cost != int.MaxValue && cost >= 0;
        }
    }

    public Vector3 Destination { get => _destination; set => _destination = value; }
    private Vector3 _destination;

    public void SetDestination(Vector3 target)  => _destination = target;
    public void Stop()                          => isEnabled = false;
    public bool CalculatePath(Vector3 target)   { SetDestination(target); return true; }

    // ── 生命周期 ──────────────────────────────────────────
    void OnEnable()  => FlowFieldNavigationSystem.Instance?.Register(this);
    void OnDisable() => FlowFieldNavigationSystem.Instance?.Unregister(this);
}