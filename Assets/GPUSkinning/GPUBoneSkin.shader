Shader "Custom/GPUSkinning/BoneMatrixShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BoneTex ("Bone Matrix Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        [NoScaleOffset] _MetallicGlossMap ("Metallic", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // GPU Instancing 所需的 setup 函数
            void setup() {}

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float4 tangentOS : TANGENT;
                uint4 blendIndices : BLENDINDICES;
                float4 blendWeights : BLENDWEIGHTS;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 positionCS : SV_POSITION;
                float fogFactor : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BoneTex);
            SAMPLER(sampler_BoneTex);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);
            float4 _BoneTex_TexelSize;
            float4 _MainTex_ST;
            float4 _BaseColor;
            float _Cutoff;
            float _Smoothness;
            float _Metallic;
            float4 _EmissionColor;

            #ifdef UNITY_INSTANCING_BUFFER_START
                UNITY_INSTANCING_BUFFER_START(AnimProps)
                    UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimStartFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimEndFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimStartFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimEndFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Transition)
                    UNITY_DEFINE_INSTANCED_PROP(int, _isTransition)
                    UNITY_DEFINE_INSTANCED_PROP(float, _AnimProgress)
                UNITY_INSTANCING_BUFFER_END(AnimProps)
            #endif

            float4x4 SampleBoneMatrix(float progress, int boneID, float startFrame, float endFrame)
            {
                float2 boneTexSize;
            #ifdef UNITY_INSTANCING_BUFFER_START
                boneTexSize = _BoneTex_TexelSize.zw;
            #else
                boneTexSize = float2(1, 1);
            #endif

                float frameLength = endFrame - startFrame;
                float currentFrame = startFrame + frameLength * progress;

                float uvX = (boneID * 4 + 0.5) / boneTexSize.x;
                float uvY = (currentFrame + 0.5) / boneTexSize.y;

                float4 row0 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX, uvY), 0);
                float4 row1 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 1.0 / boneTexSize.x, uvY), 0);
                float4 row2 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 2.0 / boneTexSize.x, uvY), 0);
                float4 row3 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 3.0 / boneTexSize.x, uvY), 0);

                return float4x4(row0, row1, row2, row3);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);

                float currStart = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimStartFrame);
                float currEnd   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimEndFrame);
                float targetStart = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimStartFrame);
                float targetEnd   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimEndFrame);
                float transition = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _Transition);
                int isTransition = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _isTransition);
                float progress = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _AnimProgress);

                int4 indices = int4(input.blendIndices);
                float4 weights = input.blendWeights;

                float4x4 boneMat0, boneMat1, boneMat2, boneMat3;

                if (isTransition == 1)
                {
                    boneMat0 = lerp(
                        SampleBoneMatrix(progress, indices.x, currStart, currEnd),
                        SampleBoneMatrix(0, indices.x, targetStart, targetEnd),
                        transition);
                    boneMat1 = lerp(
                        SampleBoneMatrix(progress, indices.y, currStart, currEnd),
                        SampleBoneMatrix(0, indices.y, targetStart, targetEnd),
                        transition);
                    boneMat2 = lerp(
                        SampleBoneMatrix(progress, indices.z, currStart, currEnd),
                        SampleBoneMatrix(0, indices.z, targetStart, targetEnd),
                        transition);
                    boneMat3 = lerp(
                        SampleBoneMatrix(progress, indices.w, currStart, currEnd),
                        SampleBoneMatrix(0, indices.w, targetStart, targetEnd),
                        transition);
                }
                else
                {
                    boneMat0 = SampleBoneMatrix(progress, indices.x, currStart, currEnd);
                    boneMat1 = SampleBoneMatrix(progress, indices.y, currStart, currEnd);
                    boneMat2 = SampleBoneMatrix(progress, indices.z, currStart, currEnd);
                    boneMat3 = SampleBoneMatrix(progress, indices.w, currStart, currEnd);
                }

                float4 skinnedPos = mul(boneMat0, float4(input.positionOS, 1)) * weights.x
                              + mul(boneMat1, float4(input.positionOS, 1)) * weights.y
                              + mul(boneMat2, float4(input.positionOS, 1)) * weights.z
                              + mul(boneMat3, float4(input.positionOS, 1)) * weights.w;

                float3 skinnedNormal = normalize(mul((float3x3)boneMat0, input.normalOS) * weights.x
                                              + mul((float3x3)boneMat1, input.normalOS) * weights.y
                                              + mul((float3x3)boneMat2, input.normalOS) * weights.z
                                              + mul((float3x3)boneMat3, input.normalOS) * weights.w);

                float3 skinnedTangent = normalize(mul((float3x3)boneMat0, input.tangentOS.xyz) * weights.x
                                               + mul((float3x3)boneMat1, input.tangentOS.xyz) * weights.y
                                               + mul((float3x3)boneMat2, input.tangentOS.xyz) * weights.z
                                               + mul((float3x3)boneMat3, input.tangentOS.xyz) * weights.w);

                VertexPositionInputs posInput;
                posInput.positionWS = TransformObjectToWorld(skinnedPos.xyz);
                posInput.positionCS = TransformWorldToHClip(posInput.positionWS);
                output.positionCS = posInput.positionCS;
                output.positionWS = posInput.positionWS ;

                output.normalWS = TransformObjectToWorldNormal(skinnedNormal);
                output.tangentWS = float4(TransformObjectToWorldDir(skinnedTangent), input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                output.fogFactor = ComputeFogFactor(posInput.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Albedo + Alpha
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(albedoAlpha.a - _Cutoff);
                #endif

                // Metallic + Smoothness（从贴图采样，与 _Metallic/_Smoothness 相乘）
                half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);

                // 构建 SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = albedoAlpha.rgb;
                surfaceData.alpha       = albedoAlpha.a;
                surfaceData.metallic    = metallicGloss.r * _Metallic;
                surfaceData.smoothness  = metallicGloss.a * _Smoothness;
                surfaceData.normalTS    = half3(0, 0, 1);
                surfaceData.emission    = _EmissionColor.rgb;
                surfaceData.occlusion   = 1.0;
                surfaceData.specular    = half3(0, 0, 0);

                // 构建 InputData
                InputData inputData = (InputData)0;
                inputData.positionWS              = input.positionWS;
                inputData.normalWS                = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord             = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord                = input.fogFactor;
                inputData.vertexLighting          = half3(0, 0, 0);
                inputData.bakedGI                 = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask              = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                // URP 标准 PBR 光照
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // 雾效
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vertDepth
            #pragma fragment DepthOnlyPass // 自定义片段着色器
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // GPU Instancing 所需的 setup 函数
            void setup() {}

            struct Attributes
            {
                float3 positionOS : POSITION;
                uint4 blendIndices : BLENDINDICES;
                float4 blendWeights : BLENDWEIGHTS;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_BoneTex);
            SAMPLER(sampler_BoneTex);
            float4 _BoneTex_TexelSize;

            #ifdef UNITY_INSTANCING_BUFFER_START
                UNITY_INSTANCING_BUFFER_START(AnimProps)
                    UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimStartFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimEndFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimStartFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimEndFrame)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Transition)
                    UNITY_DEFINE_INSTANCED_PROP(int, _isTransition)
                    UNITY_DEFINE_INSTANCED_PROP(float, _AnimProgress)
                UNITY_INSTANCING_BUFFER_END(AnimProps)
            #endif

            float4x4 SampleBoneMatrix(float progress, int boneID, float startFrame, float endFrame)
            {
                float2 boneTexSize = _BoneTex_TexelSize.zw;

                float frameLength = endFrame - startFrame;
                float currentFrame = startFrame + frameLength * progress;

                float uvX = (boneID * 4 + 0.5) / boneTexSize.x;
                float uvY = (currentFrame + 0.5) / boneTexSize.y;

                float4 row0 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX, uvY), 0);
                float4 row1 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 1.0 / boneTexSize.x, uvY), 0);
                float4 row2 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 2.0 / boneTexSize.x, uvY), 0);
                float4 row3 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 3.0 / boneTexSize.x, uvY), 0);

                return float4x4(row0, row1, row2, row3);
            }

            Varyings vertDepth(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);

                float currStart = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimStartFrame);
                float currEnd   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimEndFrame);
                float targetStart = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimStartFrame);
                float targetEnd   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimEndFrame);
                float transition = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _Transition);
                int isTransition = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _isTransition);
                float progress = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _AnimProgress);

                int4 indices = int4(input.blendIndices);
                float4 weights = input.blendWeights;

                float4x4 boneMat0, boneMat1, boneMat2, boneMat3;

                if (isTransition == 1)
                {
                    boneMat0 = lerp(
                        SampleBoneMatrix(progress, indices.x, currStart, currEnd),
                        SampleBoneMatrix(0, indices.x, targetStart, targetEnd),
                        transition);
                    boneMat1 = lerp(
                        SampleBoneMatrix(progress, indices.y, currStart, currEnd),
                        SampleBoneMatrix(0, indices.y, targetStart, targetEnd),
                        transition);
                    boneMat2 = lerp(
                        SampleBoneMatrix(progress, indices.z, currStart, currEnd),
                        SampleBoneMatrix(0, indices.z, targetStart, targetEnd),
                        transition);
                    boneMat3 = lerp(
                        SampleBoneMatrix(progress, indices.w, currStart, currEnd),
                        SampleBoneMatrix(0, indices.w, targetStart, targetEnd),
                        transition);
                }
                else
                {
                    boneMat0 = SampleBoneMatrix(progress, indices.x, currStart, currEnd);
                    boneMat1 = SampleBoneMatrix(progress, indices.y, currStart, currEnd);
                    boneMat2 = SampleBoneMatrix(progress, indices.z, currStart, currEnd);
                    boneMat3 = SampleBoneMatrix(progress, indices.w, currStart, currEnd);
                }

                float4 skinnedPos = mul(boneMat0, float4(input.positionOS, 1)) * weights.x
                              + mul(boneMat1, float4(input.positionOS, 1)) * weights.y
                              + mul(boneMat2, float4(input.positionOS, 1)) * weights.z
                              + mul(boneMat3, float4(input.positionOS, 1)) * weights.w;

                output.positionCS = TransformObjectToHClip(skinnedPos.xyz) ;
                return output ;
            }

            // 自定义极简深度片段着色器，替代官方头文件
            half DepthOnlyPass(Varyings input) : SV_TARGET
            {
                return input.positionCS.z;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef LerpWhiteTo
            #define LerpWhiteTo(b, t) (1.0 - t + b * t)
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            void setup() {}

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint4 blendIndices : BLENDINDICES;
                float4 blendWeights : BLENDWEIGHTS;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_BoneTex);
            SAMPLER(sampler_BoneTex);
            float4 _BoneTex_TexelSize;

            UNITY_INSTANCING_BUFFER_START(AnimProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimStartFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrAnimEndFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimStartFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _TargetAnimEndFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _Transition)
                UNITY_DEFINE_INSTANCED_PROP(int, _isTransition)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimProgress)
            UNITY_INSTANCING_BUFFER_END(AnimProps)

            float4x4 SampleBoneMatrix(float progress, int boneID, float startFrame, float endFrame)
            {
                float2 boneTexSize = _BoneTex_TexelSize.zw;
                float frameLength = endFrame - startFrame;
                float currentFrame = startFrame + frameLength * progress;
                float uvX = (boneID * 4 + 0.5) / boneTexSize.x;
                float uvY = (currentFrame + 0.5) / boneTexSize.y;
                float4 row0 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX, uvY), 0);
                float4 row1 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 1.0 / boneTexSize.x, uvY), 0);
                float4 row2 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 2.0 / boneTexSize.x, uvY), 0);
                float4 row3 = SAMPLE_TEXTURE2D_LOD(_BoneTex, sampler_BoneTex, float2(uvX + 3.0 / boneTexSize.x, uvY), 0);
                return float4x4(row0, row1, row2, row3);
            }

            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float currStart   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimStartFrame);
                float currEnd     = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _CurrAnimEndFrame);
                float targetStart = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimStartFrame);
                float targetEnd   = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _TargetAnimEndFrame);
                float transition  = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _Transition);
                int isTransition  = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _isTransition);
                float progress    = UNITY_ACCESS_INSTANCED_PROP(AnimProps, _AnimProgress);

                int4 indices  = int4(input.blendIndices);
                float4 weights = input.blendWeights;

                float4x4 boneMat0, boneMat1, boneMat2, boneMat3;
                if (isTransition == 1)
                {
                    boneMat0 = lerp(SampleBoneMatrix(progress, indices.x, currStart, currEnd), SampleBoneMatrix(0, indices.x, targetStart, targetEnd), transition);
                    boneMat1 = lerp(SampleBoneMatrix(progress, indices.y, currStart, currEnd), SampleBoneMatrix(0, indices.y, targetStart, targetEnd), transition);
                    boneMat2 = lerp(SampleBoneMatrix(progress, indices.z, currStart, currEnd), SampleBoneMatrix(0, indices.z, targetStart, targetEnd), transition);
                    boneMat3 = lerp(SampleBoneMatrix(progress, indices.w, currStart, currEnd), SampleBoneMatrix(0, indices.w, targetStart, targetEnd), transition);
                }
                else
                {
                    boneMat0 = SampleBoneMatrix(progress, indices.x, currStart, currEnd);
                    boneMat1 = SampleBoneMatrix(progress, indices.y, currStart, currEnd);
                    boneMat2 = SampleBoneMatrix(progress, indices.z, currStart, currEnd);
                    boneMat3 = SampleBoneMatrix(progress, indices.w, currStart, currEnd);
                }

                float4 skinnedPos = mul(boneMat0, float4(input.positionOS, 1)) * weights.x
                                  + mul(boneMat1, float4(input.positionOS, 1)) * weights.y
                                  + mul(boneMat2, float4(input.positionOS, 1)) * weights.z
                                  + mul(boneMat3, float4(input.positionOS, 1)) * weights.w;

                float3 skinnedNormal = normalize(
                    mul((float3x3)boneMat0, input.normalOS) * weights.x +
                    mul((float3x3)boneMat1, input.normalOS) * weights.y +
                    mul((float3x3)boneMat2, input.normalOS) * weights.z +
                    mul((float3x3)boneMat3, input.normalOS) * weights.w);

                float3 positionWS = TransformObjectToWorld(skinnedPos.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(skinnedNormal);
                output.positionCS = GetShadowPositionHClip(positionWS, normalWS);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}