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

    [Header("Detection Settings")]
    public float requiredTime = 5f;
    public LayerMask obstructionMask;

    public UiManager uiManager;
    public FuzzyEffectController fuzzyController;

    private float detectionTimer = 0f;

    // Update is called once per frame

    void Start()
    {
        GameObject carObj = GameObject.FindGameObjectWithTag("Car");
        if (carObj != null)
            car = carObj.transform;
        else
            Debug.LogError("Car not found! Make sure it has tag 'Car'.");
    }

    void Update()
    {
        if (CarInSpotlight())
        {
            Debug.Log("Can see youoooooo");
            //if (fuzzyController.CanDetectCar()) { 
            detectionTimer += Time.deltaTime;

            if (detectionTimer >= requiredTime)
            {
                ProjectorPlayerWins();
            }
      //  }
        }
        else
        {
            detectionTimer = 0f;
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
