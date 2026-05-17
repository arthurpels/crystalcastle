using UnityEngine;

public class PowerGenerator : MonoBehaviour, IInteractable {
  [Header("Grid")]
  [SerializeField]
  private PowerNode outputNode;
  public PowerNode OutputNode => outputNode;

  [Header("Puzzle")]
  [SerializeField]
  private SineWavePuzzleUI puzzleUI;

  public bool IsActive { get; private set; }

  void Start() {
    PowerNetwork.Instance?.RegisterGenerator(this);
    if (IsActive)
      PowerNetwork.Instance?.Evaluate();
  }

  void OnDestroy() { PowerNetwork.Instance?.UnregisterGenerator(this); }

  public void Interact() {
    if (IsActive)
      return;
    puzzleUI?.StartPuzzle(OnPuzzleSolved);
  }

  void OnPuzzleSolved() {
    IsActive = true;
    PowerNetwork.Instance?.Evaluate();
    // TODO: эффекты запуска (звук, искры, встряска)
  }

  public string PromptText =>
      IsActive ? "Генератор активен" : "[E] Настроить генератор";
}
