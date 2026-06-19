using System.Collections.Generic;
using UnityEngine;

// Логический элемент AND: выходная нода получает питание только если
// ВСЕ входные ноды запитаны. Обрабатывается в PowerNetwork.Evaluate().
public class PowerAndGate : MonoBehaviour, IPowerGate
{
    [SerializeField] private PowerNode[] inputs;
    [SerializeField] private PowerNode   output;

    public PowerNode[] Inputs => inputs;
    public PowerNode   Output  => output;

    public bool Compute(HashSet<PowerNode> powered)
    {
        if (inputs == null || inputs.Length == 0) return false;
        foreach (var i in inputs)
            if (i == null || !powered.Contains(i)) return false;
        return true;
    }
}
