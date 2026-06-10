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

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // Исключаем собственный слой игрока чтобы луч не бился об его коллайдер
        int playerMask        = 1 << gameObject.layer;
        int obstacleCheck     = obstacleLayers   & ~playerMask;
        int interactableCheck = interactableLayer & ~playerMask;

        // 1. Ближайшее препятствие ограничивает дальность поиска
        float searchDist = maxRange;
        if (obstacleCheck != 0 &&
            Physics.Raycast(ray, out RaycastHit obstacleHit, maxRange, obstacleCheck))
        {
            searchDist = obstacleHit.distance;
        }

        // 2. Точный рейкаст по interactableLayer, не дальше препятствия
        if (Physics.Raycast(ray, out RaycastHit hit, searchDist, interactableCheck))
        {
            newTarget = hit.collider.GetComponentInParent<IInteractable>();
        }

        // 3. Aim assist: сфера на конце луча
        if (newTarget == null && aimAssistRadius > 0f)
        {
            Vector3 sphereCenter = ray.GetPoint(searchDist);
            Collider[] hits = Physics.OverlapSphere(sphereCenter, aimAssistRadius, interactableCheck);
            if (hits.Length > 0)
                newTarget = hits[0].GetComponentInParent<IInteractable>();
        }

        _currentTarget = newTarget;
    }

    public void TryInteract() {
        _currentTarget?.Interact();
    }

    public IInteractable CurrentTarget => _currentTarget;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        int playerMask        = 1 << gameObject.layer;
        int obstacleCheck     = obstacleLayers   & ~playerMask;
        int interactableCheck = interactableLayer & ~playerMask;

        // Препятствие
        float searchDist = maxRange;
        bool hitObstacle = false;
        if (obstacleCheck != 0 &&
            Physics.Raycast(ray, out RaycastHit obsHit, maxRange, obstacleCheck))
        {
            searchDist  = obsHit.distance;
            hitObstacle = true;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(obsHit.point, 0.06f);
        }

        // Линия луча: до searchDist — жёлтая, остаток — серая (за препятствием)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ray.origin, ray.GetPoint(searchDist));
        if (!hitObstacle)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawLine(ray.GetPoint(searchDist), ray.GetPoint(maxRange));
        }

        // Сфера aim assist на конце луча
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(ray.GetPoint(searchDist), aimAssistRadius);

        // Интерактивный объект — зелёная точка + имя
        if (Physics.Raycast(ray, out RaycastHit intHit, searchDist, interactableCheck))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(intHit.point, 0.06f);
            UnityEditor.Handles.Label(intHit.point + Vector3.up * 0.15f, intHit.collider.name);
        }

        // Текущий target — голубой куб
        if (_currentTarget is UnityEngine.Component c)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(c.transform.position, Vector3.one * 0.35f);
        }
    }
#endif
}