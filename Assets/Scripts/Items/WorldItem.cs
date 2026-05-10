using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour
{
    public ItemData data;

    private void Reset()
    {
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = true;
            col.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
    }

    // Просто уничтожает себя. ВСЮ логику подбора делает инвентарь.
    public void Pickup() => Destroy(gameObject);
}