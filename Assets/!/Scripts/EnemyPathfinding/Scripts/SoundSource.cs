using UnityEngine;
using UnityEngine.InputSystem;

public class AreaEffect : MonoBehaviour
{
    [SerializeField] private float radius = 5f; //lol maybe later can be changed depending how lound sound is idk

    void Update()
    {
        //for now trigger alert function by pressing L, later this triggers by loud things etc.
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            CheckNearbyYokai();
            // Debug.Log("asdfasdf");
        }

        //show red radius sphere thing
        transform.GetChild(0).localScale = Vector3.one * radius * 2;
    }

    void CheckNearbyYokai()
    {
        //create a fake sphere and get list of every object it collides with
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var hitCollider in hitColliders)
        {
            //check if the is a guardian, by checking if it has GuardianAlert script
            if (hitCollider.TryGetComponent<GuardianAlert>(out GuardianAlert target))
            {
                //if game object is in raduis and is a guardian, send alert
                target.sendAlert();
            }
        }
    }
}