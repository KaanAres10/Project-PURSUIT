using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;


public class SteeringMechanics : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActions;
    public TextMeshProUGUI speedText;

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
    private Transform yAxisLockObj;
    private float fixedY; // to remember the Y

    [Header("Audio Settings")]
    public AudioSource engineAudio;
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;


    [Header("Steering wheel asset")]
    public Transform steeringWheelTransform;
    public float steeringWheelMaxRotation = 450f;
    private Quaternion initialSteeringWheelRotation;

    void Start()
    {
        GameObject obj = GameObject.Find("YAxisLock");
        if (obj != null)
        {
            yAxisLockObj = obj.transform;
            fixedY = yAxisLockObj.position.y; // store its initial Y
        }

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

        if (engineAudio == null)
            engineAudio = GetComponent<AudioSource>();
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
        float steerInput = -1f * steerLeft + 1.5f * steerRight; // needs to be calibrated to the steering wheel
        float steerAngle = steerInput * currentSteerRange;

        // Apply steering to front wheels
       // bool isAccelerating = Mathf.Sign(throttle) == Mathf.Sign(forwardSpeed);

        Vector3 pos = yAxisLockObj.position;
        yAxisLockObj.position = new Vector3(pos.x, fixedY, pos.z);

        // Example: lock rotation so only Y-axis rotates
        Vector3 rot = yAxisLockObj.rotation.eulerAngles;
        yAxisLockObj.rotation = Quaternion.Euler(0f, rot.y, 0f);

        if (steeringWheelTransform != null)
        {
            float wheelRotation = steerInput * steeringWheelMaxRotation;

            Quaternion steeringRotation = Quaternion.Euler(0, wheelRotation, 0);

            steeringWheelTransform.localRotation = initialSteeringWheelRotation * steeringRotation;
        }

        Debug.Log("tmp: " + tmp + "speed: " + forwardSpeed);


        foreach (var wheel in wheels)
        {


            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = steerAngle;
            }


            
            if (tmp == 1 && forwardSpeed >= -1e5f)
            {

                wheel.WheelCollider.brakeTorque = 0;

                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = tmp * currentMotorTorque;
                }
            }

            else if (tmp == -1 && forwardSpeed >= 0) //if braking while going forward
            {

                wheel.WheelCollider.motorTorque = 0;
                wheel.WheelCollider.brakeTorque = brakeTorque;

            }

            else if (tmp == 1 && forwardSpeed <= 1)
            {

                wheel.WheelCollider.motorTorque = 0;
                wheel.WheelCollider.brakeTorque = brakeTorque;

            }

            else if (tmp == -1 && forwardSpeed < 1)
            {
                wheel.WheelCollider.brakeTorque = 0;
                if (wheel.motorized)
                {
                    Debug.Log("reversing");
                    wheel.WheelCollider.motorTorque = tmp * currentMotorTorque;
                }
            }

            else if (tmp == 0)
            {
                wheel.WheelCollider.motorTorque = 0;
                wheel.WheelCollider.brakeTorque = 0.3f * brakeTorque;
            }
            

        }
        UpdateEngineSound(speedFactor);
        UpdateTimerUI();

    void UpdateTimerUI()
    {
        if (speedText != null)
        {
            speedText.text = $"{Mathf.RoundToInt(forwardSpeed)} mph";
        }
    }

    void UpdateEngineSound(float speedFactor)
    {
        if (engineAudio != null)
        {
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
        }
    }


    }


}