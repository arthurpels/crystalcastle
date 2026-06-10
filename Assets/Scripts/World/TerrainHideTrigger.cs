using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TerrainHideTrigger : MonoBehaviour
{
    [SerializeField] private Terrain[] terrains;
    [SerializeField] private string    playerTag = "Player";

    private static int _globalCount;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (terrains == null || terrains.Length == 0)
        {
            var active = Terrain.activeTerrain;
            if (active != null) terrains = new[] { active };
        }
    }

    void OnDisable()
    {
        // Сбрасываем при деактивации чтобы не зависнуть
        _globalCount = 0;
        SetTerrainEnabled(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _globalCount++;
        if (_globalCount == 1) SetTerrainEnabled(false);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _globalCount = Mathf.Max(0, _globalCount - 1);
        if (_globalCount == 0) SetTerrainEnabled(true);
    }

    void SetTerrainEnabled(bool enabled)
    {
        foreach (var t in terrains)
        {
            if (t == null) continue;
            t.enabled = enabled;
            var col = t.GetComponent<TerrainCollider>();
            if (col != null) col.enabled = enabled;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        if (col is BoxCollider box2)
            Gizmos.DrawWireCube(box2.center, box2.size);
    }
#endif
}
