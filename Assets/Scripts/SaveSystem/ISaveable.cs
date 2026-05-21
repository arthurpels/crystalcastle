/// <summary>
/// Реализуй этот интерфейс на MonoBehaviour, чтобы SaveManager сохранял
/// и восстанавливал состояние объекта.
///
/// На том же GameObject должен быть компонент SaveableIdentity,
/// иначе SaveManager не сможет идентифицировать объект.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Сериализуй текущее состояние и верни JSON-строку.
    /// Вызывается SaveManager при Save().
    /// </summary>
    string CaptureState();

    /// <summary>
    /// Распарси JSON-строку и примени сохранённое состояние.
    /// Вызывается SaveManager после загрузки сцены.
    /// НЕ запускай PowerNetwork.Evaluate() здесь — SaveManager делает это
    /// один раз в конце, когда все объекты уже восстановлены.
    /// </summary>
    void RestoreState(string json);
}
