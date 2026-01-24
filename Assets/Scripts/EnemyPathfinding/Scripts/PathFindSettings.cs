using UnityEngine;
using System.Collections.Generic;

public class AIPathfindingSettings : ScriptableObject
{
    [Header("Movement Dynamics")]
    public float accelerationSmoothing = 5.0f;
    
    [Header("Corner Cutting")]
    //should be somwhere between 0.1 or 2.0 or it will be cooked, but 0.5 looks best
    public float cornerApproachRadius = 0.5f;
    
    public AnimationCurve speedOverTime;
    public AnimationCurve accelerationOverTime;
    
    [Header("Pathfinding")]
    public IPathfindingStrategy pathfindingStrategy = new UnityNavMeshStrategyErik();
}
