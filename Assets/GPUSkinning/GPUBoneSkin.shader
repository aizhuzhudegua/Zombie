Shader "Custom/GPUSkinning/BoneMatrixShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BoneTex ("Bone Matrix Texture", 2D) = "white" {}
        _FrameRate ("Bake Frame Rate", Float) = 30
        
        // 动画控制参数（由C#脚本传入）
        _CurrAnimStartFrame ("Current Anim Start Frame", Float) = 0
        _CurrAnimEndFrame ("Target Anim End Frame", Float) = 0
        _TargetAnimStartFrame ("Target Anim Start Frame", Float) = 0
        _TargetAnimEndFrame ("Target Anim End Frame", Float) = 0
        _Transition ("Transition Value (0-1)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ===================== 顶点输入结构（静态Mesh专用） =====================
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                
                // 静态Mesh必须用UV通道存储骨骼数据（替代BLENDINDICES/BLENDWEIGHTS）
                uint4 blendIndices : BLENDINDICES;
                float4 blendWeights : BLENDWEIGHTS;
            };

            // ===================== 顶点输出结构 =====================
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            // ===================== 全局参数 =====================
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BoneTex;
            float4 _BoneTex_TexelSize; // 自动获取贴图宽高（z=宽，w=高）

            float _FrameRate;
            float _CurrAnimStartFrame;
            float _CurrAnimEndFrame;
            float _TargetAnimStartFrame;
            float _TargetAnimEndFrame;
            float _Transition;

            // ===================== 核心：采样单根骨骼的4x4矩阵 =====================
            // 参数：boneID-骨骼索引  startFrame-当前动画起始帧
            float4x4 SampleBoneMatrix(float time ,int boneID, float startFrame, float endFrame)
            {
                float width  = _BoneTex_TexelSize.z;
                float height = _BoneTex_TexelSize.w;
                
                float currentFrame = startFrame + fmod(time * _FrameRate, endFrame - startFrame);
                
                // 像素中心对齐，避免采样错位
                float uvX = (boneID * 4 + 0.5) / width;
                float uvY = (currentFrame + 0.5) / height;

                // 采样4行像素，拼成完整4x4矩阵
                float4 row0 = tex2Dlod(_BoneTex, float4(uvX,     uvY, 0, 0));
                float4 row1 = tex2Dlod(_BoneTex, float4(uvX+1.0/width, uvY, 0, 0));
                float4 row2 = tex2Dlod(_BoneTex, float4(uvX+2.0/width, uvY, 0, 0));
                float4 row3 = tex2Dlod(_BoneTex, float4(uvX+3.0/width, uvY, 0, 0));

                return float4x4(row0, row1, row2, row3);
            }

            // ===================== 顶点着色器：蒙皮计算+动画过渡 =====================
            v2f vert (appdata v)
            {
                v2f o;
                
                // 读取骨骼索引、权重
                int4 indices = int4(v.blendIndices);
                float4 weights = v.blendWeights;
               
                float playTime = _Time.w;
                float height = _BoneTex_TexelSize.w;

                // 1. 采样【当前动画】4根骨骼矩阵
                float4x4 matCurr0 = SampleBoneMatrix(playTime, indices.x, _CurrAnimStartFrame, _CurrAnimEndFrame);
                float4x4 matCurr1 = SampleBoneMatrix(playTime, indices.y, _CurrAnimStartFrame, _CurrAnimEndFrame);
                float4x4 matCurr2 = SampleBoneMatrix(playTime, indices.z, _CurrAnimStartFrame, _CurrAnimEndFrame);
                float4x4 matCurr3 = SampleBoneMatrix(playTime, indices.w, _CurrAnimStartFrame, _CurrAnimEndFrame);

              
                // 2. 采样【目标动画】4根骨骼矩阵
                float4x4 matTarget0 = SampleBoneMatrix(0, indices.x, _TargetAnimStartFrame, _TargetAnimEndFrame);
                float4x4 matTarget1 = SampleBoneMatrix(0, indices.y, _TargetAnimStartFrame, _TargetAnimEndFrame);
                float4x4 matTarget2 = SampleBoneMatrix(0, indices.z, _TargetAnimStartFrame, _TargetAnimEndFrame);
                float4x4 matTarget3 = SampleBoneMatrix(0, indices.w, _TargetAnimStartFrame, _TargetAnimEndFrame);

                // 3. 矩阵线性插值，实现平滑过渡（核心！）
                float4x4 mat0 = lerp(matCurr0, matTarget0, _Transition);
                float4x4 mat1 = lerp(matCurr1, matTarget1, _Transition);
                float4x4 mat2 = lerp(matCurr2, matTarget2, _Transition);
                float4x4 mat3 = lerp(matCurr3, matTarget3, _Transition);

                // 4. 标准蒙皮计算：权重混合矩阵 × 模型空间顶点
                float4 skinnedVertex = 
                    mul(mat0, v.vertex) * weights.x +
                    mul(mat1, v.vertex) * weights.y +
                    mul(mat2, v.vertex) * weights.z +
                    mul(mat3, v.vertex) * weights.w;

                // 模型空间 → 裁剪空间
                o.pos = UnityObjectToClipPos(skinnedVertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            // ===================== 片段着色器 =====================
            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShaderGUI"
}