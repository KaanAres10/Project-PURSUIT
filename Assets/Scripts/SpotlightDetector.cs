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
    private Renderer buttonsRenderer;
    private Renderer leatherRenderer;
    private Renderer screenRenderer;
    private Renderer screenBoarderRenderer;
    private Renderer textRenderer;
    private Renderer wheelRenderer;

    [Header("Detection Settings")]
    public float requiredTime = 5f;
    public float decaySpeed = 1.5f;
    public float detectionRange = 400f;
    public float spotAngle = 20f;
    public LayerMask obstructionMask;

    public UiManager uiManager;

    private float detectionTimer = 0f;

    [Header("Steering Wheel warnings")]
    public Color detectedColor = Color.red;
    public Color hiddenColor = Color.green;
    public Color screenColor;
    public Color screenEmission;


    // Update is called once per frame

    void Start()
    {
        GameObject carObj = GameObject.FindGameObjectWithTag("Car");
        if (carObj != null){ 
            car = carObj.transform;

            // Search for steering_green among children
            Transform steering = car.Find("steering_v2");
            if (steering == null)
            {
                Debug.LogError("steering_v2 not found under Car");
             
            }
            // Search for horn and leather under steering
            buttonsRenderer = steering.Find("Buttons")?.GetComponent<Renderer>();
            leatherRenderer = steering.Find("Leather")?.GetComponent<Renderer>();
            screenRenderer = steering.Find("Screen")?.GetComponent<Renderer>();
            screenBoarderRenderer = steering.Find("ScreenBoarder")?.GetComponent<Renderer>();
            textRenderer = steering.Find("Text")?.GetComponent<Renderer>();
            wheelRenderer = steering.Find("Wheel")?.GetComponent<Renderer>();
            screenColor = screenRenderer.material.GetColor("_BaseColor");
            screenEmission = screenRenderer.material.GetColor("_EmissionColor");
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

            buttonsRenderer.material.SetColor("_BaseColor", detectedColor);
            buttonsRenderer.material.SetColor("_EmissionColor", detectedColor);
          
            leatherRenderer.material.SetColor("_BaseColor", detectedColor);
            leatherRenderer.material.SetColor("_EmissionColor", detectedColor);

            screenBoarderRenderer.material.SetColor("_BaseColor", detectedColor);
            screenBoarderRenderer.material.SetColor("_EmissionColor", detectedColor);

            textRenderer.material.SetColor("_BaseColor", detectedColor);
            textRenderer.material.SetColor("_EmissionColor", detectedColor);

 

     




            if (detectionTimer >= requiredTime)
            {
                ProjectorPlayerWins();
            }
        }
        else
        {
            detectionTimer -= Time.deltaTime * decaySpeed;
            detectionTimer = Mathf.Clamp(detectionTimer, 0f, requiredTime);

            buttonsRenderer.material.SetColor("_BaseColor", hiddenColor);
            buttonsRenderer.material.SetColor("_EmissionColor", hiddenColor);

            leatherRenderer.material.SetColor("_BaseColor", hiddenColor);
            leatherRenderer.material.SetColor("_EmissionColor", hiddenColor);

            screenBoarderRenderer.material.SetColor("_BaseColor", hiddenColor);
            screenBoarderRenderer.material.SetColor("_EmissionColor", hiddenColor);

            textRenderer.material.SetColor("_BaseColor", screenColor);
            textRenderer.material.SetColor("_EmissionColor", screenEmission);


        }

        if (uiFillBar != null) 
        {
            uiFillBar.fillAmount = detectionTimer / requiredTime;
        }


    }

    public bool CarInSpotlight()
    {
        Vector3 toCar = (car.position - spotlight.transform.position);
        float distance = toCar.magnitude;
        Vector3 dirToCar = toCar.normalized;


        //Angle check
        float angle = Vector3.Angle(spotlight.transform.forward, dirToCar);
        if (angle > spotAngle) return false;

        //Distance check
        if(distance > detectionRange) return false;

        Debug.Log("Angle:" + angle + "Distance: " + distance);
        Debug.Log("necessary spot angle: " + spotAngle );

        //raycast for blockage
        if (Physics.Raycast(spotlight.transform.position, dirToCar, out RaycastHit hit, detectionRange, ~0))
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
        if (GameManager.Instance.GameIsOver()) return;

        Debug.Log("Heli Player Wins!");
        GameManager.Instance.heliPlayerWon = true;

        if (uiManager != null)
        {
            uiManager.ShowWinUI();  // could be a "Heli Wins" screen
        }
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
