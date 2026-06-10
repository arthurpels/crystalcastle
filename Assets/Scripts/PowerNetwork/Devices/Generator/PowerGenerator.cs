using UnityEngine;

public class PowerGenerator : MonoBehaviour, IInteractable, ISaveable, IPowerSource {
  [Header("Grid")]
  [SerializeField]
  private PowerNode outputNode;
  public PowerNode OutputNode => outputNode;

  [Header("Puzzle")]
  [SerializeField]
  private SineWavePuzzleUI puzzleUI;

  [Header("Звуки")]
  [SerializeField] private AudioClip startSound;
  [SerializeField] private AudioClip loopSound;
  [SerializeField] [Range(0f,1f)] private float volume = 0.8f;

  private AudioSource _loopSource;

  public bool IsActive { get; private set; }

  void Awake() {
    _loopSource = gameObject.AddComponent<AudioSource>();
    _loopSource.spatialBlend = 1f;
    _loopSource.loop        = true;
    _loopSource.playOnAwake = false;
    _loopSource.volume      = volume;
    if (loopSound != null) _loopSource.clip = loopSound;
  }

  void Start() {
    PowerNetwork.Instance?.RegisterGenerator(this);
    if (IsActive) {
      PowerNetwork.Instance?.Evaluate();
      if (_loopSource != null && loopSound != null) _loopSource.Play();
    }
  }

  void OnDestroy() { PowerNetwork.Instance?.UnregisterGenerator(this); }

  public void Interact() {
    if (IsActive)
      return;
    if (puzzleUI != null)
      puzzleUI.StartPuzzle(OnPuzzleSolved);
    else
      Debug.Log("puzzleUI == null");
  }

  void OnPuzzleSolved() {
    IsActive = true;
    PowerNetwork.Instance?.Evaluate();

    if (startSound != null)
      AudioSource.PlayClipAtPoint(startSound, transform.position, volume);

    if (_loopSource != null && loopSound != null)
      _loopSource.Play();

    if (CameraShake.Instance != null)
      CameraShake.Instance.Shake(0.18f, 0.6f);
  }

  public string PromptText =>
      IsActive ? "Генератор активен" : "Настроить генератор";

  // ── ISaveable ──────────────────────────────────────────────────────────

  [System.Serializable]
  private struct GeneratorSaveData { public bool isActive; }

  public string CaptureState() =>
      JsonUtility.ToJson(new GeneratorSaveData { isActive = IsActive });

  public void RestoreState(string json) {
    var d = JsonUtility.FromJson<GeneratorSaveData>(json);
    IsActive = d.isActive;
    // PowerNetwork.Evaluate() вызовет SaveManager один раз в конце загрузки.
  }
}
