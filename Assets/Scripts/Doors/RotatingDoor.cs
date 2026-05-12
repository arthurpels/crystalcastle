using UnityEngine;

public class RotatingDoor : Door
{
    [Header("Rotation")]
    [SerializeField] private Transform pivot; // Точка вращения (петля)
    [SerializeField] private Vector3 openAngle = new Vector3(0, 90, 0); // Угол открытия
    [SerializeField] private float speed = 180f; // Градусов в секунду
    
    private Quaternion closedRotation;
    private Quaternion targetRotation;
    
    private void Awake()
    {
        if (pivot == null) pivot = transform;
        closedRotation = pivot.localRotation;
        targetRotation = closedRotation;
    }
    
    protected override void OnStateChanged()
    {
        targetRotation = isOpen 
            ? Quaternion.Euler(openAngle) * closedRotation 
            : closedRotation;
    }
    
    private void Update()
    {
        if (pivot.localRotation != targetRotation)
        {
            pivot.localRotation = Quaternion.RotateTowards(
                pivot.localRotation, 
                targetRotation, 
                speed * Time.deltaTime
            );
        }
    }
}