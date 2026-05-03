using UnityEngine;

public class ItemSlot : MonoBehaviour
{

    [Tooltip("Пустой объект внутри кости руки, куда крепится предмет")]
    public Transform holdPoint;

    public BaseItem CurrentItem { get; private set; }

    public void Equip(BaseItem itemPrefab)
    {
        if (CurrentItem != null) Unequip();

        // Создаём предмет в руке
        CurrentItem = Instantiate(itemPrefab, holdPoint);
        CurrentItem.transform.localPosition = CurrentItem.localOffset;
        CurrentItem.transform.localEulerAngles = CurrentItem.localRotation;
        CurrentItem.visualModel?.SetActive(true);

        CurrentItem.OnEquip(this);
    }

    public void Unequip()
    {
        if (CurrentItem == null) return;
        CurrentItem.OnUnequip(this);
        Destroy(CurrentItem.gameObject);
        CurrentItem = null;
    }

    public void Use() => CurrentItem?.OnUse(this);
    public void Tick(float dt) => CurrentItem?.OnTick(this, dt);
}