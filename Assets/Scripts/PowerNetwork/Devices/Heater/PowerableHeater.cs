using UnityEngine;

public class PowerableHeater : MonoBehaviour, IPowerable
{
    public bool IsPowered { get; private set; }
    public bool IsHeating => IsPowered;

    [Header("FX")]
    [SerializeField] private AudioSource humSource;
    [SerializeField] private ParticleSystem steamParticles;
    [SerializeField] private GameObject glowLight; // <— Компонент Light, не GameObject

    public event System.Action<bool> OnHeaterStateChanged;

    public void OnPowerChanged(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        if (humSource != null) {
            if (powered) humSource.Play();
            else humSource.Stop();
        }

        if (steamParticles != null) {
            if (powered) steamParticles.Play();
            else steamParticles.Stop();
        }

        if (glowLight != null) 
          glowLight.SetActive(powered);

        OnHeaterStateChanged?.Invoke(powered);
    }
}