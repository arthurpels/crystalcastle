using UnityEngine;

[ExecuteAlways]
public class PowerCable : MonoBehaviour, IInteractable
{
    public PowerNode nodeA;
    public PowerNode nodeB;
    public bool isBroken;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Material normalMat;   // обесточен, но цел
    [SerializeField] private Material liveMat;      // под напряжением (CrystalCastle/PowerFlow)
    [SerializeField] private Material brokenMat;

    // Кабель "живой", если цел и хотя бы один из его узлов запитан.
    private bool IsLive =>
        !isBroken && ((nodeA != null && nodeA.IsPowered) || (nodeB != null && nodeB.IsPowered));

    private bool _wasLive;

    void Start()
    {
        if (!Application.isPlaying) return;
        RegisterInNodes();
        UpdateVisual();
        _wasLive = IsLive;
    }

    void Update()
    {
        if (lineRenderer == null || nodeA == null || nodeB == null) return;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, nodeA.transform.position);
        lineRenderer.SetPosition(1, nodeB.transform.position);

        // Питание меняется через PowerNetwork.Evaluate (кабель об этом не уведомляют) —
        // поэтому опрашиваем состояние и обновляем материал только при смене.
        if (Application.isPlaying)
        {
            bool live = IsLive;
            if (live != _wasLive)
            {
                _wasLive = live;
                UpdateVisual();
            }
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

        if (isBroken)
        {
            lineRenderer.sharedMaterial = brokenMat;
            SetLineColor(Color.red);
        }
        else if (IsLive && liveMat != null)
        {
            // Под напряжением — бегущая светящаяся текстура.
            lineRenderer.sharedMaterial = liveMat;
            SetLineColor(Color.white); // белый = не глушим HDR-цвет шейдера
            ApplyFlowProps();
        }
        else
        {
            // Цел, но обесточен — тусклый.
            lineRenderer.sharedMaterial = normalMat;
            SetLineColor(new Color(0.35f, 0.3f, 0.15f));
        }
    }

    void SetLineColor(Color c)
    {
        lineRenderer.startColor = c;
        lineRenderer.endColor   = c;
    }

    private static readonly int IdWorldLength = Shader.PropertyToID("_WorldLength");
    private static readonly int IdFlowDir     = Shader.PropertyToID("_FlowDir");
    private MaterialPropertyBlock _mpb;

    /// <summary>
    /// Сообщает шейдеру PowerFlow длину провода (для постоянной плотности полос)
    /// и направление течения тока: позиция 0 LineRenderer = nodeA, 1 = nodeB,
    /// поток идёт от ноды, которую BFS запитал раньше (меньший ReachOrder).
    /// </summary>
    void ApplyFlowProps()
    {
        _mpb ??= new MaterialPropertyBlock();
        float length = Vector3.Distance(nodeA.transform.position, nodeB.transform.position);
        float dir    = (nodeA.ReachOrder <= nodeB.ReachOrder) ? 1f : -1f;

        lineRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(IdWorldLength, Mathf.Max(0.01f, length));
        _mpb.SetFloat(IdFlowDir, dir);
        lineRenderer.SetPropertyBlock(_mpb);
    }

    public void Interact()
    {
        if (isBroken) Repair();
    }

    public string PromptText => isBroken ? "Починить кабель" : "Кабель исправен";
}