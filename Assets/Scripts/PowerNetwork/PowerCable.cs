using UnityEngine;

public class PowerCable : MonoBehaviour, IInteractable {
  public PowerNode nodeA;
  public PowerNode nodeB;
  public bool isBroken;

  [Header("Visual")]
  [SerializeField]
  private LineRenderer lineRenderer;
  [SerializeField]
  private Material normalMat;
  [SerializeField]
  private Material brokenMat;

  void Start() => UpdateVisual();

  public PowerNode GetOtherEnd(PowerNode from) {
    if (from == nodeA)
      return nodeB;
    if (from == nodeB)
      return nodeA;
    return null;
  }

  public void Break() {
    if (isBroken)
      return;
    isBroken = true;
    UpdateVisual();
    PowerNetwork.Instance?.Evaluate();
  }

  public void Repair() {
    if (!isBroken)
      return;
    isBroken = false;
    UpdateVisual();
    PowerNetwork.Instance?.Evaluate();
  }

  void UpdateVisual() {
    if (lineRenderer == null)
      return;
    lineRenderer.material = isBroken ? brokenMat : normalMat;
    var c = isBroken ? Color.red : new Color(1f, 0.8f, 0.2f);
    lineRenderer.startColor = c;
    lineRenderer.endColor = c;
  }

  public void Interact() {
    if (isBroken)
      Repair();
  }

  public string PromptText =>
      isBroken ? "[E] Починить кабель" : "Кабель исправен";
}
