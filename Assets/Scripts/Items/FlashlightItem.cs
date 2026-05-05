using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightItem : BaseItem {
    [Header("Settings")]
    public Light lightComp;
    public AudioClip clickSound;
    private AudioSource audioSource;

    public bool isOn;

    [Header("Flicker")]
    public float flickerIntensity = 0.4f;

    private float _flickerTimer;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        if (visualModel) visualModel.SetActive(false);
    }

    public override void OnEquip(ItemSlot slot) {
        isOn = true;
        lightComp.enabled = true;
        PlayClick();
    }

    public override void OnUnequip(ItemSlot slot) {
        isOn = false;
        lightComp.enabled = false;
    }

    public override void OnUse(ItemSlot slot) {
        isOn = !isOn;
        lightComp.enabled = isOn;
        PlayClick();
    }

    public override void OnTick(ItemSlot slot, float dt) {
        if (!isOn) return;

        _flickerTimer -= dt;
        if (_flickerTimer <= 0f) {
            _flickerTimer = Random.Range(0.05f, 0.3f);
            // lightComp.intensity = Random.Range(5f - flickerIntensity, 5f + flickerIntensity * 0.5f);
            if (Random.Range(0, 100) < 10) {
                lightComp.intensity = Random.Range(2f - flickerIntensity, 2f + flickerIntensity * 0.5f);
            } else {
                lightComp.intensity = Random.Range(5f - flickerIntensity, 5f + flickerIntensity * 0.5f);
            }
            
        }

    }

    private void PlayClick() {
        if (audioSource && clickSound) audioSource.PlayOneShot(clickSound);
    }

}
