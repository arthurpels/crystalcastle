using UnityEngine;

/// <summary>
/// Станина для взрывчатки. Принимает только заряд. Путь «уничтожение»:
/// заряды ставятся инертно, взвод и таймер — отдельным действием на терминале
/// (точка невозврата там, не при установке).
/// </summary>
public class BombMount : ItemSocket
{
    [Header("Explosive")]
    [SerializeField] private ItemData   explosiveItem;
    [SerializeField] private GameObject explosivePlacedPrefab;

    protected override ItemData   AcceptedItem => explosiveItem;
    protected override GameObject PlacedPrefab => explosivePlacedPrefab;
}
