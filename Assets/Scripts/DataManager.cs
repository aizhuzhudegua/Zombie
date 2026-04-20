using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    [HideInInspector]
    public const int cellSize = 2;

    public HashSet<Vector2Int> UsedCellDict;
    public LinkedList<RaycastUnit>[,] UnitCells;

    private HashSet<Vector2Int> obstacleCells;

    public  const int MaxCellCountX = 20;
    public  const int MaxCellCountZ = 1000;
    
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UnitCells = new LinkedList<RaycastUnit>[MaxCellCountX, MaxCellCountZ];
        UsedCellDict = new HashSet<Vector2Int>(100);
        obstacleCells = new HashSet<Vector2Int>(100);
    }
    
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    public void AddUnitToCell(Vector3Int cellIndex, RaycastUnit unit)
    {
        int x = cellIndex.x;
        int z = cellIndex.z;
        if (x < 0 || x >= MaxCellCountX || z < 0 || z >= MaxCellCountZ) return;

        var key = new Vector2Int(x, z);
        if (!UsedCellDict.Contains(key))
        {
            UnitCells[x, z] = new LinkedList<RaycastUnit>();
            UsedCellDict.Add(key);
        }
        UnitCells[x, z].AddLast(unit);
    }

    public void RemoveUnitFromCell(Vector3Int cellIndex, RaycastUnit unit)
    {
        int x = cellIndex.x;
        int z = cellIndex.z;
        if (x < 0 || x >= MaxCellCountX || z < 0 || z >= MaxCellCountZ) return;

        var key = new Vector2Int(x, z);
        if (UsedCellDict.Contains(key))
            UnitCells[x, z].Remove(unit);
    }

    public void SetObstacle(Vector2Int cell, bool isObstacle)
    {
        if (isObstacle)
            obstacleCells.Add(cell);
        else
            obstacleCells.Remove(cell);
    }

    public void SetObstacle(Vector3 worldPos, bool isObstacle)
    {
        Vector2Int cell = WorldToCell(worldPos);
        SetObstacle(cell, isObstacle);
    }

    public bool IsObstacle(Vector2Int cell)
    {
        return obstacleCells.Contains(cell);
    }
    
    public bool TryGetCell(int x, int z, out LinkedList<RaycastUnit> cellUnits)
    {
        if(x < 0 || x >= MaxCellCountX || z < 0 || z >= MaxCellCountZ)
        {
            cellUnits = null;
            return false;
        }

        if (UsedCellDict.Contains(new Vector2Int(x, z)))
        {
            cellUnits = UnitCells[x, z];
            return true;
        }

        cellUnits = null;
        return false;
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        Vector2Int cell = WorldToCell(worldPos);
        return IsWalkable(cell);
    }

    public bool IsWalkable(Vector2Int cell)
    {
        if (obstacleCells.Contains(cell))
            return false;
        return true;
    }


}
