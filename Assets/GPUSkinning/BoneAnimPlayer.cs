using UnityEngine;

public class BoneAnimPlayer : MonoBehaviour
{
    [Header("烘焙数据")]
    public Texture2D boneTex;
    public int boneCount;
    public float frameRate = 30;

    private Material mat;
    private float currentFrame;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        mat.SetTexture("_BoneTex", boneTex);
        mat.SetFloat("_BoneCount", boneCount);
    }

    void Update()
    {
        // 播放动画（仅需更新当前帧）
        currentFrame += Time.deltaTime * frameRate;
        mat.SetFloat("_CurrentFrame", currentFrame);
    }
}