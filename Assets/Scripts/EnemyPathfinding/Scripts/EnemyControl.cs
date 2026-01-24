using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]

public class EnemyControl : MonoBehaviour
{
    public Transform[] patrolPoints;
    public int nextPoint;
    public float speed; //default speed
    public float alertSpeed; //speed when he is investigating

    private NavMeshAgent agent; //the navmesh agent
    private float distance; //distance to next point

    private bool isInvestigating = false; //true if going to alertSpot rn
    private Vector3 investigationPoint; //coords of point he is investigating

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.isStopped = false; //needed so enemy starts moving

        nextPoint = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //CHECK IF INVESTIGATING OR PATROLLING
        if (isInvestigating)
        {
            HandleInvestigation();
        }
        else
        {
            HandlePatrol();
        }
    }

    void HandlePatrol(){
        // Debug.Log("dscbisd");
        agent.SetDestination(patrolPoints[nextPoint].position);
        // Debug.Log("leck mich am arsch")

        distance = Vector3.Distance(transform.position, patrolPoints[nextPoint].position);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Next Point: " + nextPoint);

        if(distance < 1.25f){
            switchNextPoint();
        }
    }

    void HandleInvestigation(){
        //turn yellow during investigation period
        GetComponent<Renderer>().material.color = Color.yellow;

        //yokai is faster during investigation
        agent.speed = alertSpeed;

        agent.SetDestination(investigationPoint);
        
        float distToAlert = Vector3.Distance(transform.position, investigationPoint); //check if reached alertpoint

        if(distToAlert < 0.75f){
            //arrived at alert spot -> resuming patrol, later of course he'll see if he finds player and if so u are cooked
            isInvestigating = false;

            //go back to default color
            GetComponent<Renderer>().material.color = Color.black;

            //go back to default speed
            agent.speed = speed;
        }
    }

    void switchNextPoint(){
        if(nextPoint < patrolPoints.Length - 1){
            nextPoint++;
        }
        else{
            nextPoint = 0;
        }
    }

    public void InvestigatePoint(Vector3 targetPosition)
    {
        //enemy has received a command to go somewhere, used for hunter alerts but could be applied to other things
        investigationPoint = targetPosition;
        isInvestigating = true;
    }
}
