using System.Collections.Generic;

/// <summary>
/// Логический элемент сети: по состоянию входных нод решает, запитывать ли выход.
/// Обрабатывается в PowerNetwork.Evaluate() (пересчёт до стабилизации, поэтому
/// допустимы немонотонные элементы — XOR, чекер последовательности).
/// </summary>
public interface IPowerGate
{
    /// <summary>Выходная нода (запитывается, если Compute вернул true).</summary>
    PowerNode Output { get; }

    /// <summary>Должен ли выход быть под напряжением при данном множестве запитанных нод.</summary>
    bool Compute(HashSet<PowerNode> powered);
}
