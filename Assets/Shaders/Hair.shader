Shader "URP/Hair"
{
    Properties
    {
        [Header(Base Parameters)]
        _BaseColor ("Base Color", Color) = (0.5, 0.3, 0.2, 1.0)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Hair Parameters)]
        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.5
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        _Specular ("Specular", Range(0.0, 1.0)) = 0.5
        _SpecularMap ("Specular Map", 2D) = "white" {}

        [Header(Advanced)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Enable Normal Map", Float) = 0.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 1.0

        [Header(Rendering Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0.0
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Alpha Test", Float) = 1.0
        [Toggle(_ALPHABLEND_ON)] _AlphaBlend ("Alpha Blend", Float) = 0.0
        [Toggle(_ALPHAPREMULTIPLY_ON)] _AlphaPremultiply ("Alpha Premultiply", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 着色器特性
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _ALPHABLEND_ON
            #pragma shader_feature_local _ALPHAPREMULTIPLY_ON

            // 包含URP核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 包含你的头发渲染头文件
            #include "HairRendering.hlsl" // 确保这个文件名与你的头文件一致

            // 材质属性
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _NormalMap_ST;
            float4 _RoughnessMap_ST;
            float4 _SpecularMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Roughness;
            half _Specular;
            half _NormalScale;
            CBUFFER_END

            // 纹理采样器
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_SpecularMap);
            SAMPLER(sampler_SpecularMap);

            // 顶点输入结构
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            // 顶点输出结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            // 顶点着色器
            Varyings vert(Attributes input)
            {
                Varyings output;

                // 使用URP的VertexPositionInputs结构体处理位置信息
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                // 计算裁剪空间位置
                output.positionCS = vertexInput.positionCS;
                
                // 计算世界空间位置
                output.positionWS = vertexInput.positionWS;

                // 计算世界空间法线、切线和副切线
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;

                // 纹理坐标
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // 简化阴影坐标计算，直接使用世界空间位置
                output.shadowCoord = float4(output.positionWS, 1.0);

                return output;
            }

            // 片段着色器
            half4 frag(Varyings input) : SV_Target
            {
                // 采样基础颜色纹理
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Alpha测试
                #ifdef _ALPHATEST_ON
                    clip(baseColor.a - _Cutoff);
                #endif

                // 采样粗糙度和高光纹理
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r * _Roughness;
                half specular = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, input.uv).r * _Specular;

                // 计算头发方向向量B（这里使用切线作为头发方向，指向发梢）
                float3 B = normalize(input.tangentWS);

                // 计算视图方向V（从表面指向相机）
                float3 V = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                // 计算光照方向L和光照颜色（简化阴影处理）
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation;

                // 计算头发基本数据
                HairTempData hairData;
                HairBaseDataCal(hairData, B, V, L, roughness);

                // 计算漫反射
                half3 diffuse = DiffuseCal(mainLight, 1.0, baseColor.rgb, L, V, B);

                // 计算高光
                half3 specularColor = SpecularCal(hairData, mainLight, baseColor.rgb, specular);

                // 最终颜色
                half3 finalColor = diffuse + specularColor;

                // 应用Alpha混合
                #ifdef _ALPHABLEND_ON
                    #ifdef _ALPHAPREMULTIPLY_ON
                        return half4(finalColor * baseColor.a, baseColor.a);
                    #else
                        return half4(finalColor, baseColor.a);
                    #endif
                #else
                    return half4(finalColor, 1.0);
                #endif
            }
            ENDHLSL
        }

        // 简化的阴影投射通道
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON

            // 只包含必要的核心库，避免使用有问题的Shadows.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 直接计算裁剪空间位置，不使用URP的阴影函数
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // Alpha测试
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #ifdef _ALPHATEST_ON
                    clip(baseColor.a - _Cutoff);
                #endif
                // 阴影投射通道只需要输出深度，不需要颜色
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShaderGUI"
}