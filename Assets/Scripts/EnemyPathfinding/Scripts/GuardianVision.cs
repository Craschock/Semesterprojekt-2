using UnityEngine;

public class GuardianVision : MonoBehaviour
{
    
    public Transform player; //player object
    public float viewRadius = 10f; //how far he can see in front of him
    public float viewAngle = 90f; //how wide the cone will be, important: will be divided by 2 later (e.g. 90° angle means 45° on each side)
    
    public LayerMask obstacleMask = default; //not yet, later this has to be the layer all walls etc are in. (maybe i use tags instead, I'll see)

    private GuardianAlert alertScript;

    void Start()
    {
        alertScript = GetComponent<GuardianAlert>();
    }

    void update()
    {
        CheckForPlayer();
    }

    void CheckForPlayer(){
        //check if player is in range at all
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if(distanceToPlayer <= viewRadius){
            //now check if he is inside the angle as well, this will basically pretend there is a cone while there actually isnt one
            //literally how did i come up with this i'm literally a genius omg
            //wish i came up with this earlier so i didnt waste 3 days on that old approach tho

            if(Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2){
                //if player is both in view range AND in the angle (meaning he is inside the cone (magic i know)) check if there's a wall in between using the old way
                
                if (!Physics.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleMask)){
                    //player is visible
                    alertScript.sendAlert();
                }
            }
        }
    }

}
