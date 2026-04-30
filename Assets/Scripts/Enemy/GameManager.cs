using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum SectorID { A, B, None }
    public event Action<SectorID> OnSectorPoweredOn;

    [Header("Auto Test (для проверки)")]
    public bool AutoTestOnStart = true;
    public SectorID TestSector = SectorID.B;
    public float TestDelay = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (AutoTestOnStart)
        {
            Invoke(nameof(TriggerTestPower), TestDelay);
        }
    }

    private void TriggerTestPower()
    {
        Debug.Log($"[GameManager]  Включаем питание сектора: {TestSector}");
        OnSectorPoweredOn?.Invoke(TestSector);
    }

    public void SetSectorPower(SectorID sector, bool isPowered)
    {
        if (isPowered)
        {
            Debug.Log($"[GameManager]  ВКЛЮЧАЕМ питание сектора: {sector}");
            OnSectorPoweredOn?.Invoke(sector);
        }
        else
        {
            Debug.Log($"[GameManager]  ВЫКЛЮЧАЕМ питание сектора: {sector}");
        }
    }

    public void PowerSectorA()
    {
        SetSectorPower(SectorID.A, true);
    }
}