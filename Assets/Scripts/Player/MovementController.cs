using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour {
    [Header("Камера")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Базовые значения")]
    [SerializeField] private float baseMoveSpeed = 4.0f;
    [SerializeField] private float baseSprintSpeed = 7.0f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Заземление")]
    [SerializeField] private float groundedOffset = -0.14f;
    [SerializeField] private float groundedRadius = 0.3f;
    [SerializeField] private LayerMask groundLayers;


    // [Header("Ссылки")]
    public bool InputEnabled { get; set; } = true;
    public bool IsSprinting { get; set; }

    private CharacterController controller;
    private Vector2 currentInput;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private float rotationSmoothVelocity;


    //Хук для системы поверхностей/модификаторов
    public float SpeedMultiplier { get; set; } = 1f;
    public float GripMultiplier { get; set; } = 1f;



    void Start() {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }


    void Update() {
        if (!InputEnabled) return;

        HandleGrounding();
        HandleGravity();
        HandleMovement();
    }

    private void HandleGrounding() {
        
    }
    private void HandleGravity() {
        
    }

    private void HandleMovement() {
        
    }
}
