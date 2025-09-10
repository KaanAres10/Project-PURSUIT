using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarNPCLogic : MonoBehaviour
{

    public Transform[] waypoints;
    public float speed = 10f;
    public float turnSpeed = 5f;
    public float waypointTolerance = 1f;

    private Transform currentWaypoint;

    // Start is called before the first frame update
    void Start()
    {
        if (waypoints.Length > 0)
        {
            PickNewWaypoint();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentWaypoint == null) return;

        //Move towards currently chosen waypoint
        Vector3 direction = (currentWaypoint.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        //make rotation smooth
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }


        //check if we have reached the waypoint
        float distance = Vector3.Distance(transform.position, currentWaypoint.position);
        if (distance < waypointTolerance)
        {
            PickNewWaypoint();
        }

    }

    void PickNewWaypoint()
    {
            if (waypoints.Length == 0) return;

            //Dont pick same waypoint
            Transform newWaypoint;
            do
            {
                newWaypoint = waypoints[Random.Range(0, waypoints.Length)];
            }
            while (newWaypoint == currentWaypoint && waypoints.Length > 1); 
            
            currentWaypoint = newWaypoint;
            
            
    }
}
