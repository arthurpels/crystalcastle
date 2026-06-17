// PS1-стилизованный шейдер с Triplanar Mapping (World Space Tiling).
// Идеально для террейна, снега, льда, грязи и больших поверхностей.
Shader "CrystalCastle/PSX_Lit_Triplanar"
{
    Properties
    {
        [MainTexture] _BaseMap        ("Текстура", 2D)            = "white" {}
        [MainColor]   _BaseColor      ("Цвет",      Color)        = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float)  = 0
        _Cutoff        ("Порог прозрачности", Range(0,1))      = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        // --- Настройки тайлинга в мировых координатах ---
        _WorldTiling      ("Мировой тайлинг (размер)", Float)   = 1.0
        _TriplanarSharpness("Резкость бленда (стены/пол)", Float)= 2.0

        // --- PSX Эффекты ---
        _SnapResolution ("Снэп вершин (px)",     Float)           = 160
        _AffineAmount   ("Аффинная деформация",  Range(0,1))      = 0

        _LightTint     ("Оттенок света",         Color)           = (1,1,1,1)
        _AmbientBoost  ("Усиление ambient",      Range(0,2))      = 1
        _WorldSnap       ("Снэп мира (прилипание)", Float) = 8.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ---------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
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
                float  _WorldTiling;
                float  _TriplanarSharpness;
                float  _WorldSnap;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                // Аффинные UV: xyz = перспективные UV * w, w = clip.w (для деления)
                noperspective float3 affineX : TEXCOORD1;
                noperspective float3 affineY : TEXCOORD2;
                noperspective float3 affineZ : TEXCOORD3;
                float3 normalWS    : TEXCOORD4;
                float  fogFactor   : TEXCOORD5;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float4 clipPos    = TransformWorldToHClip(positionWS);

                OUT.positionCS = PSX_SnapVertex(clipPos, _SnapResolution);
                OUT.positionWS = positionWS;
                OUT.normalWS   = normalWS;
                OUT.color      = IN.color;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                // Аффинное искажение PS1: UV * w → при noperspective-интерполяции
                // делении обратно на w получаем линейные (не perspective-correct) координаты
                float3 worldUV = positionWS * _WorldTiling;
                float  w = clipPos.w;
                OUT.affineX = float3(worldUV.zy * w, w);
                OUT.affineY = float3(worldUV.xz * w, w);
                OUT.affineZ = float3(worldUV.xy * w, w);

                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                // --- Квантование мировых координат ---
                float3 snappedPosWS = IN.positionWS;
                if (_WorldSnap > 0.1)
                    snappedPosWS = floor(IN.positionWS * _WorldSnap) / _WorldSnap;

                float3 worldUV = snappedPosWS * _WorldTiling;

                // Triplanar blending weights
                float3 blendWeights = abs(IN.normalWS);
                blendWeights = pow(blendWeights, _TriplanarSharpness);
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                // Perspective-correct UV
                float2 uvX_persp = worldUV.zy;
                float2 uvY_persp = worldUV.xz;
                float2 uvZ_persp = worldUV.xy;

                // Affine UV (деление на w восстанавливает линейные координаты)
                float2 uvX_affine = IN.affineX.xy / IN.affineX.z;
                float2 uvY_affine = IN.affineY.xy / IN.affineY.z;
                float2 uvZ_affine = IN.affineZ.xy / IN.affineZ.z;

                float2 uvX = lerp(uvX_persp, uvX_affine, _AffineAmount);
                float2 uvY = lerp(uvY_persp, uvY_affine, _AffineAmount);
                float2 uvZ = lerp(uvZ_persp, uvZ_affine, _AffineAmount);
                
                float4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                float4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);

                float4 col = colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
                col *= _BaseColor * IN.color;

                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                // --- ПОПИКСЕЛЬНОЕ ОСВЕЩЕНИЕ (Исправляет баги света) ---
                float3 normalWS = normalize(IN.normalWS);
                
                // Ambient
                float3 lighting = SampleSH(normalWS) * _AmbientBoost;

                // Main Light
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                #else
                    Light mainLight = GetMainLight();
                #endif

                float ndotl = saturate(dot(normalWS, mainLight.direction));
                lighting += mainLight.color * ndotl
                           * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Additional Lights
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < lightCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, IN.positionWS);
                        float nl = saturate(dot(normalWS, light.direction));
                        lighting += light.color * nl
                                  * light.distanceAttenuation * light.shadowAttenuation;
                    }
                #endif

                col.rgb *= lighting * _LightTint.rgb;
                col.rgb = MixFog(col.rgb, IN.fogFactor);
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
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "PSX_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _LightTint;
                float  _SnapResolution;
                float  _AffineAmount;
                float  _AmbientBoost;
                float  _Cutoff;
                float  _WorldTiling;
                float  _TriplanarSharpness;
                float  _WorldSnap;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct ShadowVaryings   { 
                float4 positionCS:SV_POSITION; 
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
            };

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

                OUT.positionCS = PSX_SnapVertex(clip, _SnapResolution);
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;
                return OUT;
            }

            float4 ShadowFrag (ShadowVaryings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float3 blendWeights = abs(IN.normalWS);
                    blendWeights = pow(blendWeights, _TriplanarSharpness);
                    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                    float3 worldUV = IN.positionWS * _WorldTiling;
                    float aX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.zy).a;
                    float aY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.xz).a;
                    float aZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.xy).a;

                    float a = (aX * blendWeights.x + aY * blendWeights.y + aZ * blendWeights.z) * _BaseColor.a;
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
            #pragma vertex DepthVert
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
                float  _WorldTiling;
                float  _TriplanarSharpness;
                float  _WorldSnap;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct DepthAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct DepthVaryings   { 
                float4 positionCS:SV_POSITION; 
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
            };

            DepthVaryings DepthVert (DepthAttributes IN)
            {
                DepthVaryings OUT;
                float4 clip = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionCS = PSX_SnapVertex(clip, _SnapResolution); 
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 DepthFrag (DepthVaryings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float3 blendWeights = abs(IN.normalWS);
                    blendWeights = pow(blendWeights, _TriplanarSharpness);
                    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                    float3 worldUV = IN.positionWS * _WorldTiling;
                    float aX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.zy).a;
                    float aY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.xz).a;
                    float aZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUV.xy).a;

                    float a = (aX * blendWeights.x + aY * blendWeights.y + aZ * blendWeights.z) * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   DepthNormalsVert
            #pragma fragment DepthNormalsFrag

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
                float  _WorldTiling;
                float  _TriplanarSharpness;
                float  _WorldSnap;
            CBUFFER_END

            struct DNAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct DNVaryings   { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; };

            DNVaryings DepthNormalsVert(DNAttributes IN)
            {
                DNVaryings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float4 clip = TransformWorldToHClip(positionWS);
                OUT.positionCS = PSX_SnapVertex(clip, _SnapResolution);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 DepthNormalsFrag(DNVaryings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                return float4(n * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}