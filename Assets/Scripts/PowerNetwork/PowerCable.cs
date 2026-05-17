using UnityEngine;

public class PowerCable : MonoBehaviour, IInteractable
{
    public PowerNode nodeA;
    public PowerNode nodeB;
    public bool isBroken;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material brokenMat;

    void Start()
    {
        RegisterInNodes();
        UpdateVisual();
    }

    void Update()
    {
        // Автоотрисовка провода между нодами
        if (lineRenderer != null && nodeA != null && nodeB != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, nodeA.transform.position);
            lineRenderer.SetPosition(1, nodeB.transform.position);
        }
    }

    void OnDestroy()
    {
        if (nodeA != null) nodeA.connections.Remove(this);
        if (nodeB != null) nodeB.connections.Remove(this);
    }

    void RegisterInNodes()
    {
        if (nodeA != null && !nodeA.connections.Contains(this)) nodeA.connections.Add(this);
        if (nodeB != null && !nodeB.connections.Contains(this)) nodeB.connections.Add(this);
    }

    public PowerNode GetOtherEnd(PowerNode from)
    {
        if (from == nodeA) return nodeB;
        if (from == nodeB) return nodeA;
        return null;
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate();
    }

    public void Repair()
    {
        if (!isBroken) return;
        isBroken = false;
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate();
    }

    void UpdateVisual()
    {
        if (lineRenderer == null) return;
        lineRenderer.material = isBroken ? brokenMat : normalMat;
        var c = isBroken ? Color.red : new Color(1f, 0.8f, 0.2f);
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }

    public void Interact()
    {
        if (isBroken) Repair();
    }

    public string PromptText => isBroken ? "Починить кабель" : "Кабель исправен";
}