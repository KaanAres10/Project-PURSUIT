using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringMechanics : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActions;

    [Header("Movement Settings")]
    public float acceleration = 1000000f;
    public float maxSpeed = 100f;
    public float rotationSpeed = 100f;
    public float centreOfGravityOffset = -2f;
    public float motorTorque = 200000;
    public float brakeTorque = 100000;
    public float steeringRange = 50;
    public float steeringRangeAtMaxSpeed = 10;

    [Header("Wheel References")]
    WheelControl[] wheels;

    private InputAction steerLeftAction;
    private InputAction steerRightAction;
    private InputAction throttleAction;
    private InputAction brakeAction;

    private Rigidbody rb;

    [Header("Steering wheel asset")]
    public Transform steeringWheelTransform;
    public float steeringWheelMaxRotation = 450f;
    private Quaternion initialSteeringWheelRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Vector3 centerOfMass = rb.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rb.centerOfMass = centerOfMass;

        wheels = GetComponentsInChildren<WheelControl>();

        var drivingMap = inputActions.FindActionMap("Driving");
        steerLeftAction = drivingMap.FindAction("SteerLeft");
        steerRightAction = drivingMap.FindAction("SteerRight");
        throttleAction = drivingMap.FindAction("Throttle");
        brakeAction = drivingMap.FindAction("Drift");

        drivingMap.Enable();

        if (steeringWheelTransform != null)
        {
            initialSteeringWheelRotation = steeringWheelTransform.localRotation;
        }
    }

    void FixedUpdate()
    {
        float throttle = throttleAction.ReadValue<float>();
        float steerLeft = steerLeftAction.ReadValue<float>();
        float steerRight = steerRightAction.ReadValue<float>();
        float brake = brakeAction.ReadValue<float>();
        float tmp = throttle - brake;

        float forwardSpeed = Vector3.Dot(transform.forward, rb.velocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, forwardSpeed);
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        // Combine steering inputs
        float steerInput = -3*steerLeft + steerRight; // needs to be calibrated to the steering wheel
        float steerAngle = steerInput * currentSteerRange;

        // Apply steering to front wheels
        bool isAccelerating = Mathf.Sign(throttle) == Mathf.Sign(forwardSpeed);

        if (steeringWheelTransform != null)
        {
            float wheelRotation = steerInput * steeringWheelMaxRotation;

            Quaternion steeringRotation = Quaternion.Euler(0, wheelRotation, 0);

            steeringWheelTransform.localRotation = initialSteeringWheelRotation * steeringRotation;
        }


        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = steerAngle;
            }

            if (wheel.motorized)
            {
                wheel.WheelCollider.motorTorque = tmp * currentMotorTorque;
            }
        }
    }
}