using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private CameraControlsActions _cameraControlsActions;
    private InputAction _movement;
    private Transform _cameraTransform;

    [Header("Horizontal Motion")] [SerializeField]
    private float maxSpeed = 5f;

    private float speed;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float damping = 15f;

    [Header("Vertical Motion - Zooming")] [SerializeField]
    private float stepSize = 2f;

    [SerializeField] private float zoomDampening = 7.5f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomHeight;

    [Header("Rotation")] [SerializeField] private float maxRotationSpeed = 1f;

    [Header("Screen edge motion")] [Range(0f, 0.1f)] [SerializeField]
    private float edgeTolerance = 0.05f;

    [SerializeField] private bool useScreenEdge = true;

    private Vector3 _targetPosition;
    private Vector3 _horizontalVelocity;
    private Vector3 _lastPosition;
    private Vector3 _startDrag;

    private void Awake()
    {
        _cameraControlsActions = new CameraControlsActions();
        _cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    private void OnEnable()
    {
        zoomHeight = _cameraTransform.localPosition.y;
        _cameraTransform.LookAt(transform);
        _lastPosition = transform.position;
        _movement = _cameraControlsActions.Camera.Movement;
        _cameraControlsActions.Camera.RotateCamera.performed += RotateCamera;
        _cameraControlsActions.Camera.ZoomCamera.performed += ZoomCamera;
        _cameraControlsActions.Camera.Enable();
    }

    private void OnDisable()
    {
        _cameraControlsActions.Camera.RotateCamera.performed -= RotateCamera;
        _cameraControlsActions.Camera.ZoomCamera.performed -= ZoomCamera;
        _cameraControlsActions.Camera.Disable();
    }

    private void Update()
    {
        GetKeyboardMovement();
        CheckMouseAtScreenEdge();
        DragCamera();
        UpdateVelocity();
        UpdateCameraPosition();
        UpdateBasePosition();
    }

    private void UpdateVelocity()
    {
        _horizontalVelocity = (transform.position - _lastPosition) / Time.deltaTime;
        _horizontalVelocity.y = 0;
        _lastPosition = transform.position;
    }

    private void GetKeyboardMovement()
    {
        Vector3 inputValue = _movement.ReadValue<Vector2>().x * GetCameraRight() +
                             _movement.ReadValue<Vector2>().y * GetCameraForward();

        inputValue = inputValue.normalized;

        if (inputValue.sqrMagnitude > 0.01f)
        {
            _targetPosition += inputValue;
        }
    }

    private Vector3 GetCameraRight()
    {
        Vector3 right = _cameraTransform.right;
        right.y = 0;
        return right;
    }

    private Vector3 GetCameraForward()
    {
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0;
        return forward;
    }

    private void UpdateBasePosition()
    {
        if (_targetPosition.sqrMagnitude > 0.01f)
        {
            speed = Mathf.Lerp(speed, maxSpeed, Time.deltaTime * acceleration);
            transform.position += _targetPosition * speed * Time.deltaTime;
        }
        else
        {
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, Vector3.zero, Time.deltaTime * damping);
            transform.position += _horizontalVelocity * Time.deltaTime;
        }

        _targetPosition = Vector3.zero;
    }

    private void RotateCamera(InputAction.CallbackContext inputValue)
    {
        if (!Mouse.current.middleButton.isPressed)
            return;

        float value = inputValue.ReadValue<Vector2>().x;
        transform.rotation = Quaternion.Euler(0f, value * maxRotationSpeed + transform.rotation.eulerAngles.y, 0f);
    }

    private void ZoomCamera(InputAction.CallbackContext inputValue)
    {
        float value = -inputValue.ReadValue<Vector2>().y / 100f;

        if (Math.Abs(value) > 0.1f)
        {
            zoomHeight = _cameraTransform.localPosition.y + value * stepSize;
            if (zoomHeight < minHeight)
                zoomHeight = minHeight;
            else if (zoomHeight > maxHeight)
                zoomHeight = maxHeight;
        }
    }

    private void UpdateCameraPosition()
    {
        var cameraTransformLocalPosition = _cameraTransform.localPosition;
        
        Vector3 zoomTarget = new Vector3(cameraTransformLocalPosition.x, zoomHeight, cameraTransformLocalPosition.z);
        zoomTarget -= zoomSpeed * (zoomHeight - cameraTransformLocalPosition.y) * Vector3.forward;
        _cameraTransform.localPosition =
            Vector3.Lerp(cameraTransformLocalPosition, zoomTarget, Time.deltaTime * zoomDampening);
        _cameraTransform.LookAt(transform);
    }

    private void CheckMouseAtScreenEdge()
    {
        if (!useScreenEdge)
            return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;

        if (mousePosition.x < edgeTolerance * Screen.width)
        {
            moveDirection += -GetCameraRight();
        }
        else if (mousePosition.x > (1f  - edgeTolerance) * Screen.width)
        {
            moveDirection += GetCameraRight();
        }
        
        if (mousePosition.y < edgeTolerance * Screen.height)
        {
            moveDirection += -GetCameraForward();
        }
        else if (mousePosition.y > (1f  - edgeTolerance) * Screen.height)
        {
            moveDirection += GetCameraForward();
        }

        _targetPosition += moveDirection;
    }

    private void DragCamera()
    {
        if (!Mouse.current.rightButton.isPressed)
            return;

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out float distance))
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
                _startDrag = ray.GetPoint(distance);
            else
                _targetPosition += _startDrag - ray.GetPoint(distance);
        }
    }
}