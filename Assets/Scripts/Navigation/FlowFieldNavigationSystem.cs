using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

/// <summary>
/// 完全自包含的流场导航 Job 调度器。
/// 自己维护流场、障碍、BFS 计算；不依赖 FlowFieldManager / DataManager。
/// </summary>
public class FlowFieldNavigationSystem : MonoBehaviour
{
    public static FlowFieldNavigationSystem Instance { get; private set; }

    // ── Inspector 参数 ────────────────────────────────────────
    [SerializeField] private int   gridSizeX  = 20;
    [SerializeField] private int   gridSizeZ  = 100;
    [SerializeField] private float cellSize   = 2f;

    public float CellSize => cellSize;
    public int   MaxX     => gridSizeX;
    public int   MaxZ     => gridSizeZ;

    // ── 分离常量 ──────────────────────────────────────────────
    private const int   MaxUnitsPerCell  = 16;
    private const int   SepCellRadius    = 1;
    private const float SeparationWeight = 0.6f;
    private const float AvoidanceWeight  = 0.3f;

    // ── 单位列表 ──────────────────────────────────────────────
    private readonly List<FlowFieldNavigation> _units = new List<FlowFieldNavigation>(256);
    private TransformAccessArray _transformArray;

    // ── NativeArray：流场 & 障碍（Persistent）────────────────
    private NativeArray<int>  _flowFieldX;   // [x*maxZ+z] = dir.x
    private NativeArray<int>  _flowFieldZ;
    private NativeArray<int>  _costField;    // BFS cost，int.MaxValue = 不可达
    private NativeArray<bool> _obstacle;

    // ── NativeArray：单位数据（随单位数扩容）────────────────
    private NativeArray<UnitData> _unitData;
    private NativeArray<Vector3>  _results;

    // ── NativeArray：格子→单位映射（Persistent）──────────────
    private NativeArray<int> _cellUnitIndex;   // [cellIdx*MaxUnitsPerCell+slot]
    private NativeArray<int> _cellUnitCount;   // [cellIdx]
    private NativeArray<int> _unitCellIndex;   // [unitIdx]

    private int  _capacity;
    private int  _totalCells;
    private bool _flowDirty = true;

    // BFS 队列（复用，避免每帧 GC）
    private readonly Queue<int> _bfsQueue = new Queue<int>(1024);

    // ═════════════════════════════════════════════════════════
    // Unity 生命周期
    // ═════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _totalCells = gridSizeX * gridSizeZ;
        _flowFieldX    = new NativeArray<int>( _totalCells,                  Allocator.Persistent);
        _flowFieldZ    = new NativeArray<int>( _totalCells,                  Allocator.Persistent);
        _costField     = new NativeArray<int>( _totalCells,                  Allocator.Persistent);
        _obstacle      = new NativeArray<bool>(_totalCells,                  Allocator.Persistent);
        _cellUnitIndex = new NativeArray<int>( _totalCells * MaxUnitsPerCell, Allocator.Persistent);
        _cellUnitCount = new NativeArray<int>( _totalCells,                  Allocator.Persistent);

        _transformArray = new TransformAccessArray(0);
        AllocUnitArrays(64);

