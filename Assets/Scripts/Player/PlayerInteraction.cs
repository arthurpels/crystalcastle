using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Точка в руке персонажа")]
    public Transform handPoint; 
    
    private GameObject currentItem;
    private bool isNearCrowbar = false;
    private GameObject crowbarNearby;

    void Update()
    {
        CheckForCrowbar();

        if (isNearCrowbar && currentItem == null && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            PickUpItem();
        }

        if (currentItem != null && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            DropItem();
        }
    }

    private void CheckForCrowbar()
    {
        isNearCrowbar = false;
        crowbarNearby = null;

        Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Crowbar"))
            {
                isNearCrowbar = true;
                crowbarNearby = col.gameObject;
                break;
            }
        }
    }

    private void PickUpItem()
    {
        if (crowbarNearby != null)
        {
            currentItem = crowbarNearby;
            
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            currentItem.transform.SetParent(handPoint);
            currentItem.transform.localPosition = Vector3.zero;
            currentItem.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            // НОВАЯ СТРОЧКА: Возвращаем нормальный масштаб в руке
            currentItem.transform.localScale = Vector3.one; 

            isNearCrowbar = false; 
        }
    }

    private void DropItem()
    {
        currentItem.transform.SetParent(null);

        Collider col = currentItem.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = currentItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        // НОВАЯ СТРОЧКА: Возвращаем нормальный масштаб на земле
        currentItem.transform.localScale = Vector3.one;

        currentItem = null;
    }

    void OnGUI()
    {
        if (isNearCrowbar && currentItem == null) 
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 50), "Нажми [F] чтобы взять лом", style);
        }
    }
}