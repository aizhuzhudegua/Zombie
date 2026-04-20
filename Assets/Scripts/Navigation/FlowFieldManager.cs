using UnityEngine;
using System.Collections.Generic;

public class FlowFieldManager : MonoBehaviour
{
    public static FlowFieldManager Instance { get; private set; }

    public int cellSize => DataManager.cellSize;
    public int MaxCellCountX => DataManager.MaxCellCountX;
    public int MaxCellCountZ => DataManager.MaxCellCountZ;

    public int[,] costField;
    public Vector2Int[,] flowField;
    private Vector2Int targetCell;
    private int maxCost;

    private static readonly Vector2Int[] CardinalDirs = new Vector2Int[4]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] AllDirs = new Vector2Int[8]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
    };

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        costField = new int[MaxCellCountX, MaxCellCountZ];
        flowField = new Vector2Int[MaxCellCountX, MaxCellCountZ];
    }

    private void Start()
    {
        
    }

    public void SetTarget(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        targetCell = new Vector2Int(x, z);
        UpdateCostField();
        UpdateFlowField();
    }

    public void UpdateCostField()
    {
        for (int x = 0; x < MaxCellCountX; x++)
        for (int z = 0; z < MaxCellCountZ; z++)
        {
            costField[x, z] = int.MaxValue;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(targetCell);
        costField[targetCell.x, targetCell.y] = 0;
        maxCost = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentCost = costField[current.x, current.y];

            foreach (var dir in CardinalDirs)
            {
                Vector2Int neighbor = current + dir;

                if (!IsInBounds(neighbor.x, neighbor.y)) continue;
                if (costField[neighbor.x, neighbor.y] != int.MaxValue) continue;
                if (!IsWalkable(neighbor)) continue;

                int newCost = currentCost + 1;
                costField[neighbor.x, neighbor.y] = newCost;
                maxCost = Mathf.Max(maxCost, newCost);
                queue.Enqueue(neighbor);
            }
        }
    }

    public void UpdateFlowField()
    {
        for (int x = 0; x < MaxCellCountX; x++)
        for (int z = 0; z < MaxCellCountZ; z++)
        {
            if (costField[x, z] == int.MaxValue)
            {
                flowField[x, z] = Vector2Int.zero;
                continue;
            }

            Vector2Int bestDir = Vector2Int.zero;
            int bestCost = costField[x, z];

            foreach (var dir in AllDirs)
            {
                Vector2Int neighbor = new Vector2Int(x + dir.x, z + dir.y);

                if (!IsInBounds(neighbor.x, neighbor.y)) continue;
                if (!IsWalkable(neighbor)) continue;

                if (costField[neighbor.x, neighbor.y] < bestCost)
                {
                    bestCost = costField[neighbor.x, neighbor.y];
                    bestDir = dir;
                }
            }

            flowField[x, z] = bestDir;
        }
    }

    private bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < MaxCellCountX && z >= 0 && z < MaxCellCountZ;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        Vector3 worldPos = CellToWorld(cell);
        return DataManager.Instance.IsWalkable(worldPos);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize + cellSize * 0.5f, 0, cell.y * cellSize + cellSize * 0.5f);
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    public int GetCost(Vector3 worldPos)
    {
        Vector2Int cell = WorldToCell(worldPos);
        if (!IsInBounds(cell.x, cell.y)) return int.MaxValue;
        return costField[cell.x, cell.y];
    }

    public Vector2Int GetDir(Vector3 worldPos)
    {
        Vector2Int cell = WorldToCell(worldPos);
        if (!IsInBounds(cell.x, cell.y)) return Vector2Int.zero;
        return flowField[cell.x, cell.y];
    }

    public Vector2Int GetDir(Vector2Int cell)
    {
        if (!IsInBounds(cell.x, cell.y)) return Vector2Int.zero;
        return flowField[cell.x, cell.y];
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        return DataManager.Instance.IsWalkable(worldPos);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (flowField == null) return;

        for (int x = 0; x < MaxCellCountX; x++)
        {
            for (int z = 0; z < MaxCellCountZ; z++)
            {
                Vector3 center = CellToWorld(new Vector2Int(x, z));

                if (!IsWalkable(new Vector2Int(x, z)))
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
                    continue;
                }

                Vector2Int dir = flowField[x, z];
                if (dir != Vector2Int.zero)
                {
                    Gizmos.color = Color.blue;
                    Vector3 dir3D = new Vector3(dir.x, 0, dir.y).normalized;
                    Gizmos.DrawRay(center, dir3D * cellSize * 0.4f);
                }
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(CellToWorld(targetCell), cellSize * 0.5f);
    }
#endif
}
