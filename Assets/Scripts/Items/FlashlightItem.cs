using UnityEngine;

public class FlashlightHandItem : HandItem {
    [Header("Components")]
    public Light lightComp;
    public AudioClip clickSound;

    public float flickerIntensity = 0.4f;

    // [Header("Rig")]
    // public HandRigConfig rigConfig = new HandRigConfig {
    //     behavior = HandRigConfig.RigBehavior.AimAtCamera,
    //     AimWeight = 0.8f,
    //     blendInSpeed = 4f,
    //     blendOutSpeed = 2f
    // };

    public bool IsOn { get; private set; }
    private float _flickerTimer;
    private AudioSource _audio;

    private void Awake() {
        _audio = GetComponent<AudioSource>();
        if (lightComp) lightComp.enabled = false;
    }

    public override void OnEquip() {
        ToggleLight();
    }

    public override void OnUnequip() {
        TurnLightOff();
    }

    public override void OnUse() => ToggleLight();

    public override void OnTick(float dt) {
        if (!IsOn) return;

        _flickerTimer -= dt;
        if (_flickerTimer <= 0f) {
            _flickerTimer = Random.Range(0.05f, 0.2f);
            lightComp.intensity = Random.Range(5f - flickerIntensity, 5f + flickerIntensity * 0.5f);
        }
    }

    private void ToggleLight() {
        IsOn = !IsOn;
        lightComp.enabled = IsOn;
        if (_audio && clickSound) _audio.PlayOneShot(clickSound);
    }

    private void TurnLightOff() {
        IsOn = false;
        lightComp.enabled = false;
    }
}