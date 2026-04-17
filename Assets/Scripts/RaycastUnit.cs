using UnityEngine;
using System.Collections.Generic;

public class RaycastUnit : MonoBehaviour
{
    private Collider selfCollider;
    private HashSet<Vector3Int> coverRange = new HashSet<Vector3Int>();

    private void Start()
    {
        selfCollider = GetComponent<Collider>();
        UpdateCells();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateCells();
            transform.hasChanged = false;
        }
    }

    private void UpdateCells()
    {
        int cellSize = DataManager.cellSize;
        Bounds bounds = selfCollider.bounds;

        int minX = Mathf.FloorToInt(bounds.min.x / cellSize) * cellSize;
        int minY = Mathf.FloorToInt(bounds.min.y / cellSize) * cellSize;
        int minZ = Mathf.FloorToInt(bounds.min.z / cellSize) * cellSize;

        int maxX = Mathf.FloorToInt(bounds.max.x / cellSize) * cellSize;
        int maxY = Mathf.FloorToInt(bounds.max.y / cellSize) * cellSize;
        int maxZ = Mathf.FloorToInt(bounds.max.z / cellSize) * cellSize;

        HashSet<Vector3Int> newRange = new HashSet<Vector3Int>();

        for (int x = minX; x <= maxX; x += cellSize)
        for (int y = minY; y <= maxY; y += cellSize)
        for (int z = minZ; z <= maxZ; z += cellSize)
        {
            Vector3Int key = new Vector3Int(x, y, z);
            newRange.Add(key);

            if (!coverRange.Contains(key))
            {
                DataManager.Instance.AddUnitToCell(key, this);
            }
        }

        foreach (var oldCell in coverRange)
        {
            if (!newRange.Contains(oldCell))
            {
                DataManager.Instance.RemoveUnitFromCell(oldCell, this);
            }
        }

        coverRange = newRange;
    }

    public bool CheckHit(Ray ray, float maxDist, out float hitDist)
    {
        RaycastHit hit;
        if (selfCollider.Raycast(ray, out hit, maxDist))
        {
            hitDist = hit.distance;
            return true;
        }
        hitDist = maxDist;
        return false;
    }
}
