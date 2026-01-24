using UnityEngine;
using UnityEngine.InputSystem;

public class GuardianAlert : MonoBehaviour
{

    [SerializeField] private HunterAlert hunterAlertScript;

    void Start()
    {
        
    }

    void Update()
    {
        //for now trigger alert function by pressing K, later this triggers by loud things etc.
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            sendAlert();
        }
    }

    public void sendAlert() {
        //call alert and give current position
        hunterAlertScript.gotAlerted(transform.position);
    }
}
