using UnityEngine;

[RequireComponent(typeof(IHealth))]
public class DamageSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    
    private IHealth health;

    void Awake()
    {
        health = GetComponent<IHealth>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDamaged += PlayHitSound;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= PlayHitSound;
    }

    void PlayHitSound(float damageAmount)
    {
        if (audioSource == null || hitSound == null) return;
        
        audioSource.pitch = Random.Range(0.95f, 1.05f); // чуть вариативности
        audioSource.PlayOneShot(hitSound);
    }
}