using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Renderer Feature: понижение цветности + дизеринг (PS1-постобработка).
/// Добавляется в URP Renderer asset. Шейдер — CrystalCastle/PSX_PostProcess.
/// </summary>
public class PSXPostProcessFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PSXSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [Range(2, 64)]  public int   colorLevels    = 32;  // градаций на канал
        [Range(0f, 1f)] public float ditherStrength = 1f;  // сила дизеринга
    }

    [SerializeField] private Shader shader;
    public PSXSettings settings = new PSXSettings();

    private Material _material;
    private PSXPass  _pass;

    public override void Create()
    {
        if (shader == null) return;
        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new PSXPass(_material, settings)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null) return;
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;
        renderer.EnqueuePass(_pass);
    }

    // cameraColorTargetHandle корректно доступен только здесь (URP 14)
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (_material == null) return;
        _pass.SetTarget(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
        _pass?.Dispose();
    }

    // ----------------------------------------------------------------
    private class PSXPass : ScriptableRenderPass
    {
        private readonly Material   _material;
        private readonly PSXSettings _settings;
        private RTHandle _source;
        private RTHandle _temp;

        public PSXPass(Material material, PSXSettings settings)
        {
            _material = material;
            _settings = settings;
            profilingSampler = new ProfilingSampler("PSX PostProcess");
        }

        public void SetTarget(RTHandle source) => _source = source;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples     = 1;
            RenderingUtils.ReAllocateIfNeeded(ref _temp, desc, FilterMode.Point,
                TextureWrapMode.Clamp, name: "_PSXTempTex");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _source == null) return;

            _material.SetFloat("_ColorLevels",    _settings.colorLevels);
            _material.SetFloat("_DitherStrength", _settings.ditherStrength);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                // нельзя блитить текстуру саму в себя — идём через временную
                Blitter.BlitCameraTexture(cmd, _source, _temp, _material, 0);
                Blitter.BlitCameraTexture(cmd, _temp, _source);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() => _temp?.Release();
    }
}
