using UnityEngine;

/// <summary>
/// Крутящаяся аварийная мигалка. Вращает «ротор» (с дочерним спотлайтом —
/// тогда конус света подметает помещение) и опц. пульсирует яркостью.
/// Расставляешь по уровню выключенными — CountdownTimer включает их на старте.
/// </summary>
public class WarningLight : MonoBehaviour
{
    [Header("Вращение")]
    [Tooltip("Что крутить (повесь сюда спотлайт/мигающую часть). Пусто = этот объект.")]
    [SerializeField] private Transform rotor;
    [SerializeField] private float rotateSpeed = 220f;   // град/сек
    [SerializeField] private Vector3 axis = Vector3.up;

    [Header("Пульс света (опц.)")]
    [SerializeField] private Light lightSource;
    [SerializeField] private bool   pulse = true;
    [SerializeField] private float  pulseSpeed = 5f;
    [SerializeField] private float  minIntensity = 0.3f;
    [SerializeField] private float  maxIntensity = 3f;

    [Header("Звук (опц.)")]
    [Tooltip("Зацикленный гул/вой — играет, пока объект активен.")]
    [SerializeField] private AudioSource hum;

    private void Reset() => rotor = transform;

    private void Awake()
    {
        if (rotor == null) rotor = transform;
    }

    private void OnEnable()
    {
        if (hum != null && !hum.isPlaying) hum.Play();
    }

    private void OnDisable()
    {
        if (hum != null) hum.Stop();
    }

    private void Update()
    {
        rotor.Rotate(axis, rotateSpeed * Time.deltaTime, Space.Self);

        if (pulse && lightSource != null)
        {
            float k = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, k);
        }
    }
}
