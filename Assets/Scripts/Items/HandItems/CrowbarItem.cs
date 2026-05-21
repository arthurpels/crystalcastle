using UnityEngine;

public class CrowbarHandItem : HandItem {
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 7f;
    [SerializeField] private LayerMask hitMask;

    [Header("Cooldown")]
    [SerializeField] private float attackCooldown = 0.8f;
    private float _cooldownTimer;

    [Header("Debug Visuals")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private float debugLineDuration = 5f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swingSound;

    public override void OnEquip() { }
    public override void OnUnequip() { }

    public override void OnUse() {

        if (_cooldownTimer > 0f) return;

        _cooldownTimer = attackCooldown;

        Camera cam = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward;

        if (audioSource != null && swingSound != null)
            audioSource.PlayOneShot(swingSound);

        // ВИЗУАЛ: луч удара
        if (showDebug)
            Debug.DrawRay(origin, direction * range, Color.red, debugLineDuration);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask)) {
            // ВИЗУАЛ: сфера в точке попадания
            if (showDebug)
                Debug.DrawLine(origin, hit.point, Color.green, debugLineDuration);

            IHealth target = hit.collider.GetComponentInParent<IHealth>();

            if (target != null && target.IsAlive) {
                // ВИЗУАЛ: большая сфера, лог
                if (showDebug) {
                    Debug.DrawRay(hit.point, Vector3.up * 0.5f, Color.yellow, debugLineDuration);
                    Debug.Log($"<color=green>[HIT]</color> {hit.collider.name} HP: {target.CurrentHP} → {target.CurrentHP - damage}");
                }

                target.TakeDamage(damage);
                SpawnHitEffect(hit.point, hit.normal);
            } else {
                // Попали в объект без IHealth (стена, пол)
                if (showDebug)
                    Debug.Log($"<color=gray>[MISS]</color> {hit.collider.name} — нет IHealth");

                SpawnMissEffect(hit.point, hit.normal);
            }
        } else {
            // Луч в никуда
            if (showDebug)
                Debug.Log("<color=red>[WHIFF]</color> Луч не попал ни во что");
        }
    }
    public override void OnTick(float dt) {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;
    }

    private void SpawnHitEffect(Vector3 pos, Vector3 normal) {
        // Временный визуал: куб на 0.5 секунды
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = pos;
        cube.transform.rotation = Quaternion.LookRotation(normal);
        cube.transform.localScale = Vector3.one * 0.1f;
        Destroy(cube.GetComponent<Collider>());
        Destroy(cube, 0.5f);
    }

    private void SpawnMissEffect(Vector3 pos, Vector3 normal) {
        // Временный визуал: сфера на 0.3 секунды
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * 0.15f;
        Destroy(sphere.GetComponent<Collider>());
        Destroy(sphere, 0.3f);
    }
}