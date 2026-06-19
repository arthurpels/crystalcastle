using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Детонатор: IPowerable на выходной ноде таймера. Получил питание → взрыв.
/// Делает VFX, звук, тряску, опц. урон по радиусу (убьёт игрока, если не сбежал,
/// и врагов), переключает объекты на «разрушенное» состояние и дёргает onDetonated
/// (туда — экран концовки/GameEnding).
///
/// Вешать на объект ПОД выходной нодой таймера (PowerNode ищет IPowerable в детях).
/// </summary>
public class Detonator : MonoBehaviour, IPowerable
{
    [Header("Срабатывание")]
    [SerializeField] private bool onlyOnce = true;

    [Header("Эффекты")]
    [Tooltip("Взрыв: префаб (Instantiate) или заранее размещённый объект (SetActive).")]
    [SerializeField] private GameObject explosionVfx;
    [Tooltip("true — explosionVfx это префаб (заспавнить); false — объект в сцене (включить).")]
    [SerializeField] private bool vfxIsPrefab = false;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float shakeIntensity = 0.4f;
    [SerializeField] private float shakeDuration  = 0.8f;

    [Header("Урон по радиусу (опц.)")]
    [SerializeField] private bool      dealDamage = true;
    [SerializeField] private float     radius     = 8f;
    [SerializeField] private float     damage     = 9999f;
    [SerializeField] private LayerMask damageMask = ~0;

    [Header("Переключение объектов")]
    [Tooltip("Выключить при взрыве (целые бочки/ТНТ/кристалл).")]
    [SerializeField] private GameObject[] disableOnDetonate;
    [Tooltip("Включить при взрыве (обломки/разрушенное состояние).")]
    [SerializeField] private GameObject[] enableOnDetonate;

    [Header("Событие")]
    public UnityEvent onDetonated;   // экран концовки / GameEnding

    public bool IsPowered { get; private set; }
    public bool HasDetonated => _detonated;

    private bool _detonated;

    // ── IPowerable ───────────────────────────────────────────────────────────
    public void OnPowerChanged(bool powered, bool force = false)
    {
        IsPowered = powered;
        if (powered) Detonate();
    }

    [ContextMenu("Detonate")]
    public void Detonate()
    {
        if (onlyOnce && _detonated) return;
        _detonated = true;

        // VFX
        if (explosionVfx != null)
        {
            if (vfxIsPrefab) Instantiate(explosionVfx, transform.position, transform.rotation);
            else             explosionVfx.SetActive(true);
        }

        // Звук
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);

        // Тряска
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeIntensity, shakeDuration);

        // Переключение состояний
        if (disableOnDetonate != null)
            foreach (var go in disableOnDetonate) if (go != null) go.SetActive(false);
        if (enableOnDetonate != null)
            foreach (var go in enableOnDetonate)  if (go != null) go.SetActive(true);

        // Урон по радиусу (игрок рядом — умер; враги — тоже)
        if (dealDamage)
        {
            var hits = Physics.OverlapSphere(transform.position, radius, damageMask, QueryTriggerInteraction.Ignore);
            var done = new HashSet<IHealth>();
            foreach (var col in hits)
            {
                var hp = col.GetComponentInParent<IHealth>();
                if (hp != null && hp.IsAlive && done.Add(hp))
                    hp.TakeDamage(damage);
            }
        }

        onDetonated?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!dealDamage) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
