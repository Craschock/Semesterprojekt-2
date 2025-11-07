using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log($"{name} was interacted with!");
        Destroy(gameObject);
    }

    public void OnFocus()
    {
        Debug.Log($"{name} focused");
        // Optional: highlight object or change color
        GetComponent<Renderer>().material.color = Color.yellow;
    }

    public void OnLoseFocus()
    {
        Debug.Log($"{name} lost focus");
        GetComponent<Renderer>().material.color = Color.white;
    }
}
