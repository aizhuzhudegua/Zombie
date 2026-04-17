#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(VoxelGridGenerator))]
public class VoxelGridEditorWindow : Editor
{
    public override void OnInspectorGUI()
    {
        VoxelGridGenerator grid = (VoxelGridGenerator)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("编辑器工具", EditorStyles.boldLabel);
        
        int totalCells = grid.gridSize.x * grid.gridSize.y * grid.gridSize.z;
        float memMB = (totalCells * 24f) / (1024f * 1024f);
        
        EditorGUILayout.HelpBox(
            $"网格数量: {totalCells:N0}\n" +
            $"预估内存: ~{memMB:F1}MB",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("生成网格数据", GUILayout.Height(30)))
        {
            grid.GenerateGrid();
            EditorUtility.SetDirty(grid);
        }
        
        if (GUILayout.Button("检测碰撞体并保存JSON", GUILayout.Height(30)))
        {
            DetectAndSave(grid);
        }
        
        EditorGUILayout.EndVertical();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(grid);
        }
    }
    
    void DetectAndSave(VoxelGridGenerator grid)
    {
        if (grid.cells == null)
        {
            EditorUtility.DisplayDialog("错误", "请先生成网格数据！", "OK");
            return;
        }
        
        string savePath = Application.dataPath + "/Resources/VoxelData";
        
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        
        string fullPath = savePath + "/voxel_grid.json";
        
        VoxelGridJsonData data = new VoxelGridJsonData
        {
            gridSize = grid.gridSize,
            cellSize = grid.cellSize,
            gridPosition = grid.transform.position,
            cells = new List<VoxelCellJsonData>()
        };
        
        Collider[] hits = new Collider[100];
        
        for (int x = 0; x < grid.gridSize.x; x++)
        {
            for (int y = 0; y < grid.gridSize.y; y++)
            {
                for (int z = 0; z < grid.gridSize.z; z++)
                {
                    VoxelCell cell = grid.cells[x, y, z];
                    float halfSize = grid.cellSize * 0.5f;
                    Vector3 center = cell.center;
                    
                    VoxelCellJsonData cellData = new VoxelCellJsonData
                    {
                        x = x,
                        y = y,
                        z = z,
                        cx = cell.center.x,
                        cy = cell.center.y,
                        cz = cell.center.z,
                        colliders = new List<ColliderJsonData>()
                    };
                    
                    Bounds bounds = new Bounds(center, Vector3.one * halfSize * 0.95f);
                    
                    int count = Physics.OverlapBoxNonAlloc(center, Vector3.one * halfSize * 0.5f, hits, Quaternion.identity);
                    
                    for (int i = 0; i < count; i++)
                    {
                        Collider col = hits[i];
                        cellData.colliders.Add(new ColliderJsonData
                        {
                            name = col.name,
                            tag = col.tag,
                            layer = LayerMask.LayerToName(col.gameObject.layer)
                        });
                    }
                    
                    data.cells.Add(cellData);
                }
            }
        }
        
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(fullPath, json);
        
        AssetDatabase.Refresh();
        
        int occupiedCount = 0;
        int totalColliders = 0;
        foreach (var cell in data.cells)
        {
            if (cell.colliders.Count > 0)
            {
                occupiedCount++;
                totalColliders += cell.colliders.Count;
            }
        }
        
        EditorUtility.DisplayDialog("成功", 
            $"已保存到: {fullPath}\n" +
            $"占用格子: {occupiedCount}/{data.cells.Count}\n" +
            $"碰撞体总数: {totalColliders}", "OK");
        
        Debug.Log($"Voxel数据已保存: 占用{occupiedCount}格, 共{totalColliders}个碰撞体");
    }
    
    [System.Serializable]
    class VoxelGridJsonData
    {
        public Vector3Int gridSize;
        public float cellSize;
        public Vector3 gridPosition;
        public List<VoxelCellJsonData> cells;
    }
    
    [System.Serializable]
    class VoxelCellJsonData
    {
        public int x, y, z;
        public float cx, cy, cz;
        public List<ColliderJsonData> colliders;
    }
    
    [System.Serializable]
    class ColliderJsonData
    {
        public string name;
        public string tag;
        public string layer;
    }
    
    [MenuItem("Tools/创建体素网格")]
    static void CreateVoxelGrid()
    {
        GameObject go = new GameObject("VoxelGrid");
        go.AddComponent<VoxelGridGenerator>();
        Selection.activeGameObject = go;
    }
}
#endif
