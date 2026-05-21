using UnityEngine;

/// <summary>
/// Кладётся на GameObject в сцене — документ, с которым взаимодействует игрок.
/// Реализует IInteractable: InteractionManager видит объект, показывает prompt,
/// игрок жмёт E → открывается DocumentReaderUI.
///
/// После первого прочтения:
///  — Collider отключается (prompt исчезает, объект остаётся декорацией).
///  — Audio-кассеты дополнительно деактивируются целиком.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DocumentWorldObject : MonoBehaviour, IInteractable, ISaveable
{
    [SerializeField] private DocumentData data;

    private Collider _col;
    private bool _hasBeenRead;

    private void Awake() => _col = GetComponent<Collider>();

    // ── IInteractable ────────────────────────────────────────────────────────

    public string PromptText => (data != null && !_hasBeenRead) ? data.interactPrompt : null;

    public void Interact()
    {
        if (data == null) return;
        if (DocumentManager.Instance == null)
        {
            Debug.LogWarning("[DocumentWorldObject] DocumentManager не найден в сцене!", this);
            return;
        }

        DocumentManager.Instance.Read(data);

        if (!_hasBeenRead) MarkRead();
    }

    private void MarkRead()
    {
        _hasBeenRead = true;
        if (_col != null) _col.enabled = false;
        if (data.type == DocumentData.DocumentType.Audio)
            gameObject.SetActive(false);
    }

    // ── ISaveable ──────────────────────────────────────────────────────────

    [System.Serializable]
    private struct DocSaveData { public bool hasBeenRead; }

    public string CaptureState() =>
        JsonUtility.ToJson(new DocSaveData { hasBeenRead = _hasBeenRead });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<DocSaveData>(json);
        if (d.hasBeenRead && !_hasBeenRead)
        {
            _hasBeenRead = true;
            if (_col != null) _col.enabled = false;
            if (data != null && data.type == DocumentData.DocumentType.Audio)
                gameObject.SetActive(false);

            // Восстанавливаем архив документов тихо (без UI)
            if (data != null) DocumentManager.Instance?.AddToArchiveQuiet(data);
        }
    }

    /// <summary>Используется SaveManager для поиска DocumentData по id.</summary>
    public DocumentData GetDocumentData() => data;
}
