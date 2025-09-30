using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpotlightDetector : MonoBehaviour
{

    [Header("Setup")]
    public Light spotlight;
    public Transform car;
    public Image uiFillBar;
    private Renderer hornRenderer;
    private Renderer leatherRenderer;

    [Header("Detection Settings")]
    public float requiredTime = 5f;
    public LayerMask obstructionMask;

    public UiManager uiManager;
    public FuzzyEffectController fuzzyController;

    private float detectionTimer = 0f;

    [Header("Steering Wheel warnings")]
    public Color detectedColor = Color.red;
    public Color hiddenColor = Color.green;

    // Update is called once per frame

    void Start()
    {
        GameObject carObj = GameObject.FindGameObjectWithTag("Car");
        if (carObj != null){ 
            car = carObj.transform;

            // Search for steering_green among children
            Transform steering = car.Find("steering_green");
            if (steering == null)
            {
                Debug.LogError("steering_green not found under Car");
             
            }
            // Search for horn and leather under steering
            hornRenderer = steering.Find("horn")?.GetComponent<Renderer>();
            leatherRenderer = steering.Find("Leather")?.GetComponent<Renderer>();
            Debug.Log(hornRenderer);

            if (hornRenderer == null)
            {
                Debug.Log("horn not found under Car");
            }
            if (leatherRenderer == null)
            {
                Debug.Log("Leather not found under Car");
            }
        }
        else
            Debug.LogError("Car not found! Make sure it has tag 'Car'.");
    }

    void Update()
    {
        if (CarInSpotlight())
        {
            Debug.Log("Can see youoooooo");
           
            detectionTimer += Time.deltaTime;

            if (hornRenderer != null) {
                hornRenderer.material.SetColor("_BaseColor", detectedColor);
                hornRenderer.material.SetColor("_EmissionColor", detectedColor);
            }
            if (leatherRenderer != null)
            {
                leatherRenderer.material.SetColor("_BaseColor", detectedColor);
                leatherRenderer.material.SetColor("_EmissionColor", detectedColor);
            }

            if (detectionTimer >= requiredTime)
            {
                ProjectorPlayerWins();
            }
        }
        else
        {
            detectionTimer = 0f;

            if (hornRenderer != null)
            {
                hornRenderer.material.SetColor("_BaseColor", hiddenColor);
                hornRenderer.material.SetColor("_EmissionColor", hiddenColor);
            }
            if (leatherRenderer != null)
            {
                leatherRenderer.material.SetColor("_BaseColor", hiddenColor);
                leatherRenderer.material.SetColor("_EmissionColor", hiddenColor);
            }

        }

        if (uiFillBar != null) 
        {
            uiFillBar.fillAmount = detectionTimer / requiredTime;
        }


    }

    bool CarInSpotlight()
    {
        Vector3 toCar = (car.position - spotlight.transform.position);
        float distance = toCar.magnitude;
        Vector3 dirToCar = toCar.normalized;


        //Angle check
        float angle = Vector3.Angle(spotlight.transform.forward, dirToCar);
        if (angle > spotlight.spotAngle / 2f) return false;

        //Distance check
        if(distance > spotlight.range) return false;

        Debug.Log("Angle:" + angle + "Distance: " + distance);
        Debug.Log("necessary spot angle: " + spotlight.spotAngle / 2f);

        //raycast for blockage
        if (Physics.Raycast(spotlight.transform.position, dirToCar, out RaycastHit hit, spotlight.range, ~0))
        {
            //if ray hits the car, success
            if (hit.transform == car) return true;

            //if hits something else, blocked
            if (((1 << hit.transform.gameObject.layer) & obstructionMask) != 0) return false;
        }

        return false;



    }

    void ProjectorPlayerWins()
    {
        Debug.Log("Projector Player Wins!");
        uiManager.ShowWinUI();
    }

    private void OnDrawGizmos()
    {
        if (spotlight != null && car != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spotlight.transform.position, car.position);
        }
    }
}
