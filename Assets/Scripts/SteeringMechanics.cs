using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringMechanics : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActions;

    [Header("Movement Settings")]
    public float acceleration = 1000f;
    public float maxSpeed = 40f;
    public float rotationSpeed = 100f;
    public float centreOfGravityOffset = -1f;
    public float motorTorque = 2000;
    public float brakeTorque = 2000;
    public float steeringRange = 30;
    public float steeringRangeAtMaxSpeed = 10;

    [Header("Wheel References")]
    WheelControl[] wheels;

    private InputAction steerLeftAction;
    private InputAction steerRightAction;
    private InputAction throttleAction;
    private InputAction brakeAction;

    private Rigidbody rb;

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
    }

    void FixedUpdate()
    {
        float throttle = throttleAction.ReadValue<float>();
        float steerLeft = steerLeftAction.ReadValue<float>();
        float steerRight = steerRightAction.ReadValue<float>();

        // Combine steering inputs
        float steerAngle = (-steerLeft + steerRight) * rotationSpeed;

        float forwardSpeed = Vector3.Dot(transform.forward, rb.velocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, forwardSpeed);
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);
        // Apply steering to front wheels
        bool isAccelerating = Mathf.Sign(throttle) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            // Apply steering to Wheel colliders that have "Steerable" enabled
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = steerAngle * currentSteerRange;
            }

            if (isAccelerating)
            {
                // Apply torque to Wheel colliders that have "Motorized" enabled
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = throttle * currentMotorTorque;
                }
                wheel.WheelCollider.brakeTorque = 0;
            }
            else
            {
                // If the user is trying to go in the opposite direction
                // apply brakes to all wheels
                wheel.WheelCollider.brakeTorque = Mathf.Abs(throttle) * brakeTorque;
                wheel.WheelCollider.motorTorque = 0;
            }
        }
    }
}
