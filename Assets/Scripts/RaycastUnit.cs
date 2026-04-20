using UnityEngine;

public class RaycastUnit : MonoBehaviour
{
    [SerializeField] private float radius = 0.5f;

    // 复用，避免每帧 new HashSet 产生 GC
    private readonly System.Collections.Generic.HashSet<Vector3Int> _coverRange = new System.Collections.Generic.HashSet<Vector3Int>();
    private readonly System.Collections.Generic.HashSet<Vector3Int> _newRange   = new System.Collections.Generic.HashSet<Vector3Int>();
    private readonly System.Collections.Generic.List<Vector3Int>    _toRemove   = new System.Collections.Generic.List<Vector3Int>();

    private void Start()
    {
        UpdateCells();
    }

    private void Update()
    {
        if (!transform.hasChanged) return;
        transform.hasChanged = false;
        UpdateCells();
    }

    private void UpdateCells()
    {
        int     cellSize = DataManager.cellSize;
        Vector3 center   = transform.position;
        float   r        = radius;

        // 以圆心外接方块范围迭代格子
        int minX = Mathf.FloorToInt((center.x - r) / cellSize);
        int minZ = Mathf.FloorToInt((center.z - r) / cellSize);
        int maxX = Mathf.FloorToInt((center.x + r) / cellSize);
        int maxZ = Mathf.FloorToInt((center.z + r) / cellSize);
        int y    = Mathf.FloorToInt(center.y       / cellSize);

        float rSq = r * r;

        _newRange.Clear();

        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            // 格子中心世界坐标
            float cx = x * cellSize + cellSize * 0.5f;
            float cz = z * cellSize + cellSize * 0.5f;
            float dx = cx - center.x;
            float dz = cz - center.z;
            if (dx * dx + dz * dz > rSq) continue;

            var key = new Vector3Int(x, y, z);
            _newRange.Add(key);

            if (!_coverRange.Contains(key))
                DataManager.Instance.AddUnitToCell(key, this);
        }

        // 移除不再覆盖的格子
        _toRemove.Clear();
        foreach (var old in _coverRange)
        {
            if (!_newRange.Contains(old))
                _toRemove.Add(old);
        }
        for (int i = 0; i < _toRemove.Count; i++)
            DataManager.Instance.RemoveUnitFromCell(_toRemove[i], this);

        _coverRange.Clear();
        foreach (var cell in _newRange)
            _coverRange.Add(cell);
    }

    /// <summary>射线与广告板平面求交（简化各向等采样）</summary>
    public bool CheckHit(Ray ray, float maxDist, out float hitDist)
    {
        // 射线与球体求交
        Vector3 oc = ray.origin - transform.position;
        float   b  = Vector3.Dot(oc, ray.direction);
        float   c  = Vector3.Dot(oc, oc) - radius * radius;
        float   discriminant = b * b - c;

        if (discriminant < 0f) { hitDist = maxDist; return false; }

        float t = -b - Mathf.Sqrt(discriminant);
        if (t < 0f) t = -b + Mathf.Sqrt(discriminant); // 射线起点在球内
        if (t < 0f || t > maxDist) { hitDist = maxDist; return false; }

        hitDist = t;
        return true;
    }
}
