using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ºæ±º¹Ç÷À
[CreateAssetMenu(menuName ="Config/AnimationConfig")]
public class AnimationConfig : SerializedScriptableObject
{
    public Dictionary<string, AnimationClipInfo> clips = new Dictionary<string, AnimationClipInfo>();
}

public class AnimationClipInfo
{
    public float start;
    public float end;
    public bool isLoop;
}


// ºæ±º¶¥µã
[CreateAssetMenu(menuName = "Config/AnimationConfigMeshSkinning")]
public class AnimationMeshSkinningConfig : SerializedScriptableObject
{
    public Dictionary<string, AnimationMeshSkinningClipInfo> clips = new Dictionary<string, AnimationMeshSkinningClipInfo>();
}

public class AnimationMeshSkinningClipInfo
{
    public float startTime;
    public float length;
    public bool isLoop;
}