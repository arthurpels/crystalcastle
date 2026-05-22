using UnityEngine;

public class CrystalInteractable : MonoBehaviour, IInteractable {
    public string PromptText {
        get {
            return "Подчиниться кристаллу";
        }
    }

    public void Interact() {
        GameEnding.Instance.Trigger(GameEnding.EndingType.Dream); // Сон/Подчинение
    }
}