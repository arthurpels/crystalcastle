using UnityEngine;

// Логический элемент AND: выходная нода получает питание только если
// ВСЕ входные ноды запитаны. Обрабатывается в PowerNetwork.Evaluate().
public class PowerAndGate : MonoBehaviour
{
    [SerializeField] private PowerNode[] inputs;
    [SerializeField] private PowerNode   output;

    public PowerNode[] Inputs => inputs;
    public PowerNode   Output  => output;
}