        // 初始化 costField 为不可达
        for (int i = 0; i < _totalCells; i++) _costField[i] = int.MaxValue;
    }

    void OnDestroy()
    {
        if (_transformArray.isCreated) _transformArray.Dispose();
        SafeDispose(ref _unitData);
        SafeDispose(ref _results);
        SafeDispose(ref _unitCellIndex);
        SafeDispose(ref _flowFieldX);
        SafeDispose(ref _flowFieldZ);
        SafeDispose(ref _costField);
        SafeDispose(ref _obstacle);
        SafeDispose(ref _cellUnitIndex);
        SafeDispose(ref _cellUnitCount);
    }

    // ═════════════════════════════════════════════════════════
    // 公共接口：障碍 & 目标
    // ═════════════════════════════════════════════════════════

    /// <summary>设置/清除某世界坐标格子的障碍状态。</summary>
    public void SetObstacle(Vector3 worldPos, bool isObstacle)
    {
        int cx = WorldToCellX(worldPos.x);
        int cz = WorldToCellZ(worldPos.z);
        if (!InBounds(cx, cz)) return;
        _obstacle[Idx(cx, cz)] = isObstacle;
        _flowDirty = true;
    }

    public void SetObstacle(int cx, int cz, bool isObstacle)
    {
        if (!InBounds(cx, cz)) return;
        _obstacle[Idx(cx, cz)] = isObstacle;
        _flowDirty = true;
    }

    public bool IsObstacle(int cx, int cz)  => InBounds(cx, cz) && _obstacle[Idx(cx, cz)];
    public bool IsObstacle(Vector3 worldPos) => IsObstacle(WorldToCellX(worldPos.x), WorldToCellZ(worldPos.z));

    /// <summary>设置寻路目标（世界坐标），触发 BFS + 流场重建。</summary>
    public void SetTarget(Vector3 worldPos)
    {
        int cx = WorldToCellX(worldPos.x);
        int cz = WorldToCellZ(worldPos.z);
        if (!InBounds(cx, cz)) return;
        RebuildFlowField(cx, cz);
    }

    // ═════════════════════════════════════════════════════════
    // 公共接口：查询
    // ═════════════════════════════════════════════════════════
    public int GetCost(Vector3 worldPos)
    {
        int cx = WorldToCellX(worldPos.x);
        int cz = WorldToCellZ(worldPos.z);
        if (!InBounds(cx, cz)) return int.MaxValue;
        return _costField[Idx(cx, cz)];
    }

    public Vector2Int GetDir(Vector3 worldPos)
    {
        int cx = WorldToCellX(worldPos.x);
        int cz = WorldToCellZ(worldPos.z);
        if (!InBounds(cx, cz)) return Vector2Int.zero;
        int i = Idx(cx, cz);
        return new Vector2Int(_flowFieldX[i], _flowFieldZ[i]);
    }

    // ═════════════════════════════════════════════════════════
    // 注册 / 注销
    // ═════════════════════════════════════════════════════════
    public void Register(FlowFieldNavigation nav)
    {
        if (_units.Contains(nav)) return;
        _units.Add(nav);
        _transformArray.Add(nav.transform);
        if (_units.Count > _capacity) AllocUnitArrays(_units.Count * 2);
    }

    public void Unregister(FlowFieldNavigation nav)
    {
        int idx = _units.IndexOf(nav);
        if (idx < 0) return;
        int last = _units.Count - 1;
        _units[idx] = _units[last];
        _units.RemoveAt(last);
        _transformArray.RemoveAtSwapBack(idx);
    }

    // ═════════════════════════════════════════════════════════
    // 主循环
    // ═════════════════════════════════════════════════════════
    void LateUpdate()
    {
        int count = _units.Count;
        if (count == 0) return;

        FillUnitData(count);
        BuildCellUnitMap(count);

        var job = new NavigationJob
        {
            unitData        = _unitData,
            flowFieldX      = _flowFieldX,
            flowFieldZ      = _flowFieldZ,
            obstacle        = _obstacle,
            cellUnitIndex   = _cellUnitIndex,
            cellUnitCount   = _cellUnitCount,
            unitCellIndex   = _unitCellIndex,
            results         = _results,
            maxX            = gridSizeX,
            maxZ            = gridSizeZ,
            cellSizeInt     = Mathf.RoundToInt(cellSize),
            cellSizeF       = cellSize,
            maxUnitsPerCell = MaxUnitsPerCell,
            sepCellRadius   = SepCellRadius,
            separationWeight= SeparationWeight,
            avoidanceWeight = AvoidanceWeight,
            deltaTime       = Time.deltaTime,
        };

        job.Schedule(_transformArray).Complete();

        // Apply 结果到 Transform
        for (int i = 0; i < count; i++)
        {
            Vector3 delta = _results[i];
            if (delta.sqrMagnitude < 0.00001f) continue;

            Vector3 dir = delta.normalized;
            _units[i].transform.position += delta;
            _units[i].transform.rotation  = Quaternion.LookRotation(dir);
            _units[i].lastDirection        = dir;
        }
    }

    // ═════════════════════════════════════════════════════════
    // BFS 流场重建（主线程，仅 SetTarget 时触发）
    // ═════════════════════════════════════════════════════════
    private static readonly Vector2Int[] CardinalDirs = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
    };
    private static readonly Vector2Int[] AllDirs = {
        new Vector2Int(1,0),  new Vector2Int(-1,0),
        new Vector2Int(0,1),  new Vector2Int(0,-1),
        new Vector2Int(1,1),  new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1),
    };

    private void RebuildFlowField(int targetCX, int targetCZ)
    {
        // 重置 costField
        for (int i = 0; i < _totalCells; i++) _costField[i] = int.MaxValue;

        // BFS
        _bfsQueue.Clear();
        int startIdx = Idx(targetCX, targetCZ);
        _costField[startIdx] = 0;
        _bfsQueue.Enqueue(startIdx);

        while (_bfsQueue.Count > 0)
        {
            int cur = _bfsQueue.Dequeue();
            int curCX = cur / gridSizeZ;
            int curCZ = cur % gridSizeZ;
            int curCost = _costField[cur];

            foreach (var dir in CardinalDirs)
            {
                int nx = curCX + dir.x;
                int nz = curCZ + dir.y;
                if (!InBounds(nx, nz)) continue;
                int ni = Idx(nx, nz);
                if (_costField[ni] != int.MaxValue) continue;
                if (_obstacle[ni]) continue;
                _costField[ni] = curCost + 1;
                _bfsQueue.Enqueue(ni);
            }
        }

        // 流场方向：每格选 cost 最低的邻居
        for (int x = 0; x < gridSizeX; x++)
        for (int z = 0; z < gridSizeZ; z++)
        {
            int i = Idx(x, z);
            if (_costField[i] == int.MaxValue)
            {
                _flowFieldX[i] = 0;
                _flowFieldZ[i] = 0;
                continue;
            }

            Vector2Int bestDir  = Vector2Int.zero;
            int        bestCost = _costField[i];

            foreach (var dir in AllDirs)
            {
                int nx = x + dir.x;
                int nz = z + dir.y;
                if (!InBounds(nx, nz)) continue;
                int ni = Idx(nx, nz);
                if (_obstacle[ni]) continue;
                if (_costField[ni] < bestCost)
                {
                    bestCost = _costField[ni];
                    bestDir  = dir;
                }
            }

            _flowFieldX[i] = bestDir.x;
            _flowFieldZ[i] = bestDir.y;
        }
    }

    // ═════════════════════════════════════════════════════════
    // 辅助方法
    // ═════════════════════════════════════════════════════════
    private void FillUnitData(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var u = _units[i];
            _unitData[i] = new UnitData
            {
                position         = u.transform.position,
                moveSpeed        = u.moveSpeed,
                separationRadius = u.separationRadius,
                smoothTime       = u.smoothTime,
                lastDirection    = u.lastDirection,
                isEnabled        = u.isEnabled,
            };
        }
    }

    private void BuildCellUnitMap(int count)
    {
        for (int i = 0; i < _cellUnitCount.Length; i++) _cellUnitCount[i] = 0;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = _unitData[i].position;
            int cx = WorldToCellX(pos.x);
            int cz = WorldToCellZ(pos.z);
            if (!InBounds(cx, cz)) { _unitCellIndex[i] = -1; continue; }

            int cellIdx = Idx(cx, cz);
            _unitCellIndex[i] = cellIdx;
            int slot = _cellUnitCount[cellIdx];
            if (slot < MaxUnitsPerCell)
            {
                _cellUnitIndex[cellIdx * MaxUnitsPerCell + slot] = i;
                _cellUnitCount[cellIdx] = slot + 1;
            }
        }
    }

    private void AllocUnitArrays(int cap)
    {
        SafeDispose(ref _unitData);
        SafeDispose(ref _results);
        SafeDispose(ref _unitCellIndex);
        _capacity      = cap;
        _unitData      = new NativeArray<UnitData>(cap, Allocator.Persistent);
        _results       = new NativeArray<Vector3>( cap, Allocator.Persistent);
        _unitCellIndex = new NativeArray<int>(     cap, Allocator.Persistent);
    }

    // 坐标转换
    public int   WorldToCellX(float wx) => Mathf.FloorToInt(wx / cellSize);
    public int   WorldToCellZ(float wz) => Mathf.FloorToInt(wz / cellSize);
    public bool  InBounds(int cx, int cz) => cx >= 0 && cx < gridSizeX && cz >= 0 && cz < gridSizeZ;
    private int  Idx(int cx, int cz) => cx * gridSizeZ + cz;

    public Vector3 CellToWorld(int cx, int cz)
        => new Vector3(cx * cellSize + cellSize * 0.5f, 0f, cz * cellSize + cellSize * 0.5f);

    private static void SafeDispose<T>(ref NativeArray<T> arr) where T : struct
    {
        if (arr.IsCreated) arr.Dispose();
    }

    // ═════════════════════════════════════════════════════════
    // 数据结构
    // ═════════════════════════════════════════════════════════
    public struct UnitData
    {
        public Vector3 position;
        public Vector3 lastDirection;
        public float   moveSpeed;
        public float   separationRadius;
        public float   smoothTime;
        public bool    isEnabled;
    }

    // ═════════════════════════════════════════════════════════
    // NavigationJob
    // ═════════════════════════════════════════════════════════
    [BurstCompile]
    private struct NavigationJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<UnitData> unitData;
        [ReadOnly] public NativeArray<int>      flowFieldX;
        [ReadOnly] public NativeArray<int>      flowFieldZ;
        [ReadOnly] public NativeArray<bool>     obstacle;
        [ReadOnly] public NativeArray<int>      cellUnitIndex;
        [ReadOnly] public NativeArray<int>      cellUnitCount;
        [ReadOnly] public NativeArray<int>      unitCellIndex;
        [WriteOnly] public NativeArray<Vector3> results;

        public int   maxX;
        public int   maxZ;
        public int   cellSizeInt;
        public float cellSizeF;
        public int   maxUnitsPerCell;
        public int   sepCellRadius;
        public float separationWeight;
        public float avoidanceWeight;
        public float deltaTime;

        public void Execute(int index, TransformAccess transform)
        {
            UnitData ud = unitData[index];
            if (!ud.isEnabled) { results[index] = Vector3.zero; return; }

            // ── 流场方向 ──────────────────────────────────────
            int cx = (int)(ud.position.x / cellSizeF);
            int cz = (int)(ud.position.z / cellSizeF);
            if (cx < 0 || cx >= maxX || cz < 0 || cz >= maxZ)
            { results[index] = Vector3.zero; return; }

            int ffIdx = cx * maxZ + cz;
            int fdx   = flowFieldX[ffIdx];
            int fdz   = flowFieldZ[ffIdx];
            if (fdx == 0 && fdz == 0) { results[index] = Vector3.zero; return; }

            Vector3 flowDir3D = VNormalize(new Vector3(fdx, 0f, fdz));

            // ── 分离力 ────────────────────────────────────────
            Vector3 sep   = CalcSeparation(index, ud.position, ud.separationRadius, cx, cz);

            // ── 避障力 ────────────────────────────────────────
            Vector3 avoid = CalcAvoidance(ud.position, ud.separationRadius * 2f, cx, cz);

            // ── 合并方向 ──────────────────────────────────────
            Vector3 desired = flowDir3D + sep * separationWeight + avoid * avoidanceWeight;
            if (desired.sqrMagnitude < 0.001f) desired = flowDir3D;
            else desired = VNormalize(desired);

            // ── 方向平滑（Lerp 近似）──────────────────────────
            float   t        = Clamp01(deltaTime / ud.smoothTime);
            Vector3 smoothDir = VNormalize(VLerp(ud.lastDirection, desired, t));
            if (smoothDir.sqrMagnitude < 0.001f) smoothDir = desired;

            // ── 速度大小严格等于 moveSpeed ────────────────────
            results[index] = smoothDir * ud.moveSpeed * deltaTime;
        }

        private Vector3 CalcSeparation(int selfIdx, Vector3 selfPos, float radius, int selfCX, int selfCZ)
        {
            Vector3 sep   = Vector3.zero;
            int     count = 0;
            float   rSq   = radius * radius;

            for (int dx = -sepCellRadius; dx <= sepCellRadius; dx++)
            for (int dz = -sepCellRadius; dz <= sepCellRadius; dz++)
            {
                int nx = selfCX + dx;
                int nz = selfCZ + dz;
                if (nx < 0 || nx >= maxX || nz < 0 || nz >= maxZ) continue;

                int cellIdx  = nx * maxZ + nz;
                int cellCnt  = cellUnitCount[cellIdx];
                int baseSlot = cellIdx * maxUnitsPerCell;

                for (int s = 0; s < cellCnt; s++)
                {
                    int other = cellUnitIndex[baseSlot + s];
                    if (other == selfIdx) continue;

                    Vector3 offset = selfPos - unitData[other].position;
                    float   dSq    = offset.x * offset.x + offset.z * offset.z;
                    if (dSq > 0f && dSq < rSq)
                    {
                        sep += offset / Mathf.Sqrt(dSq);
                        count++;
                    }
                }
            }
            return count > 0 ? VNormalize(sep) : Vector3.zero;
        }

        private Vector3 CalcAvoidance(Vector3 selfPos, float lookAhead, int cx, int cz)
        {
            Vector3 avoid = Vector3.zero;
            CheckObs(cx,     cz,     selfPos, lookAhead, ref avoid);
            CheckObs(cx + 1, cz,     selfPos, lookAhead, ref avoid);
            CheckObs(cx - 1, cz,     selfPos, lookAhead, ref avoid);
            CheckObs(cx,     cz + 1, selfPos, lookAhead, ref avoid);
            CheckObs(cx,     cz - 1, selfPos, lookAhead, ref avoid);
            return avoid;
        }

        private void CheckObs(int ox, int oz, Vector3 selfPos, float lookAhead, ref Vector3 avoid)
        {
            if (ox < 0 || ox >= maxX || oz < 0 || oz >= maxZ) return;
            if (!obstacle[ox * maxZ + oz]) return;
            var  center = new Vector3(ox * cellSizeF + cellSizeF * 0.5f, 0f, oz * cellSizeF + cellSizeF * 0.5f);
            Vector3 off = selfPos - center;
            float   d   = Mathf.Sqrt(off.x * off.x + off.z * off.z);
            if (d > 0f && d < lookAhead)
                avoid += VNormalize(off) * (1f - d / lookAhead);
        }

        // ── Burst 友好的数学工具 ──────────────────────────────
        private static Vector3 VNormalize(Vector3 v)
        {
            float m = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 0.0001f ? new Vector3(v.x / m, v.y / m, v.z / m) : Vector3.zero;
        }

        private static Vector3 VLerp(Vector3 a, Vector3 b, float t)
            => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
