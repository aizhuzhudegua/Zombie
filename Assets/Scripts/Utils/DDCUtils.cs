using UnityEngine;
using System.Collections.Generic;

public static class DDCUtils
{
    public static bool Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDist,
        out VoxelHitInfo hitInfo
    )
    {
        hitInfo = null;

        if (DataManager.Instance == null) return false;

        int cellSize = DataManager.cellSize;

        if (direction == Vector3.zero) return false;
        Vector3 dir = direction.normalized;

        int mapX = Mathf.FloorToInt(origin.x / cellSize) * cellSize;
        int mapY = Mathf.FloorToInt(origin.y / cellSize) * cellSize;
        int mapZ = Mathf.FloorToInt(origin.z / cellSize) * cellSize;

        int stepX = dir.x > 0 ? 1 : -1;
        int stepY = dir.y > 0 ? 1 : -1;
        int stepZ = dir.z > 0 ? 1 : -1;

        float tMaxX, tMaxY, tMaxZ;
        float tDeltaX = cellSize;
        float tDeltaY = cellSize;
        float tDeltaZ = cellSize;

        if (dir.x > 0)
            tMaxX = (mapX + 1) * cellSize - origin.x;
        else
            tMaxX = origin.x - mapX * cellSize;

        if (dir.y > 0)
            tMaxY = (mapY + 1) * cellSize - origin.y;
        else
            tMaxY = origin.y - mapY * cellSize;

        if (dir.z > 0)
            tMaxZ = (mapZ + 1) * cellSize - origin.z;
        else
            tMaxZ = origin.z - mapZ * cellSize;

        float currentMaxDist = maxDist;
        Ray ray = new Ray(origin, dir);
        HashSet<RaycastUnit> checkedUnits = new HashSet<RaycastUnit>();
        RaycastUnit nearestUnit = null;
        float nearestDist = currentMaxDist;

        while (true)
        {
            float minT = Mathf.Min(tMaxX, tMaxY, tMaxZ);
            if (minT > currentMaxDist) break;

            if (DataManager.Instance.TryGetCell(mapX, mapZ, out LinkedList<RaycastUnit> units))
            {
                foreach (RaycastUnit unit in units)
                {
                    if (checkedUnits.Contains(unit)) continue;
                    checkedUnits.Add(unit);

                    if (unit.CheckHit(ray, nearestDist, out float hitDist))
                    {
                        nearestDist = hitDist;
                        nearestUnit = unit;
                        currentMaxDist = nearestDist;
                    }
                }
            }

            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                mapX += stepX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxZ)
            {
                mapY += stepY;
                tMaxY += tDeltaY;
            }
            else
            {
                mapZ += stepZ;
                tMaxZ += tDeltaZ;
            }
        }

        if (nearestUnit != null)
        {
            hitInfo = new VoxelHitInfo
            {
                unit = nearestUnit,
                distance = nearestDist
            };
            return true;
        }

        return false;
    }
}

public class VoxelHitInfo
{
    public RaycastUnit unit;
    public float distance;
}
