using UnityEngine;

public class PistolHandItem : HandItem
{
    [Header("Shooting")]
    [SerializeField] private Transform shootTransform;      // точка вылета пули (конец дула)
    [SerializeField] private float maxRange = 50f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float fireCooldown = 0.15f;
    [SerializeField] private LayerMask hitMask;

    [Header("Ammo")]
    [SerializeField] private ItemData ammoType;             // ссылка на ItemData патронов
    [SerializeField] private int ammoPerShot = 1;

    [Header("Visual")]
    [SerializeField] private ParticleSystem muzzleFlash;    // вспышка выстрела
    [SerializeField] private TrailRenderer bulletTrail;     // след пули (префаб)

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

        // Проверяем патроны
        if (!PlayerInventory.Instance.HasItem(ammoType, ammoPerShot))
        {
            Debug.Log("[Pistol] Нет патронов!");
            // TODO: звук щелчка пустого магазина
            return;
        }

        // Тратим патрон
        PlayerInventory.Instance.Consume(ammoType, ammoPerShot);

        _cooldownTimer = fireCooldown;
        Shoot();
    }

    private void Shoot()
    {
        // === 1. Луч из камеры — куда целится игрок ===
        Vector3 screenCenter = _cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimDir = _cam.transform.forward;

        // Raycast из камеры для определения точки прицеливания
        Vector3 aimPoint = screenCenter + aimDir * maxRange;
        if (Physics.Raycast(screenCenter, aimDir, out RaycastHit aimHit, maxRange, hitMask))
        {
            aimPoint = aimHit.point;
        }

        // === 2. Основной луч из дула в точку прицеливания ===
        Vector3 shootDir = (aimPoint - shootTransform.position).normalized;
        
        bool hasHit = Physics.Raycast(shootTransform.position, shootDir, out RaycastHit hit, maxRange, hitMask);

        // === 3. Визуал ===
        Vector3 endPoint = hasHit ? hit.point : shootTransform.position + shootDir * maxRange;
        SpawnBulletTrail(endPoint);
        if (muzzleFlash != null) muzzleFlash.Play();

        // === 4. Урон ===
        if (hasHit)
        {
            IHealth target = hit.collider.GetComponentInParent<IHealth>();
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage);
            }
        }
    }

    private void SpawnBulletTrail(Vector3 endPoint)
    {
        if (bulletTrail == null) return;

        TrailRenderer trail = Instantiate(bulletTrail, shootTransform.position, Quaternion.identity);
        trail.AddPosition(shootTransform.position);

        // Анимируем след к точке попадания
        float distance = Vector3.Distance(shootTransform.position, endPoint);
        float duration = distance / 100f; // скорость следа ~100 м/с

        StartCoroutine(AnimateTrail(trail, endPoint, duration));
    }

    private System.Collections.IEnumerator AnimateTrail(TrailRenderer trail, Vector3 end, float duration)
    {
        float t = 0f;
        Vector3 start = trail.transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            trail.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        trail.transform.position = end;
        yield return new WaitForSeconds(trail.time); // ждём пока след растворится
        Destroy(trail.gameObject);
    }
}