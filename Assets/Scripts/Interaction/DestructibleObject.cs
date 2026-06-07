using UnityEngine;

public class DestructibleObject : MonoBehaviour, IHealth, ISaveable {
    [SerializeField] private float maxHP = 30f;
    [SerializeField] private GameObject destroyedPrefab; // обломки (опционально)
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip[] hitSounds;      // звуки удара (рандомный)

    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;
    public bool IsAlive => CurrentHP > 0;

    public event System.Action<float> OnHPChanged;
    public event System.Action OnDeath;

    public event System.Action<float> OnDamaged;


    void Awake() => CurrentHP = maxHP;

    public void TakeDamage(float amount) {
        if (!IsAlive) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnDamaged?.Invoke(amount);

        // Эффект попадания (искры, звук)
        PlayHitEffect();

        if (!IsAlive) {
            OnDeath?.Invoke();
            DestroyObject();
        }
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

        // Уведомляем SaveManager перед уничтожением
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterDestroyed(this);
        Destroy(gameObject);
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