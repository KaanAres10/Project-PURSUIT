using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectorTrackingScaler : MonoBehaviour
{

    [Header("References")]
    public Transform trackerRaw;
    public Transform trackerScaled;
    public Transform spawnPoint;

    [Header("Scaling")]
    public float scaleFactor = 3.0f;
    public bool scaleXZOnly = false;

    private Vector3 initialRawPos;

    // Start is called before the first frame update
    void Start()
    {
        initialRawPos = trackerRaw.position;  //Helicopter object spawn

        if (spawnPoint != null)
        {
            trackerScaled.position = spawnPoint.position;
        }
        else
        {
            //trackerScaled.position = new Vector3(0f, 5f, 0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("initial pos" + initialRawPos);

        Vector3 offset = trackerRaw.position - initialRawPos;

        Debug.Log("offset" + offset);

        if (scaleXZOnly) 
        {
            offset = new Vector3(offset.x * scaleFactor, offset.y, offset.z * scaleFactor);
        }
        else
        {
            offset *= scaleFactor;
        }

        trackerScaled.position = initialRawPos + offset;
        trackerScaled.rotation = trackerRaw.rotation;

        Debug.Log("raw tracker" + trackerRaw.position);
        Debug.Log("post calc offset: " + offset);

    }
}
