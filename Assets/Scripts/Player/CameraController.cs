using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class CameraController : MonoBehaviour {
    [Header("Настройки")]
    [SerializeField] private GameObject cameraTarget;
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float minVerticalAngle = -60f;
    [SerializeField] private float maxVerticalAngle = 70f;
    [SerializeField] private bool invertY = false;

    private float _yaw;   // Горизонталь (мировая ось Y)
    private float _pitch; // Вертикаль (локальная ось X)

    private PlayerInputHandler _inputHandler;

    private void Awake() {
        _inputHandler = GetComponent<PlayerInputHandler>();
        Debug.Assert(cameraTarget != null, $"[{name}] CameraTarget not assigned!", this);

        // Инициализация
        var euler = cameraTarget.transform.rotation.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
    }

    private void LateUpdate() {
        if (!_inputHandler.InputEnabled) return;

        Vector2 lookInput = Mouse.current.delta.ReadValue();

        if (lookInput.sqrMagnitude >= 0.01f) {

            _yaw += lookInput.x * mouseSensitivity;
            _pitch -= lookInput.y * mouseSensitivity * (invertY ? -1f : 1f);
        }

        _yaw = ClampAngle(_yaw, float.MinValue, float.MaxValue);
        _pitch = ClampAngle(_pitch, minVerticalAngle, maxVerticalAngle);

        cameraTarget.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}