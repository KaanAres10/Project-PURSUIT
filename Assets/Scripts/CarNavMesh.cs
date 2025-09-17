using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CarNavMesh : MonoBehaviour
{

    public float wanderingRadius = 15f;

    private NavMeshAgent agent;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetRandomDestination();
    }

    // Update is called once per frame
    void Update()
    {
        if(!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetRandomDestination();
        }
    }

    void SetRandomDestination()
    {

        Vector3 forward = transform.forward;
        Vector3 randomOffset = (Quaternion.Euler(0, Random.Range(-60f, 60f), 0) * forward) * Random.Range(10f, wanderingRadius);

        Vector3 targetPos = transform.position + randomOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, wanderingRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
