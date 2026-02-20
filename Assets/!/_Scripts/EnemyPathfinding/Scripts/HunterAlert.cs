using UnityEngine;

public class HunterAlert : MonoBehaviour
{

    private EnemyControl enemyControl;

    void Start()
    {
        enemyControl = GetComponent<EnemyControl>();
    }

    void Update()
    {
        
    }

    public void gotAlerted(Vector3 alertedPosition)
    {
        // Debug.Log("Guardian Alerted at " + alertedPosition);
        if (enemyControl != null)
        {
            //tell yokai to stop patrolling and go to this point instead
            enemyControl.InvestigatePoint(alertedPosition);
        }
    }
}
