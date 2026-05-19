#ifndef PSX_COMMON_INCLUDED
#define PSX_COMMON_INCLUDED

// Снэп позиции в clip-space на пиксельную сетку — вершинный джиттер PS1.
// resolution ≈ половина горизонтального разрешения кадра (160 → ~320 "пикселей").
// Подключать ПОСЛЕ Core.hlsl (нужен _ScreenParams).
float4 PSX_SnapVertex(float4 clipPos, float resolution)
{
    if (resolution <= 0.0) return clipPos;

    // По вертикали корректируем на соотношение сторон, чтобы ячейка была квадратной
    float2 grid = float2(resolution, resolution * (_ScreenParams.y / _ScreenParams.x));

    float4 snapped = clipPos;
    snapped.xy = clipPos.xy / clipPos.w;          // в NDC
    snapped.xy = floor(grid * snapped.xy) / grid; // прибиваем к сетке
    snapped.xy *= clipPos.w;                      // обратно в clip-space
    return snapped;
}

#endif
