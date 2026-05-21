using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Добавляй на каждый GameObject, который должен сохраняться.
/// GUID генерируется автоматически при добавлении компонента в редакторе
/// и хранится в файле сцены — никогда не меняется при перезапуске.
///
/// Порядок работы:
///   1. Добавь компонент в сцене — ID генерируется в OnValidate.
///   2. Сохрани сцену — ID записывается в .unity.
///   3. SaveManager читает его при Save/Load для идентификации объекта.
/// </summary>
[DisallowMultipleComponent]
public class SaveableIdentity : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string _id;

    public string Id => _id;

#if UNITY_EDITOR
    // В редакторе: генерируется при добавлении или если поле пустое.
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_id))
        {
            _id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
    }
#endif

    // В билде: страховка для runtime-созданных объектов.
    private void Awake()
    {
        if (string.IsNullOrEmpty(_id))
            _id = System.Guid.NewGuid().ToString();
    }
}
