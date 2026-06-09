using UnityEngine;

public class InteractionManager : MonoBehaviour {
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float aimAssistRadius = 0.2f; // Сфера на конце луча

    private IInteractable _currentTarget;

    private void Awake() {
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update() => UpdateTarget();

    private void UpdateTarget() {
        IInteractable newTarget = null;
        float sphereDistance = maxRange;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        LayerMask combinedMask = interactableLayer | obstacleLayers;

        // 1. Точный рейкаст по интерактивным И препятствиям вместе.
        //    Если первым попалось препятствие — объект за ним недоступен.
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, combinedMask)) {
            sphereDistance = hit.distance;
            // IInteractable ищем только если хит попал именно в interactableLayer.
            // Объект на obstacle/другом слое — блокирует луч, но не интерактивен.
            if (IsInLayerMask(hit.collider.gameObject.layer, interactableLayer))
                newTarget = hit.collider.GetComponentInParent<IInteractable>();
        }

        // 2. Если луч промахнулся → проверяем сферу у точки попадания / конца луча.
        //    sphereDistance уже ограничен первым хитом, стена не пропустит сферу дальше.
        if (newTarget == null && aimAssistRadius > 0f) {
            Vector3 sphereCenter = ray.GetPoint(sphereDistance);
            Collider[] hits = Physics.OverlapSphere(sphereCenter, aimAssistRadius, interactableLayer);

            if (hits.Length > 0)
                newTarget = hits[0].GetComponentInParent<IInteractable>();
        }

        _currentTarget = newTarget;
    }

    public void TryInteract() {
        _currentTarget?.Interact();
    }

    public IInteractable CurrentTarget => _currentTarget;

    private static bool IsInLayerMask(int layer, LayerMask mask) =>
        (mask.value & (1 << layer)) != 0;
}