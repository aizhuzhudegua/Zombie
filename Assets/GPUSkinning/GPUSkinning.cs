using UnityEngine;

public class GPUSkinning : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Material gpuSkinningMaterial;

    private Matrix4x4[] boneMatrices = new Matrix4x4[100]; // 假设最多支持 100 个骨骼

    void Update()
    {
        if (skinnedMeshRenderer == null || gpuSkinningMaterial == null)
            return;

        // 获取骨骼的变换矩阵
        var bones = skinnedMeshRenderer.bones;
        for (int i = 0; i < bones.Length; i++)
        {
            boneMatrices[i] = bones[i].localToWorldMatrix * skinnedMeshRenderer.sharedMesh.bindposes[i];
        }

        // 将骨骼矩阵传递给 Shader
        gpuSkinningMaterial.SetMatrixArray("_BoneMatrices", boneMatrices);
    }
}