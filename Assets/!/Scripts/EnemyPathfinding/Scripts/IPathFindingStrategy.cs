using UnityEngine;
using System.Collections.Generic;

public interface IPathfindingStrategy
{
    bool CalculatePath(Vector3 start, Vector3 end, out List<Vector3> pathPoints);
    void Stop();
    bool IsPathStale();
}

public class UnityNavMeshStrategyErik : MonoBehaviour, IPathfindingStrategy
{
    private UnityEngine.AI.NavMeshAgent _agent;

    public void Awake() { _agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); }

    public bool CalculatePath(Vector3 start, Vector3 end, out List<Vector3> pathPoints)
    {
        //later put modular wrapper shit here or i have to apply this to every yokai manually
        pathPoints = new List<Vector3>();
        if (!_agent.isOnNavMesh) return false;
        
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        _agent.CalculatePath(end, path);
        
        foreach(var corner in path.corners) {
            pathPoints.Add(corner);
        }
        return path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    }

    public void Stop() { _agent.isStopped = true; }

    public bool IsPathStale() { return _agent.pathPending; }
}