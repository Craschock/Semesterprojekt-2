using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyPath : MonoBehaviour
{
    //patrol stuff
    public Transform[] patrolPoints;
    public int currentPointIndex = 0;
    public float speed = 1.0f;
    public float runSpeed = 3.0f;

    public NavMeshAgent agent; //drag n drop inside unity
    
    //states, maybe use for animation
    public bool isAlerted = false;
    public Vector3 alertLocation;

    //stuff for a* to expand on navMesh
    public bool useMyPathfinding = false; 
    GridPathfinder pathfinder;
    List<Vector3> currentPath; //path enemy is going before a*
    int pathIndex = 0;

    void Start()
    {
        if(agent == null)
            agent = GetComponent<NavMeshAgent>();

        agent.speed = speed;
        
        //setup a* grid //Grid size hardcoded erstmal, muss melvo fragen erst ob wir das dynamic brauchen
        if (useMyPathfinding)
        {
            pathfinder = new GridPathfinder();
            pathfinder.CreateGrid(transform.position, 50, 1);
        }
    }

    void Update()
    {
        //debug
        // if(isAlerted) 
        //     GetComponent<Renderer>().material.color = Color.red;
        // else 
        //     GetComponent<Renderer>().material.color = Color.blue;

        if (useMyPathfinding)
        {
            Vector3 target;
            //expand navmesh via a* grid
            if (isAlerted)
            {
                target = alertLocation;
            }
            else
            {
                target = patrolPoints[currentPointIndex].position;
            }
            
            //calculate path only if we have none or target changed 
            //for now only recalc if path is null (cuz this might be ass for performance, gotta check)
            if(currentPath == null)
            {
                currentPath = pathfinder.FindPath(transform.position, target);
                pathIndex = 0;
            }

            //custom navmesh moveTowards mechanic
            if(currentPath != null && pathIndex < currentPath.Count)
            {
                Vector3 nextNode = currentPath[pathIndex];
                
                float currentMovementSpeed;
                if (isAlerted)
                {
                    currentMovementSpeed = runSpeed;
                }
                else
                {
                    currentMovementSpeed = speed;
                }

                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    nextNode, 
                    currentMovementSpeed * Time.deltaTime
                );

                //check if node distance is reached, if so move to next
                if(Vector3.Distance(transform.position, nextNode) < 0.2f)
                {
                    pathIndex++;
                }
            }
            else
            {
                //runs if we reached end of current path
                currentPath = null; //set currentPath to null so earlier check will recalculate
                
                if(!isAlerted)
                {
                    //go to next
                    currentPointIndex++;
                    if (currentPointIndex >= patrolPoints.Length) currentPointIndex = 0;
                }
                else
                {
                    //check if investigation over
                    float dist = Vector3.Distance(transform.position, alertLocation);
                    if(dist < 1f)
                    {
                        isAlerted = false;
                        agent.speed = speed;
                    }
                }
            }
        }
        else
        {
            //NAV MESH LOGIC
            if (isAlerted)
            {
                //investigation
                agent.speed = runSpeed;
                agent.SetDestination(alertLocation);

                if (Vector3.Distance(transform.position, alertLocation) < 1.0f)
                {
                    //he is tehre
                    isAlerted = false;
                    agent.speed = speed;
                }
            }
            else
            {
                //patrol/random walk for main yokai
                agent.speed = speed;
                if (patrolPoints.Length > 0)
                {
                    agent.SetDestination(patrolPoints[currentPointIndex].position);

                    //check distance
                    if (agent.remainingDistance < 0.5f && !agent.pathPending)
                    {
                        currentPointIndex++;
                        if (currentPointIndex >= patrolPoints.Length)
                        {
                            currentPointIndex = 0;
                        }
                    }
                }
            }
        }
    }

    //call to trigger enemy
    public void GoToPoint(Vector3 pos)
    {
        alertLocation = pos;
        isAlerted = true;
        currentPath = null; //reset a* path to recalculate
    }
}

//Grid Pathfinder
//this is mostly from the tutorial, but just adjusted
public class GridPathfinder
{
    Node[,] grid;
    float gridSize;
    float nodeRadius;
    int gridSizeX, gridSizeY;
    Vector3 gridOrigin;

    public void CreateGrid(Vector3 center, float size, float nodeSize)
    {
        gridSize = size;
        nodeRadius = nodeSize / 2;
        gridSizeX = Mathf.RoundToInt(size / nodeSize);
        gridSizeY = Mathf.RoundToInt(size / nodeSize);
        gridOrigin = center;

        grid = new Node[gridSizeX, gridSizeY];

        //loop through nodes inside grid instead of instancing like in the tutorial
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                //then turn node pos into world pos
                float xPos = (x * nodeSize) - (size / 2) + center.x;
                float zPos = (y * nodeSize) - (size / 2) + center.z;
                Vector3 worldPoint = new Vector3(xPos, 0, zPos);

                //and check if in layer Default (anstatt wie in tutorial walkable check (melvin sagt Default Layer sind nur obstacles))
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, LayerMask.GetMask("Default"));
                
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        
        if (startNode == null || targetNode == null || !targetNode.walkable) return null;

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            
            //get lowest fCost node
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost() < currentNode.fCost() || openSet[i].fCost() == currentNode.fCost() && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return GetPath(startNode, targetNode);
            }

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        return null; 
    }

    List<Vector3> GetPath(Node start, Node end)
    {
        List<Vector3> path = new List<Vector3>();
        Node curr = end;
        while(curr != start)
        {
            path.Add(curr.worldPos);
            curr = curr.parent;
        }
        path.Reverse();
        return path;
    }

    float GetDistance(Node A, Node B)
    {
        int dstX = Mathf.Abs(A.gridX - B.gridX);
        int dstY = Mathf.Abs(A.gridY - B.gridY);
        //14 is ca. sqrt(2) * 10 für diagonale, und 10 für gerade
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    Node NodeFromWorldPoint(Vector3 worldPos)
    {
        //convert world to grid
        float percentX = (worldPos.x + gridSize / 2) - gridOrigin.x; //simplified math from tutorial... kinda
        float percentY = (worldPos.z + gridSize / 2) - gridOrigin.z;
        
        //i know this looks bad but it works for 0,0 center imo
        percentX = percentX / gridSize;
        percentY = percentY / gridSize;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        
        return grid[x, y];
    }
}

public class Node //node class init
{
    public bool walkable;
    public Vector3 worldPos;
    public int gridX, gridY;
    
    public float gCost;
    public float hCost;
    public Node parent;

    public Node(bool _walkable, Vector3 _pos, int _x, int _y)
    {
        walkable = _walkable;
        worldPos = _pos;
        gridX = _x;
        gridY = _y;
    }

    public float fCost() { return gCost + hCost; }
}