using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable
{
    public ItemData data;

    private void Reset()
    {
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = false;
            col.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
    }

    public void Pickup() => Destroy(gameObject);
    public string PromptText => "Поднять";
    public void Interact() {
        FindObjectOfType<PlayerInventory>().PickupItem(this);
    }
}