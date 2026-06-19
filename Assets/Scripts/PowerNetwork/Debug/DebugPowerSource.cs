using UnityEngine;

/// <summary>
/// Тестовый источник питания. Вкл/Выкл через контекстное меню инспектора
/// (ПКМ на компоненте → Toggle / Turn On / Turn Off) или клавишей toggleKey.
/// </summary>
public class DebugPowerSource : MonoBehaviour, IPowerSource
{
    [SerializeField] private PowerNode outputNode;
    [SerializeField] private KeyCode   toggleKey   = KeyCode.F9;
    [SerializeField] private bool      startActive = true;

    public bool      IsActive   { get; private set; }
    public PowerNode OutputNode => outputNode;

    void Start()
    {
        IsActive = startActive;
        PowerNetwork.Instance?.RegisterSource(this);
        PowerNetwork.Instance?.Evaluate();
    }

    void OnDestroy()
    {
        PowerNetwork.Instance?.UnregisterSource(this);
    }

    void Update()
    {
        // if (Input.GetKeyDown(toggleKey))
        //     Toggle();
    }

    [ContextMenu("Toggle")]
    public void Toggle()
    {
        IsActive = !IsActive;
        Debug.Log($"[DebugPowerSource] {name} → {(IsActive ? "ON" : "OFF")}");
        PowerNetwork.Instance?.Evaluate();
    }

    [ContextMenu("Turn On")]
    public void TurnOn()
    {
        if (IsActive) return;
        IsActive = true;
        PowerNetwork.Instance?.Evaluate();
    }

    [ContextMenu("Turn Off")]
    public void TurnOff()
    {
        if (!IsActive) return;
        IsActive = false;
        PowerNetwork.Instance?.Evaluate();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (outputNode == null) return;
        Gizmos.color = IsActive ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, outputNode.transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
#endif
}
