using System;
using System.Collections.Generic;
using UnityEngine;

public enum SectorID
{
    SectorA,
    SectorB,
    SectorC,
    ReactorCore
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<SectorID> OnSectorPoweredOn;
    public event Action<SectorID> OnSectorPoweredOff;
    public event Action<SectorID, bool> OnSectorPowerChanged;

    private Dictionary<SectorID, bool> _sectorPower = new Dictionary<SectorID, bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSectors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSectors()
    {
        _sectorPower.Add(SectorID.SectorA, false);
        _sectorPower.Add(SectorID.SectorB, false);
        _sectorPower.Add(SectorID.SectorC, false);
        _sectorPower.Add(SectorID.ReactorCore, false);
    }

    public bool IsSectorPowered(SectorID sector)
    {
        if (_sectorPower.TryGetValue(sector, out bool isPowered))
        {
            return isPowered;
        }
        return false; 
    }

    public void SetSectorPower(SectorID sector, bool isPowered)
    {
        if (_sectorPower[sector] == isPowered)
        {
            return; 
        }

        _sectorPower[sector] = isPowered;

        if (isPowered)
        {
            OnSectorPoweredOn?.Invoke(sector);
        }
        else
        {
            OnSectorPoweredOff?.Invoke(sector);
        }

        OnSectorPowerChanged?.Invoke(sector, isPowered);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetSectorPower(SectorID.SectorA, true);
        }
    }
}