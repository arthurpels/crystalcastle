// PS1-стилизованный поверхностный шейдер для URP.
// Эффекты: снэп вершин, аффинные текстуры, повертексное освещение, туман.
Shader "CrystalCastle/PSX_Lit"
{
    Properties
    {
        [MainTexture] _BaseMap        ("Текстура", 2D)            = "white" {}
        [MainColor]   _BaseColor      ("Цвет",      Color)        = (1,1,1,1)

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float)  = 0
        _Cutoff        ("Порог прозрачности",    Range(0,1))      = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        _SnapResolution ("Снэп вершин (px)",     Float)           = 160
        _AffineAmount   ("Аффинная деформация",  Range(0,1))      = 1

        _LightTint     ("Оттенок света",         Color)           = (1,1,1,1)
        _AmbientBoost  ("Усиление ambient",      Range(0,2))      = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ---------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PSX_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _LightTint;
                float  _SnapResolution;
                float  _AffineAmount;
                float  _AmbientBoost;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvPersp    : TEXCOORD0;               // перспективно-корректные UV
                noperspective float2 uvAffine : TEXCOORD1;   // аффинные UV (искажение PS1)
                float3 lighting   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float4 color      : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float4 clipPos    = TransformWorldToHClip(positionWS);

                OUT.positionCS = PSX_SnapVertex(clipPos, _SnapResolution);

                OUT.uvPersp  = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvAffine = OUT.uvPersp;

                // --- Повертексное освещение ---
                float3 lighting = SampleSH(normalWS) * _AmbientBoost;

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                #else
                    Light mainLight = GetMainLight();
                #endif

                float ndotl = saturate(dot(normalWS, mainLight.direction));
                lighting += mainLight.color * ndotl
                          * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < lightCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, positionWS);
                        float nl = saturate(dot(normalWS, light.direction));
                        lighting += light.color * nl
                                  * light.distanceAttenuation * light.shadowAttenuation;
                    }
                #endif

                OUT.lighting  = lighting * _LightTint.rgb;
                OUT.color     = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv  = lerp(IN.uvPersp, IN.uvAffine, _AffineAmount);
                float4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor * IN.color;

                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                col.rgb *= IN.lighting;
                col.rgb  = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _LightTint;
                float  _SnapResolution;
                float  _AffineAmount;
                float  _AmbientBoost;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct ShadowVaryings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            ShadowVaryings ShadowVert (ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 clip = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    clip.z = min(clip.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z = max(clip.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = clip;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 ShadowFrag (ShadowVaryings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PSX_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _LightTint;
                float  _SnapResolution;
                float  _AffineAmount;
                float  _AmbientBoost;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct DepthAttributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct DepthVaryings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            DepthVaryings DepthVert (DepthAttributes IN)
            {
                DepthVaryings OUT;
                float4 clip = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionCS = PSX_SnapVertex(clip, _SnapResolution); // снэп — чтобы depth совпал с ForwardLit
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 DepthFrag (DepthVaryings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
