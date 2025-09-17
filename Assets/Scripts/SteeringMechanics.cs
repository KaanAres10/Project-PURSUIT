using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringMechanics : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActions;

    [Header("Movement Settings")]
    public float speed = 6.0f;
    public float gravity = 20.0f;
    public float rotationSpeed = 100f;

    [Header("Drift Settings")]
    public float driftIntensity = 2.0f;
    public float driftSteerMultiplier = 2.0f;
    public KeyCode driftKey = KeyCode.Space;

    [Header("Debug")]
    public float driftAngle;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    private InputAction steerAction;
    private InputAction throttleAction;
    private InputAction driftAction;

    private Vector3 previousForward;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        var drivingMap = inputActions.FindActionMap("Driving");
        steerAction = drivingMap.FindAction("Steer");
        throttleAction = drivingMap.FindAction("Throttle");
        driftAction = drivingMap.FindAction("Drift");

        drivingMap.Enable();

        previousForward = transform.forward;
    }

    void Update()
    {
        float throttle = throttleAction.ReadValue<float>();
        float steer = steerAction.ReadValue<float>();

        // Remap 0-1 to -1 to +1
        steer = (steer - 0.5f) * 2f + 0.5f;

        // Deadzone
        if (Mathf.Abs(steer) < 0.1f)
            steer = 0;

        // Calculate forward and lateral movement
        Vector3 forward = transform.forward * throttle * speed;
        Vector3 lateral = - transform.right * steer * driftIntensity;

        bool isDrifting = driftAction.ReadValue<float>() > 0f;

        // Combine movement
        moveDirection = isDrifting ? forward + lateral : forward;
        moveDirection.y -= gravity * Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);

        // Rotate car
        float rotationFactor = isDrifting ? driftSteerMultiplier : 1f;
        transform.Rotate(0, steer * rotationSpeed * rotationFactor * Time.deltaTime, 0);

        // Drift angle debug
        CalculateDriftAngle();
    }

    void CalculateDriftAngle()
    {
        // Angle between where car is facing and where it's actually moving
        Vector3 flatVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z);

        if (flatVelocity.magnitude > 0.1f)
        {
            driftAngle = Vector3.SignedAngle(flatForward, flatVelocity, Vector3.up);
        }
        else
        {
            driftAngle = 0;
        }
    }
}
