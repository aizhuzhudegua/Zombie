#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;
using System.Collections;

public class GPUSkinningTransitionWindow : OdinEditorWindow
{
    [MenuItem("GPUSKinning/骨骼矩阵烘焙器")]
    public static void Open()
    {
        GetWindow<GPUSkinningTransitionWindow>("骨骼矩阵烘焙");
    }

    [LabelText("蒙皮游戏物体")] public GameObject gameObject;
    [LabelText("动画数组")] public List<AnimationClip> animationClips;
    [LabelText("导出路径"), FolderPath] public string path;
    [LabelText("采样帧率")] public int frameRate = 30;
    [LabelText("动画配置")] public AnimationConfig animationConfig;

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Animator animator;
    private Transform[] bones;
    private int boneCount;

    [Button("开始烘焙", ButtonSizes.Large)]
    public void Bake()
    {
        if (!Check()) return;

        skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        animator = gameObject.GetComponentInChildren<Animator>();
        bones = skinnedMeshRenderer.bones;
        boneCount = bones.Length;
        animationConfig.clips.Clear();

        EditorCoroutineUtility.StartCoroutineOwnerless(BakeBoneMatrixTexture());
    }

    private bool Check()
    {
        if (gameObject == null || animationClips.Count < 1 || !path.StartsWith("Assets") || animationConfig == null)
        {
            EditorUtility.DisplayDialog("错误", "参数不全", "OK");
            return false;
        }
        return true;
    }

    // 核心：只烘焙动画本体的骨骼矩阵（无过渡）
    private IEnumerator BakeBoneMatrixTexture()
    {
        int totalFrames = 0;
        // 只计算动画本体帧数（删除过渡计算）
        foreach (var c in animationClips)
            totalFrames += Mathf.CeilToInt(c.length * frameRate);

        // 纹理：宽度=骨骼数*4 | 高度=总帧数
        Texture2D tex = new Texture2D(boneCount * 4, totalFrames, TextureFormat.RGBAHalf, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        int currentY = 0;

        foreach (var clip in animationClips)
        {
            int frameCount = Mathf.CeilToInt(clip.length * frameRate);
            var graph = PlayableGraph.Create();
            var playable = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput.Create(graph, "", animator).SetSourcePlayable(playable);
            graph.Play();

            int startY = currentY;
            for (int i = 0; i < frameCount; i++)
            {
                playable.SetTime(i * clip.length / frameCount);
                yield return null;
                WriteBones(tex, currentY++);
            }

            // 保存动画配置
            animationConfig.clips.Add(clip.name, new AnimationClipInfo()
            {
                start = startY,
                end = currentY-1,
                isLoop = clip.isLooping
            });
            graph.Destroy();
        }

        // 保存纹理
        tex.Apply();
        AssetDatabase.CreateAsset(tex, $"{path}/{gameObject.name}_BoneTex.asset");
        EditorUtility.SetDirty(animationConfig);
        AssetDatabase.SaveAssets();
        Debug.Log("骨骼矩阵烘焙完成！仅烘焙动画本体");
        yield return null;
    }

    private void WriteBones(Texture2D tex, int y)
    {
        var bindposes = skinnedMeshRenderer.sharedMesh.bindposes;
        Matrix4x4 rootMatrix = skinnedMeshRenderer.transform.worldToLocalMatrix;

        for (int i = 0; i < boneCount; i++)
        {
            // 正确公式
            Matrix4x4 boneWorld = bones[i].localToWorldMatrix;
            Matrix4x4 bindPose = bindposes[i];

            Matrix4x4 m = rootMatrix * boneWorld * bindPose;

            // 写入纹理（顺序正确）
            tex.SetPixel(i * 4 + 0, y, new Color(m.m00, m.m01, m.m02, m.m03));
            tex.SetPixel(i * 4 + 1, y, new Color(m.m10, m.m11, m.m12, m.m13));
            tex.SetPixel(i * 4 + 2, y, new Color(m.m20, m.m21, m.m22, m.m23));
            tex.SetPixel(i * 4 + 3, y, new Color(m.m30, m.m31, m.m32, m.m33));
        }
    }
}
#endif