using UnityEngine;

[RequireComponent(typeof(WorldItem))]
public class WorldItemAdapter : MonoBehaviour, IInteractable {
    private WorldItem _worldItem;

    public string PromptText => "Поднять";
    public float InteractionPriority => 1f;

    private void Awake() => _worldItem = GetComponent<WorldItem>();


    public void Interact() {
        FindObjectOfType<PlayerInventory>().PickupItem(_worldItem);
    }
}