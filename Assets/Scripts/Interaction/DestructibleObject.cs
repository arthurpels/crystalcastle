using UnityEngine;

public class DestructibleObject : MonoBehaviour, IHealth {
    [SerializeField] private float maxHP = 30f;
    [SerializeField] private GameObject destroyedPrefab; // обломки (опционально)
    [SerializeField] private AudioClip destroySound;

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
        // TODO: частицы, звук удара по дереву/металлу
    }

    private void DestroyObject() {
        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        if (destroyedPrefab != null)
            Instantiate(destroyedPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}