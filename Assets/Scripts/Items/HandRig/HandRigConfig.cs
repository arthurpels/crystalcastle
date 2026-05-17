using System;
using UnityEngine;

[Serializable]
public class HandRigConfig
{
    public enum RigBehavior { None, AimAtCamera, PlayAnimation, Custom }
    
    public RigBehavior behavior = RigBehavior.None;
    
    [Header("All")]
    public float blendInSpeed = 5f;
    public float blendOutSpeed = 3f;
    
    [Header("AimAtCamera")]
    public float AimWeight = 0.8f;
    public float maxAngle = 90f;
    
    [Header("PlayAnimation")]
    public string triggerParam = "";     // Animator.SetTrigger
    public string boolParam = "";        // Animator.SetBool
    public float animationBlendSpeed = 4f;
}