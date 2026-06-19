using UnityEngine;

public class DestructibleObject : MonoBehaviour, IHealth, ISaveable {

    [System.Serializable]
    public struct LootDrop {
        public ItemData item;
        [Min(1)] public int amount;
        [Range(0f, 1f)] public float chance;
    }

    [SerializeField] private float maxHP = 30f;
    [SerializeField] private GameObject destroyedPrefab; // обломки (опционально)
    [SerializeField] private LootDrop[] loot;            // выпадающие предметы
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip[] hitSounds;      // звуки удара (рандомный)

    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;
    public bool IsAlive => CurrentHP > 0;

    public event System.Action<float> OnHPChanged;
    public event System.Action OnDeath;

    public event System.Action<float> OnDamaged;

    /// <summary>Удар с точкой и нормалью попадания (для импакт-фидбека).</summary>
    public event System.Action<Vector3, Vector3> OnHit;

    private bool _hasPendingHit;
    private Vector3 _pendingHitPoint, _pendingHitNormal;

    void Awake() => CurrentHP = maxHP;

    public void TakeDamage(float amount) {
        if (!IsAlive) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnDamaged?.Invoke(amount);

        // Точка удара: из перегрузки ниже, иначе — центр объекта.
        Vector3 point  = _hasPendingHit ? _pendingHitPoint  : transform.position;
        Vector3 normal = _hasPendingHit ? _pendingHitNormal : Vector3.up;
        _hasPendingHit = false;
        OnHit?.Invoke(point, normal);

        // Эффект попадания (искры, звук)
        PlayHitEffect();

        if (!IsAlive) {
            OnDeath?.Invoke();
            DestroyObject();
        }
    }

    /// <summary>
    /// Опциональная перегрузка: атакующий, у которого есть RaycastHit,
    /// передаёт точку/нормаль — фидбек спавнит партиклы именно там.
    /// Интерфейс IHealth остаётся прежним.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal) {
        _pendingHitPoint  = hitPoint;
        _pendingHitNormal = hitNormal;
        _hasPendingHit    = true;
        TakeDamage(amount);
    }

    private void PlayHitEffect() {
        if (hitSounds != null && hitSounds.Length > 0)
        {
            var clip = hitSounds[Random.Range(0, hitSounds.Length)];
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, transform.position, 0.7f);
        }
    }

    private void DestroyObject()
    {
        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        if (destroyedPrefab != null)
            Instantiate(destroyedPrefab, transform.position, transform.rotation);

        SpawnLoot();

        // Уведомляем SaveManager перед уничтожением
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterDestroyed(this);
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        if (loot == null || loot.Length == 0) return;

        foreach (var drop in loot)
        {
            if (drop.item == null || drop.item.worldPrefab == null) continue;
            if (Random.value > drop.chance) continue;

            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
            var go = Instantiate(drop.item.worldPrefab, spawnPos, Random.rotation);

            if (go.TryGetComponent(out WorldItem wi))
            {
                wi.amount = drop.amount;
                wi.isRuntimeSpawned = true;
            }

            if (go.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity  = true;
                Vector3 scatter = new Vector3(
                    Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)
                ).normalized;
                rb.AddForce(scatter * Random.Range(2f, 4f), ForceMode.Impulse);
            }
        }
    }

    // ── ISaveable ──────────────────────────────────────────────────────────

    [System.Serializable]
    private struct DestructibleSaveData { public float hp; }

    public string CaptureState() =>
        JsonUtility.ToJson(new DestructibleSaveData { hp = CurrentHP });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<DestructibleSaveData>(json);
        CurrentHP = d.hp;
        OnHPChanged?.Invoke(CurrentHP);
    }
}