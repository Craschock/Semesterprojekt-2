using UnityEngine;
using System.Collections.Generic;

//maybe increases performance, not quite sure lol
public class PathRequestManager : MonoBehaviour
{
    private Queue<System.Action> _pathRequestQueue = new Queue<System.Action>();
    
    void Update() {
        //only allow one pathfinder per frame
        if (_pathRequestQueue.Count > 0) {
            _pathRequestQueue.Dequeue().Invoke();
        }
    }
    
    public void RequestPath(System.Action callback) {
        _pathRequestQueue.Enqueue(callback);
    }
}
