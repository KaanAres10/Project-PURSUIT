using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWaypointsLoop : MonoBehaviour
{
    public Transform[] waypoints;
    public int startIndex = 0;     // set different values per car (e.g., 0 and 2)
    public float moveSpeed = 10f;
    public float turnSpeed = 6f;
    public float arriveDistance = 2f;

    int index;

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        index = ((startIndex % waypoints.Length) + waypoints.Length) % waypoints.Length;
        // Optional: start each car at its start waypoint
        transform.position = waypoints[index].position;
        transform.rotation = Quaternion.LookRotation(
            new Vector3(
                (waypoints[(index + 1) % waypoints.Length].position - transform.position).x,
                0f,
                (waypoints[(index + 1) % waypoints.Length].position - transform.position).z
            )
        );
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 target = waypoints[index].position;
        Vector3 to = target - transform.position;
        Vector3 flatDir = new Vector3(to.x, 0f, to.z);

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(flatDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        if (flatDir.magnitude <= arriveDistance)
            index = (index + 1) % waypoints.Length; // loop forever
    }
}

