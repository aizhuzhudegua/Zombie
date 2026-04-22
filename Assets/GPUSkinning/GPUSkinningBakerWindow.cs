#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;

public class GPUSkinningBakerWindow : OdinEditorWindow
{
   
    [MenuItem("GPUSKinning/BakerWindow")]
    public static void Open()
    {
        GetWindow<GPUSkinningBakerWindow>();
    }

    [LabelText("游戏物体")] public GameObject gameObject;
    [LabelText("动画片段")] public System.Collections.Generic.List<AnimationClip> animationClips;
    [LabelText("导出路径"),FolderPath] public string path;
    [LabelText("采样频率")] public int frameRate;
    [LabelText("动画配置")] public AnimationMeshSkinningConfig animationConfig;
    [LabelText("部件")] public System.Collections.Generic.List<Transform> parts;

    [Button()]
    public void Bake()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(DoBake());
    }

    private System.Collections.IEnumerator DoBake()
    {
        Debug.Log("开始烘焙动画");
        
        SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        Animator animator = gameObject.GetComponentInChildren<Animator>();
        Mesh mesh = new Mesh();

        
        int verCount = skinnedMeshRenderer.sharedMesh.vertexCount;
        // 确定贴图的总尺寸 
        int totalFrames = 0;
        for (int i = 0; i < animationClips.Count; i++)
        {
            totalFrames += (int)(animationClips[i].length * frameRate); // 总帧数
        }
        Texture2D texture = new Texture2D(verCount, totalFrames, TextureFormat.RGBAHalf, false);

        animationConfig.clips.Clear();
        // 挨个采样动画
        int offsetY = 0;
        for (int index = 0; index < animationClips.Count; index++)
        {
            AnimationClip animationClip = animationClips[index];
            int clipFrames = (int)(animationClip.length * frameRate); // 总帧数
            PlayableGraph graph = PlayableGraph.Create();
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, animationClip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "GPUSkinning", animator);
            output.SetSourcePlayable(clipPlayable, 0);
            graph.Play();
            clipPlayable.Pause();

            // 动画配置
            animationConfig.clips.Add(animationClip.name, new AnimationMeshSkinningClipInfo()
            {
                startTime = offsetY * (1f / frameRate),
                length = animationClip.length,
                isLoop = animationClip.isLooping
            });

            // 部件动画
            List<PartAnimationInfo> partAnimationInfos = new List<PartAnimationInfo>(parts.Count);
            for (int p = 0; p < parts.Count; p++)
            {
                PartAnimationInfo partAnimationInfo = new PartAnimationInfo();
                partAnimationInfo.Init(clipFrames,parts[p].gameObject.name);
                partAnimationInfos.Add(partAnimationInfo);
            }


            // 采样
            float perTime = animationClip.length / clipFrames; // 获取帧间隔/秒
            for (int i = 0; i < clipFrames; i++)
            {
                clipPlayable.SetTime(perTime * i);
                yield return null;
                skinnedMeshRenderer.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                for (int v = 0; v < verCount; v++)
                {
                    // 顶点的位置保存到texture中
                    Vector3 vertex = vertices[v];
                    texture.SetPixel(v, offsetY, new Color(vertex.x, vertex.y, vertex.z));
                }

                // 最后补一帧
                int times = i == clipFrames - 1 ? 2 : 1;
                for (int l = 0; l < times; l++)
                {
                    float partTime = perTime * (i + 1);
                    for (int p = 0; p < partAnimationInfos.Count; p++)
                    {
                        // 相对于animator所在的物体的Transform
                        Vector3 pos = animator.gameObject.transform.InverseTransformPoint(parts[p].position);
                        Quaternion rot = Quaternion.Euler(animator.gameObject.transform.InverseTransformDirection(parts[p].eulerAngles));
                        partAnimationInfos[p].Set(i+l , partTime, pos, rot, parts[p].lossyScale);
                    }
                }

                
                offsetY++;
            }

            AnimationClip partClip = new AnimationClip();
            partClip.frameRate = frameRate;
            partClip.legacy = false;
            partClip.wrapMode = animationClip.isLooping ? WrapMode.Loop : WrapMode.Default;
            for (int p = 0; p < partAnimationInfos.Count; p++)
            {
                partAnimationInfos[p].Bind(partClip);
            }
            AssetDatabase.CreateAsset(partClip, $"{path}/{animationClip.name}.anim");

            graph.Stop();
            graph.Destroy();
           
        }

       
        

        // 保存贴图
        texture.Apply();
        EditorUtility.SetDirty(animationConfig);
        AssetDatabase.SaveAssetIfDirty(animationConfig);
        AssetDatabase.CreateAsset(texture,$"{path}/{gameObject.name}.asset");
        AssetDatabase.Refresh();

        Debug.Log("烘焙完成");
        yield return null;
    }


    private class PartAnimationInfo
    {
        Keyframe[] posXKeys;
        Keyframe[] posYKeys;
        Keyframe[] posZKeys;
        Keyframe[] rotXKeys;
        Keyframe[] rotYKeys;
        Keyframe[] rotZKeys;
        Keyframe[] rotWKeys;
        Keyframe[] scaXKeys;
        Keyframe[] scaYKeys;
        Keyframe[] scaZKeys;
        string name;

        public void Init(int clipFrameCount, string name)
        {
            this.name = name;
            posXKeys = new Keyframe[clipFrameCount + 1];
            posYKeys = new Keyframe[clipFrameCount + 1];
            posZKeys = new Keyframe[clipFrameCount + 1];
            rotXKeys = new Keyframe[clipFrameCount + 1];
            rotYKeys = new Keyframe[clipFrameCount + 1];
            rotZKeys = new Keyframe[clipFrameCount + 1];
            rotWKeys = new Keyframe[clipFrameCount + 1];
            scaXKeys = new Keyframe[clipFrameCount + 1];
            scaYKeys = new Keyframe[clipFrameCount + 1];
            scaZKeys = new Keyframe[clipFrameCount + 1];
        }    
        
        public void Set(int index, float time , Vector3 pos, Quaternion rot, Vector3 scale)
        {
            posXKeys[index].time = time;
            posYKeys[index].time = time;
            posZKeys[index].time = time;
            posXKeys[index].value = pos.x;
            posYKeys[index].value = pos.y;
            posZKeys[index].value = pos.z;

            rotXKeys[index].time = time;
            rotYKeys[index].time = time;
            rotZKeys[index].time = time;
            rotWKeys[index].time = time;
            rotXKeys[index].value = rot.x;
            rotYKeys[index].value = rot.y;
            rotZKeys[index].value = rot.z;
            rotWKeys[index].value = rot.w;

            scaXKeys[index].time = time;
            scaYKeys[index].time = time;
            scaZKeys[index].time = time;
            scaXKeys[index].value = scale.x;
            scaYKeys[index].value = scale.y;
            scaZKeys[index].value = scale.z;
        }  
        
        public void Bind(AnimationClip clip)
        {
            AnimationCurve posXCurve = new AnimationCurve(posXKeys);
            AnimationCurve posYCurve = new AnimationCurve(posYKeys);
            AnimationCurve posZCurve = new AnimationCurve(posZKeys);
            AnimationCurve rotXCurve = new AnimationCurve(rotXKeys);
            AnimationCurve rotYCurve = new AnimationCurve(rotYKeys);
            AnimationCurve rotZCurve = new AnimationCurve(rotZKeys);
            AnimationCurve rotWCurve = new AnimationCurve(rotWKeys);
            AnimationCurve scaleXCurve = new AnimationCurve(scaXKeys);
            AnimationCurve scaleYCurve = new AnimationCurve(scaYKeys);
            AnimationCurve scaleZCurve = new AnimationCurve(scaZKeys);

            EditorCurveBinding posXBind = new EditorCurveBinding();
            posXBind.path = name;
            posXBind.propertyName = "m_LocalPosition.x";
            posXBind.type = typeof(Transform);

            EditorCurveBinding posYBind = new EditorCurveBinding();
            posYBind.path = name;
            posYBind.propertyName = "m_LocalPosition.y";
            posYBind.type = typeof(Transform);

            EditorCurveBinding posZBind = new EditorCurveBinding();
            posZBind.path = name;
            posZBind.propertyName = "m_LocalPosition.z";
            posZBind.type = typeof(Transform);

            EditorCurveBinding rotXBind = new EditorCurveBinding();
            rotXBind.path = name;
            rotXBind.propertyName = "m_LocalRotation.x";
            rotXBind.type = typeof(Transform);

            EditorCurveBinding rotYBind = new EditorCurveBinding();
            rotYBind.path = name;
            rotYBind.propertyName = "m_LocalRotation.y";
            rotYBind.type = typeof(Transform);

            EditorCurveBinding rotZBind = new EditorCurveBinding();
            rotZBind.path = name;
            rotZBind.propertyName = "m_LocalRotation.z";
            rotZBind.type = typeof(Transform);

            EditorCurveBinding rotWBind = new EditorCurveBinding();
            rotWBind.path = name;
            rotWBind.propertyName = "m_LocalRotation.w";
            rotWBind.type = typeof(Transform);

            EditorCurveBinding scaleXBind = new EditorCurveBinding();
            scaleXBind.path = name;
            scaleXBind.propertyName = "m_LocalScale.x";
            scaleXBind.type = typeof(Transform);

            EditorCurveBinding scaleYBind = new EditorCurveBinding();
            scaleYBind.path = name;
            scaleYBind.propertyName = "m_LocalScale.y";
            scaleYBind.type = typeof(Transform);

            EditorCurveBinding scaleZBind = new EditorCurveBinding();
            scaleZBind.path = name;
            scaleZBind.propertyName = "m_LocalScale.z";
            scaleZBind.type = typeof(Transform);


            AnimationUtility.SetEditorCurve(clip, posXBind, posXCurve);
            AnimationUtility.SetEditorCurve(clip, posYBind, posYCurve);
            AnimationUtility.SetEditorCurve(clip, posZBind, posZCurve);
            AnimationUtility.SetEditorCurve(clip, rotXBind, rotXCurve);
            AnimationUtility.SetEditorCurve(clip, rotYBind, rotYCurve);
            AnimationUtility.SetEditorCurve(clip, rotZBind, rotZCurve);
            AnimationUtility.SetEditorCurve(clip, rotWBind, rotWCurve);
            AnimationUtility.SetEditorCurve(clip, scaleXBind, scaleXCurve);
            AnimationUtility.SetEditorCurve(clip, scaleYBind, scaleYCurve);
            AnimationUtility.SetEditorCurve(clip, scaleZBind, scaleZCurve);
        }
    }

}
#endif