using UnityEngine;
using System.Collections.Generic;

public class FlowFieldNavigation : MonoBehaviour, INavigation
{
    public Transform transform => base.transform;

    [SerializeField]
    private float moveSpeed = 3f;

    [SerializeField]
    private float separationRadius = 1f;

    [SerializeField]
    private float smoothTime = 0.1f;

    private const float FlowWeight = 0.1f;
    private const float SeparationWeight = 0.5f;
    private const float AvoidanceWeight = 0.3f;

    private Vector3 smoothVelocity;
    private Vector3 lastDirection;

    public bool enabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }
    private bool isEnabled = true;

    public float speed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float remainingDistance
    {
        get
        {
            if (FlowFieldManager.Instance == null) return float.MaxValue;
            return FlowFieldManager.Instance.GetCost(transform.position) * FlowFieldManager.Instance.cellSize;
        }
    }

    public bool hasPath
    {
        get
        {
            if (FlowFieldManager.Instance == null) return false;
            int cost = FlowFieldManager.Instance.GetCost(transform.position);
            return cost != int.MaxValue && cost >= 0;
        }
    }

    public Vector3 Destination
    {
        get => destination;
        set => destination = value;
    }
    private Vector3 destination;

    public void SetDestination(Vector3 target)
    {
        destination = target;
    }

    public void Stop()
    {
        isEnabled = false;
    }

    public bool CalculatePath(Vector3 targetPosition)
    {
        SetDestination(targetPosition);
        return true;
    }

    void Update()
    {
        if (!isEnabled) return;
        if (FlowFieldManager.Instance == null) return;

        Vector2Int flowDir = FlowFieldManager.Instance.GetDir(transform.position);

        if (flowDir != Vector2Int.zero)
        {
            Vector3 flowForce = new Vector3(flowDir.x, 0, flowDir.y).normalized * FlowWeight;

            Vector3 separationForce = CalculateSeparation() * SeparationWeight;

            Vector3 avoidanceForce = CalculateAvoidance() * AvoidanceWeight;

            Vector3 totalForce = flowForce + separationForce + avoidanceForce;

            if (totalForce.magnitude > 0.01f)
            {
                Vector3 smoothDirection = Vector3.SmoothDamp(lastDirection, totalForce.normalized, ref smoothVelocity, smoothTime);

                if (smoothDirection.magnitude > 0.1f)
                {
                    transform.position += smoothDirection * moveSpeed * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(smoothDirection);
                    lastDirection = smoothDirection;
                }
            }
        }
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int count = 0;

        if (DataManager.Instance.UnitCells == null) return separation;

        Vector2Int selfCell = FlowFieldManager.Instance.WorldToCell(transform.position);
        int cellRadius = Mathf.CeilToInt(separationRadius / FlowFieldManager.Instance.cellSize);

        int minX = Mathf.Max(0, selfCell.x - cellRadius);
        int maxX = Mathf.Min(FlowFieldManager.Instance.MaxCellCountX - 1, selfCell.x + cellRadius);
        int minZ = Mathf.Max(0, selfCell.y - cellRadius);
        int maxZ = Mathf.Min(FlowFieldManager.Instance.MaxCellCountZ - 1, selfCell.y + cellRadius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (x == selfCell.x && z == selfCell.y) continue;

                if (DataManager.Instance.UnitCells[x, z] != null)
                {
                    foreach (var unit in DataManager.Instance.UnitCells[x, z])
                    {
                        if (unit == null) continue;
                        if (unit.transform == transform) continue;

                        Vector3 offset = transform.position - unit.transform.position;
                        float dist = offset.magnitude;
                        if (dist < separationRadius && dist > 0)
                        {
                            separation += offset.normalized / dist;
                            count++;
                        }
                    }
                }
            }
        }

        return count > 0 ? separation.normalized : separation;
    }

    private Vector3 CalculateAvoidance()
    {
        Vector3 avoidance = Vector3.zero;
        float lookAhead = separationRadius * 2f;

        Vector2Int currentCell = FlowFieldManager.Instance.WorldToCell(transform.position);
        Vector2Int lookAheadCell = FlowFieldManager.Instance.WorldToCell(transform.position + transform.forward * lookAhead);

        Vector2Int[] checkCells = new Vector2Int[]
        {
            currentCell,
            lookAheadCell,
            new Vector2Int(currentCell.x + 1, currentCell.y),
            new Vector2Int(currentCell.x - 1, currentCell.y),
            new Vector2Int(currentCell.x, currentCell.y + 1),
            new Vector2Int(currentCell.x, currentCell.y - 1),
        };

        foreach (var cell in checkCells)
        {
            Vector3 cellWorldPos = FlowFieldManager.Instance.CellToWorld(cell);
            if (!FlowFieldManager.Instance.IsWalkable(cellWorldPos))
            {
                Vector3 offset = transform.position - cellWorldPos;
                float dist = offset.magnitude;
                if (dist < lookAhead && dist > 0)
                {
                    avoidance += offset.normalized * (1f - dist / lookAhead);
                }
            }
        }

        return avoidance;
    }
}
