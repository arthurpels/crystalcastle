using UnityEngine;

/// <summary>
/// ScriptableObject — данные одного внутриигрового документа.
/// Создавать: Assets → CrystalCastle → Document.
/// </summary>
[CreateAssetMenu(fileName = "NewDocument", menuName = "CrystalCastle/Document")]
public class DocumentData : ScriptableObject
{
    public enum DocumentType
    {
        Paper,      // записка / журнал — полноэкранный оверлей
        Audio,      // аудиокассета — воспроизводится фоном
        MultiPart   // составной документ (дневник Горина и т.п.)
    }

    [Header("Идентификация")]
    [Tooltip("Уникальный ID документа. Никогда не меняй после создания!")]
    public string id;

    [Tooltip("Отображается в архиве и заголовке UI")]
    public string title;

    [Header("Содержимое")]
    [TextArea(4, 25)]
    [Tooltip("Полный текст. Поддерживает \\n. Оставь пустым для Audio-типа.")]
    public string body;

    public DocumentType type = DocumentType.Paper;

    [Header("Составной документ (MultiPart)")]
    [Tooltip("Общий ID группы. У всех частей одинаковый, например \"gorin_diary\"")]
    public string groupId;

    [Tooltip("Номер части: 0, 1, 2 ...")]
    public int partIndex;

    [Tooltip("Всего частей в группе")]
    public int totalParts = 1;

    [Header("Аудио")]
    [Tooltip("Только для Audio-типа — голосовая запись")]
    public AudioClip audioClip;

    [Tooltip("Фоновый звук во время чтения (шелест бумаги и т.п.)")]
    public AudioClip ambientOnRead;

    [Header("Взаимодействие")]
    [Tooltip("Текст подсказки: «Читать», «Поднять кассету» ...")]
    public string interactPrompt = "Читать";
}
