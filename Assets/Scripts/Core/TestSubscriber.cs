using UnityEngine;

public class TestSubscriber : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.Instance.OnSectorPoweredOn += HandleSectorOn;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSectorPoweredOn -= HandleSectorOn;
        }
    }

    private void HandleSectorOn(SectorID sector)
    {
        Debug.Log($"{sector} powered: True");
    }
}

