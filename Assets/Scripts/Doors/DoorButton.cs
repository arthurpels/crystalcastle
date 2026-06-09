using UnityEngine;

public class DoorButton : MonoBehaviour, IInteractable
{
    [SerializeField] private DoorPowerAdapter[] doors;

    public void Interact()
    {
        foreach (var door in doors)
            if (door != null) door.TryToggle();
    }

    public string PromptText
    {
        get
        {
            if (doors.Length == 0 || doors[0] == null) return "Кнопка";
            if (!doors[0].IsPowered) return "Нет питания";
            return doors[0].IsDoorOpen ? "Закрыть дверь" : "Открыть дверь";
        }
    }
}
