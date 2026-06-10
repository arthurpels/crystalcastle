using UnityEngine;

public class PowerableLight : MonoBehaviour, IPowerable
{
    public bool IsPowered { get; private set; }

    [SerializeField] private GameObject targetLightGO;
    [SerializeField] private AudioSource humSource;
    [SerializeField] private ParticleSystem steamParticles;

    public void OnPowerChanged(bool powered, bool force = false)
    {
        if (IsPowered == powered && !force) return;
        IsPowered = powered;

        if (targetLightGO != null) 
            targetLightGO.SetActive(powered);

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