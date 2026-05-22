using UnityEngine;
using System.Collections;

public class PistolHandItem : HandItem
{
    [Header("Shooting")]
    [SerializeField] private Transform shootTransform;
    [SerializeField] private float maxRange = 50f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private LayerMask hitMask;

    [Header("Ammo")]
    [SerializeField] private ItemData ammoType;
    [SerializeField] private int ammoPerShot = 1;

    [Header("Visuals & Audio")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private LineRenderer bulletTrailPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip emptyClickSound;
    [SerializeField] private AudioClip impactSound; // звук попадания (опционально)

    [Header("Feedback")]
    [SerializeField] private float shakeIntensity = 0.06f;
    [SerializeField] private float shakeDuration  = 0.12f;

    private float _cooldownTimer;
    private Camera _cam;

    void Start() => _cam = Camera.main;

    public override void OnEquip() { }
    public override void OnUnequip() { }

    public override void OnTick(float dt)
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= dt;
    }

    public override void OnUse()
    {
        if (_cooldownTimer > 0f) return;

        // Проверка патронов
        if (!PlayerInventory.Instance.HasItem(ammoType, ammoPerShot))
        {
            PlaySound(emptyClickSound);
            return;
        }

        // Трата патронов
        PlayerInventory.Instance.Consume(ammoType, ammoPerShot);
        _cooldownTimer = fireCooldown;
        Shoot();
    }

    private void Shoot()
    {
        PlaySound(shootSound);
        if (muzzleFlash) muzzleFlash.Play();
        TriggerShake();

        // Направление взгляда игрока
        Vector3 aimDir = _cam.transform.forward;
        Vector3 origin = _cam.transform.position;

        // Луч из камеры определяет точку прицеливания
        if (Physics.Raycast(origin, aimDir, out RaycastHit aimHit, maxRange, hitMask))
        {
            SpawnTrail(shootTransform.position, aimHit.point);
            ApplyDamage(aimHit.collider, aimHit.point);
            PlaySound(impactSound);
        }
        else
        {
            // Промах: пуля летит в бесконечность
            SpawnTrail(shootTransform.position, origin + aimDir * maxRange);
        }
    }

    private void SpawnTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrailPrefab == null) return;

        LineRenderer trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        trail.SetPositions(new[] { start, start });
        trail.gameObject.SetActive(true);
        StartCoroutine(AnimateTrail(trail, end));
    }

    private IEnumerator AnimateTrail(LineRenderer trail, Vector3 targetEnd)
    {
        Vector3 startPos = trail.GetPosition(0);
        float flyDuration = 0.12f; // Скорость полёта трассера
        float elapsed = 0f;

        // Фаза 1: Полёт трассера
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            trail.SetPosition(1, Vector3.Lerp(startPos, targetEnd, t));
            yield return null;
        }

        trail.SetPosition(1, targetEnd);

        // Фаза 2: Затухание
        float fadeDuration = 0.15f;
        elapsed = 0f;
        Color startColor = trail.startColor;
        Color fadeColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            trail.startColor = Color.Lerp(startColor, fadeColor, t);
            trail.endColor   = fadeColor;
            yield return null;
        }

        Destroy(trail.gameObject);
    }

    private void ApplyDamage(Collider target, Vector3 hitPoint)
    {
        IHealth health = target.GetComponentInParent<IHealth>();
        if (health != null && health.IsAlive)
        {
            health.TakeDamage(damage);
            // Сюда можно добавить спавн крови/искр, как в ломе:
            // SpawnHitFX(hitPoint, hit.normal, health is EnemyHealth);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = Random.Range(0.94f, 1.06f);
        audioSource.PlayOneShot(clip);
    }

    private void TriggerShake()
    {
        if (Camera.main.GetComponent<CameraShake>() != null ) {
            Camera.main.GetComponent<CameraShake>().Shake(shakeIntensity, shakeDuration);
        }
        
    }
}