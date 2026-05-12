using UnityEngine;

public class SlidingDoor : Door
{
    [Header("Sliding")]
    [SerializeField] private Transform mover; // Объект, который двигается
    [SerializeField] private Vector3 openOffset = new Vector3(0, 0, 2); // Смещение
    [SerializeField] private float speed = 2f; // Единиц в секунду
    
    private Vector3 closedPosition;
    private Vector3 targetPosition;
    
    private void Awake()
    {
        if (mover == null) mover = transform;
        closedPosition = mover.localPosition;
        targetPosition = closedPosition;
    }
    
    protected override void OnStateChanged()
    {
        // Вычисляем целевую позицию
        targetPosition = isOpen 
            ? closedPosition + openOffset 
            : closedPosition;
    }
    
    private void Update()
    {
        // Плавное движение
        if (mover.localPosition != targetPosition)
        {
            mover.localPosition = Vector3.MoveTowards(
                mover.localPosition, 
                targetPosition, 
                speed * Time.deltaTime
            );
        }
    }
}