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

        // 1. Точный рейкаст
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, interactableLayer)) {
            newTarget = hit.collider.GetComponentInParent<IInteractable>();
        }

        // 2. Если луч промахнулся → проверяем сферу на конце
        if (newTarget == null && aimAssistRadius > 0f) {
            if (Physics.Raycast(ray, out RaycastHit obstacleHit, maxRange, obstacleLayers)) {
                sphereDistance = obstacleHit.distance;
            }

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
}