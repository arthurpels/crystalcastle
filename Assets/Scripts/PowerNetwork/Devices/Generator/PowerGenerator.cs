using UnityEngine;

public class PowerGenerator : MonoBehaviour, IInteractable, ISaveable {
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
      IsActive ? "Генератор активен" : "Настроить генератор";

  // ── ISaveable ──────────────────────────────────────────────────────────

  [System.Serializable]
  private struct GeneratorSaveData { public bool isActive; }

  public string CaptureState() =>
      JsonUtility.ToJson(new GeneratorSaveData { isActive = IsActive });

  public void RestoreState(string json)
  {
      var d   = JsonUtility.FromJson<GeneratorSaveData>(json);
      IsActive = d.isActive;
      // PowerNetwork.Evaluate() вызовет SaveManager один раз в конце загрузки.
  }
}
