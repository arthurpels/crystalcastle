using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Реагирует на событие из GameEventLog. Реакция настраивается в инспекторе
/// через UnityEvent — например, HintUI.ShowHint("Осмотрите провода вокруг").
///
/// Подписан на СТАТИЧЕСКОЕ GameEventLog.OnEvent, поэтому не зависит от порядка
/// инициализации (Instance может ещё не существовать на момент OnEnable).
/// </summary>
public class NarrativeTrigger : MonoBehaviour
{
    [Tooltip("ID события, на которое реагируем (тот же, что в Raise).")]
    [SerializeField] private string eventId;

    [Tooltip("Сработать только когда событие произошло ВПЕРВЫЕ.")]
    [SerializeField] private bool onlyFirstTime = true;

    [SerializeField] private UnityEvent response;

    void OnEnable()  => GameEventLog.OnEvent += Handle;
    void OnDisable() => GameEventLog.OnEvent -= Handle;

    private void Handle(string id, bool isNew)
    {
        if (id != eventId) return;
        if (onlyFirstTime && !isNew) return;
        response?.Invoke();
    }
}
