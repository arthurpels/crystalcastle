using UnityEngine;

public class PowerableLight : MonoBehaviour, IPowerable
{
    public bool IsPowered { get; private set; }

    [SerializeField] private Light targetLight;
    [SerializeField] private AudioSource humSource;
    [SerializeField] private ParticleSystem steamParticles;

    public void OnPowerChanged(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        if (targetLight != null) targetLight.enabled = powered;

        if (humSource != null)
        {
            if (powered) humSource.Play();
            else humSource.Stop();
        }

        if (steamParticles != null)
        {
            if (powered) steamParticles.Play();
            else steamParticles.Stop();
        }
    }
}