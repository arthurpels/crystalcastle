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
  [Tooltip("3D-источник на генераторе. Назначь компонент AudioSource с этого объекта — " +
           "дальность/затухание настраиваются прямо в нём (3D Sound Settings).")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip startSound;
  [SerializeField] private AudioClip loopSound;
  [SerializeField] [Range(0f,1f)] private float volume = 0.8f;

  public bool IsActive { get; private set; }

  void Awake() {
    // Источник назначается в инспекторе (там же настраивается дальность).
    // Фолбэк: берём/создаём на этом объекте, если поле пустое.
    if (audioSource == null) audioSource = GetComponent<AudioSource>();
    if (audioSource == null) {
      audioSource = gameObject.AddComponent<AudioSource>();
      audioSource.spatialBlend = 1f; // авто-созданному задаём 3D
    }
    audioSource.playOnAwake = false;
    audioSource.loop        = true;
    audioSource.volume      = volume;
    if (loopSound != null) audioSource.clip = loopSound;
  }

  void Start() {
    PowerNetwork.Instance?.RegisterGenerator(this);
    if (IsActive) {
      PowerNetwork.Instance?.Evaluate();
      if (audioSource != null && loopSound != null) audioSource.Play();
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

    // Нарративная метка — пример. NarrativeTrigger с eventId="generator_started"
    // поймает это и покажет подсказку через HintUI.
    GameEventLog.Instance?.Raise("generator_started");

    if (audioSource != null) {
      // Стартовый звук — разовый поверх лупа, из того же 3D-источника
      // (значит, тоже затухает по настроенной дальности).
      if (startSound != null) audioSource.PlayOneShot(startSound);
      if (loopSound != null)  audioSource.Play();
    }

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
