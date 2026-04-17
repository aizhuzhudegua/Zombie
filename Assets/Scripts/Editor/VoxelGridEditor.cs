using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public struct VoxelCell
{
    public Vector3Int index;
    public Vector3 center;
    
    public VoxelCell(Vector3Int idx, Vector3 c)
    {
        index = idx;
        center = c;
    }
}

public class VoxelGridGenerator : MonoBehaviour
{
    [Header("体素设置")]
    public Vector3Int gridSize = new Vector3Int(50, 10, 50);
    public float cellSize = 2f;
    
    [Header("显示设置")]
    public bool showBounds = true;
    public Color boundsColor = Color.green;
    public Color occupiedColor = Color.red;
    
    [Header("网格数据")]
    public VoxelCell[,,] cells;
    
    [ContextMenu("生成网格")]
    public void GenerateGrid()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        
        cells = new VoxelCell[gridSize.x, gridSize.y, gridSize.z];
        
        Vector3 halfSize = new Vector3(
            gridSize.x * cellSize * 0.5f,
            gridSize.y * cellSize * 0.5f,
            gridSize.z * cellSize * 0.5f
        );
        Vector3 startPos = transform.position - halfSize;
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3 center = startPos + new Vector3(
                        (x + 0.5f) * cellSize,
                        (y + 0.5f) * cellSize,
                        (z + 0.5f) * cellSize
                    );
                    
                    cells[x, y, z] = new VoxelCell(new Vector3Int(x, y, z), center);
                }
            }
        }
        
        sw.Stop();
        int totalCells = gridSize.x * gridSize.y * gridSize.z;
        float memMB = (totalCells * 24f) / (1024f * 1024f);
        
        Debug.Log($"体素网格生成: {totalCells:N0} 格 | {sw.ElapsedMilliseconds}ms | ~{memMB:F1}MB");
    }
    
    public bool TryGetCell(Vector3 worldPos, out VoxelCell cell)
    {
        Vector3 localPos = worldPos - (transform.position - new Vector3(
            gridSize.x * cellSize * 0.5f,
            gridSize.y * cellSize * 0.5f,
            gridSize.z * cellSize * 0.5f
        ));
        
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.y / cellSize);
        int z = Mathf.FloorToInt(localPos.z / cellSize);
        
        if (x >= 0 && x < gridSize.x && 
            y >= 0 && y < gridSize.y && 
            z >= 0 && z < gridSize.z)
        {
            cell = cells[x, y, z];
            return true;
        }
        
        cell = default;
        return false;
    }
    
    public VoxelCell GetCellDirect(int x, int y, int z)
    {
        return cells[x, y, z];
    }
    
    public void GetCellsAlongRay(Vector3 rayOrigin, Vector3 rayDir, float maxDist, List<VoxelCell> result)
    {
        result.Clear();
        
        if (cells == null) return;
        
        Vector3 currentPos = rayOrigin;
        Vector3 step = rayDir.normalized * cellSize;
        float remainingDist = Mathf.Min(maxDist, 500f);
        
        for (int i = 0; i < 500; i++)
        {
            if (remainingDist <= 0) break;
            
            if (TryGetCell(currentPos, out VoxelCell cell))
            {
                if (result.Count == 0 || result[result.Count - 1].index != cell.index)
                {
                    result.Add(cell);
                }
            }
            
            currentPos += step;
            remainingDist -= cellSize;
        }
    }
    
    void OnDrawGizmos()
    {
      
    }
}
