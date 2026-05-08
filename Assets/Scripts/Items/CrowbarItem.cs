using UnityEngine;

public class CrowbarHandItem : HandItem
{
    [Header("Combat")]
    public float damage = 25f;
    public float hitRange = 2.5f;
    public float swingCooldown = 0.6f;
    
    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip hitSound;

    private float _cooldownTimer;
    private Camera _cam;
    private AudioSource _audio;
    private Animator _playerAnimator;

    private void Awake()
    {
        _cam = Camera.main;
        _audio = GetComponent<AudioSource>();
        _playerAnimator = GetComponentInParent<Animator>();
    }

    public override void OnEquip() {}
    public override void OnUnequip() {}

    public override void OnUse()
    {
        if (_cooldownTimer > 0f) return;
        Swing();
    }

    private void Swing()
    {
        _cooldownTimer = swingCooldown;
        if (_audio && swingSound) _audio.PlayOneShot(swingSound);
        if (_playerAnimator) _playerAnimator.SetTrigger("CrowbarSwing");
        CheckHit();
    }

    private void CheckHit()
    {
        if (_cam == null) return;
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out RaycastHit hit, hitRange))
        {
            Debug.DrawRay(ray.origin, ray.direction * hitRange, Color.red, 0.5f);
            
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log($"Hit Enemy: {damage} dmg!");
                if (_audio && hitSound) _audio.PlayOneShot(hitSound);
                // hit.collider.GetComponent<Health>()?.TakeDamage(damage);
            }
        }
    }

    public override void OnTick(float dt)
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= dt;
    }
}