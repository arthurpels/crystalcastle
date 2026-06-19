using UnityEngine;

/// <summary>
/// Стенд вокруг кристалла. Принимает только теслу. Путь «вознесение»:
/// когда все стенды заполнены и запитаны → AND-gate → AscensionController.
/// </summary>
public class TeslaStand : ItemSocket
{
    [Header("Tesla")]
    [SerializeField] private ItemData   teslaItem;
    [SerializeField] private GameObject teslaPlacedPrefab;

    protected override ItemData   AcceptedItem => teslaItem;
    protected override GameObject PlacedPrefab => teslaPlacedPrefab;
}
