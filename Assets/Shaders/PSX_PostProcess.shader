// Полноэкранный PS1-эффект: понижение цветности + ordered dithering (матрица Байера 4x4).
// Запускается через PSXPostProcessFeature (ScriptableRendererFeature).
Shader "CrystalCastle/PSX_PostProcess"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off

        Pass
        {
            Name "PSXPost"
            ZTest Always

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Blit.hlsl"

            float _ColorLevels;     // число градаций на канал (PS1 ≈ 32)
            float _DitherStrength;  // 0 — жёсткая постеризация, 1 — полный дизеринг

            // Матрица Байера 4x4 для упорядоченного дизеринга
            static const float BAYER[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            float4 frag (Varyings IN) : SV_Target
            {
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, IN.texcoord).rgb;

                // Индекс в матрице Байера по экранным пикселям
                int2 px = int2(IN.positionCS.xy) & 3;          // % 4
                float dither = (BAYER[px.y * 4 + px.x] + 0.5) / 16.0; // диапазон (0,1)

                // Квантование цвета с дизерингом
                float levels = max(_ColorLevels, 2.0) - 1.0;
                col = floor(col * levels + dither * _DitherStrength) / levels;

                return float4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
