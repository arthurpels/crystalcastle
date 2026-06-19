using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Концовка «вознесение». Следит, сколько тесла-стендов реально запитано
/// (IsEnergized = тесла стоит И нода под током), и по доле x/n плавно нагнетает:
///   1) интенсивность источника света;
///   2) HDR-эмиссию кристалла (PSX_Unlit): от тёмного цвета до белого + сила;
///   3) масштаб контейнера с кристаллами;
///   4) включает молнии — по одной от каждого запитанного стенда к кристаллу.
/// Когда запитаны все — дёргает onFullyPowered (катсцена) и лочит стенды.
/// </summary>
public class AscensionController : MonoBehaviour
{
    [Header("Стенды")]
    [SerializeField] private TeslaStand[] stands;
    [Tooltip("Считать по запитанным (тесла + ток). Выкл — считать просто по установленным.")]
    [SerializeField] private bool requirePower = true;

    [Header("1. Свет")]
    [SerializeField] private Light crystalLight;
    [SerializeField] private float lightMin = 0.2f;
    [SerializeField] private float lightMax = 8f;

    [Header("2. Эмиссия кристалла (PSX_Unlit)")]
    [SerializeField] private Renderer[] crystalRenderers;
    [SerializeField] private Color emissionDark = new Color(0.10f, 0.10f, 0.16f);
    [SerializeField] private Color emissionFull = Color.white;
    [SerializeField] private float strengthMin = 1f;
    [SerializeField] private float strengthMax = 6f;

    [Header("3. Размер")]
    [SerializeField] private Transform crystalContainer;
    [SerializeField] private float scaleMin = 1f;
    [SerializeField] private float scaleMax = 2f;

    [Header("4. Молнии (параллельно stands)")]
    [Tooltip("Объект молнии [i] включается, когда запитан stands[i].")]
    [SerializeField] private GameObject[] lightningPerStand;

    [Header("Плавность")]
    [Tooltip("Скорость нагнетания эффекта (доля в секунду).")]
    [SerializeField] private float rampSpeed = 1.5f;

    [Header("Финал")]
    public UnityEvent onFullyPowered;

    private static readonly int IdEmission = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdStrength = Shader.PropertyToID("_EmissionStrength");

    private MaterialPropertyBlock _mpb;
    private Vector3 _baseScale = Vector3.one;
    private float   _current;     // сглаженный прогресс 0..1
    private bool    _fullFired;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (crystalContainer != null) _baseScale = crystalContainer.localScale;
    }

    private void Update()
    {
        int n = stands != null ? stands.Length : 0;
        int active = CountActive();
        float target = n > 0 ? (float)active / n : 0f;

        _current = Mathf.MoveTowards(_current, target, rampSpeed * Time.deltaTime);

        ApplyLight(_current);
        ApplyEmission(_current);
        ApplyScale(_current);
        ApplyLightning();

        if (!_fullFired && n > 0 && active >= n)
        {
            _fullFired = true;
            LockStands();
            onFullyPowered?.Invoke();
        }
    }

    private bool IsActive(TeslaStand s) =>
        s != null && (requirePower ? s.IsEnergized : s.IsFilled);

    private int CountActive()
    {
        int c = 0;
        if (stands != null)
            foreach (var s in stands) if (IsActive(s)) c++;
        return c;
    }

    private void ApplyLight(float t)
    {
        if (crystalLight != null)
            crystalLight.intensity = Mathf.Lerp(lightMin, lightMax, t);
    }

    private void ApplyEmission(float t)
    {
        if (crystalRenderers == null) return;

        Color emis = Color.Lerp(emissionDark, emissionFull, t);
        float str  = Mathf.Lerp(strengthMin, strengthMax, t);

        foreach (var r in crystalRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(IdEmission, emis);
            _mpb.SetFloat(IdStrength, str);
            r.SetPropertyBlock(_mpb);
        }
    }

    private void ApplyScale(float t)
    {
        if (crystalContainer != null)
            crystalContainer.localScale = _baseScale * Mathf.Lerp(scaleMin, scaleMax, t);
    }

    private void ApplyLightning()
    {
        if (lightningPerStand == null) return;

        for (int i = 0; i < lightningPerStand.Length; i++)
        {
            if (lightningPerStand[i] == null) continue;
            bool on = stands != null && i < stands.Length && IsActive(stands[i]);
            if (lightningPerStand[i].activeSelf != on)
                lightningPerStand[i].SetActive(on);
        }
    }

    private void LockStands()
    {
        if (stands == null) return;
        foreach (var s in stands) if (s != null) s.Lock();
    }
}
