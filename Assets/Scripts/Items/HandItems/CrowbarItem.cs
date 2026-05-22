using UnityEngine;

public class CrowbarHandItem : HandItem {
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range  = 2.2f;
    [SerializeField] private LayerMask hitMask;

    [Header("Cooldown")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float _cooldownTimer;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   swingSound;
    [SerializeField] private AudioClip[] hitFleshSounds;   // звук попадания по врагу
    [SerializeField] private AudioClip[] hitHardSounds;    // звук попадания по стене/объекту

    [Header("VFX (опционально)")]
    [SerializeField] private ParticleSystem hitSparksPrefab;   // искры по стенам
    [SerializeField] private ParticleSystem hitBloodPrefab;    // кровь по врагам

    [Header("Screen shake (опционально)")]
    [SerializeField] private float shakeIntensity = 0.08f;
    [SerializeField] private float shakeDuration  = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    public override void OnEquip() { }
    public override void OnUnequip() { }

    public override void OnUse()
    {
        if (_cooldownTimer > 0f) return;
        _cooldownTimer = attackCooldown;

        Camera cam = Camera.main;
        Vector3 origin    = cam.transform.position;
        Vector3 direction = cam.transform.forward;

        // Звук замаха — всегда
        PlaySound(swingSound);

        if (showDebug)
            Debug.DrawRay(origin, direction * range, Color.red, 0.5f);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
            return; // промах

        IHealth target = hit.collider.GetComponentInParent<IHealth>();

        if (target != null && target.IsAlive)
        {
            target.TakeDamage(damage);
            SpawnFX(hitBloodPrefab,  hit.point, hit.normal);
            PlayRandomSound(hitFleshSounds);
            if (showDebug)
                Debug.Log($"[Crowbar] Удар по {hit.collider.name}: -{damage} HP");
        }
        else
        {
            SpawnFX(hitSparksPrefab, hit.point, hit.normal);
            PlayRandomSound(hitHardSounds);
        }

        TriggerShake();
    }

    public override void OnTick(float dt)
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= dt;
    }

    // ── VFX / SFX helpers ────────────────────────────────────────────────

    private static void SpawnFX(ParticleSystem prefab, Vector3 pos, Vector3 normal)
    {
        if (prefab == null) return;
        Vector3 spawnPos = pos + normal * 0.05f;
        var fx = Instantiate(prefab, spawnPos, Quaternion.LookRotation(normal));
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        audioSource.PlayOneShot(clip);
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySound(clips[Random.Range(0, clips.Length)]);
    }

    private void TriggerShake()
    {
        // Если есть CameraShake-компонент — вызываем его
        var shake = Camera.main?.GetComponent<CameraShake>();
        shake?.Shake(shakeIntensity, shakeDuration);
    }
}