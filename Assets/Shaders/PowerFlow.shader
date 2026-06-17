Shader "CrystalCastle/PowerFlow"
{
    // Бегущие светящиеся полосы вдоль провода — визуальный сигнал "линия под напряжением".
    // Анимация полностью на _Time (без кода в Update), цвет HDR — ловится Bloom'ом.
    // Рассчитан на LineRenderer (UV.x идёт вдоль провода).
    Properties
    {
        [HDR] _FlowColor  ("Цвет потока (HDR)", Color) = (0.3, 0.9, 1.0, 1.0)
        _Speed     ("Скорость бега",      Float)        = 2.0
        _Tiling    ("Полос на метр",       Float)        = 2.0
        _Sharpness ("Резкость импульса",   Range(1, 16)) = 5.0
        _BaseGlow  ("Базовое свечение",    Range(0, 1))  = 0.15
        // Задаются из PowerCable через MaterialPropertyBlock:
        _WorldLength ("Длина провода (м)", Float) = 1.0
        _FlowDir     ("Направление (+1/-1)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend One One   // аддитивно: читается как энергия/свет
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FlowColor;
                float  _Speed;
                float  _Tiling;
                float  _Sharpness;
                float  _BaseGlow;
                float  _WorldLength;
                float  _FlowDir;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Позиция вдоль провода в метрах → постоянная плотность полос
                // независимо от длины провода (_Tiling = полос на метр).
                float distM = IN.uv.x * _WorldLength;
                // _FlowDir (+1/-1) задаёт направление течения тока (из BFS).
                float wave  = frac(distM * _Tiling - _Time.y * _Speed * _FlowDir);
                // Острый "пробегающий" импульс.
                float pulse = pow(saturate(sin(wave * 3.14159265)), _Sharpness);
                float glow  = _BaseGlow + pulse;

                half3 col = _FlowColor.rgb * glow;
                // Учитываем вертекс-цвет LineRenderer (start/end color) как доп. тинт.
                return half4(col * IN.color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
