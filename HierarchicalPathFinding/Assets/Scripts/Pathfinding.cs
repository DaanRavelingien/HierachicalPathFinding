using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//pathfinding on a grid
public class Pathfinding : MonoBehaviour
{
    [SerializeField]
    GridWorld m_World = null;

    [SerializeField]
    PathVisualizer m_PathVisualizer = null;

    Vector4 m_SearchRect = new Vector4();
    public Vector4 SearchRect
    {
        get { return m_SearchRect; }
        set { m_SearchRect = value; }
    }

    private void Awake()
    {
        SetSizeToWorld();
    }

    struct CellRecord
    {
        public Vector2Int cellPos;
        public Vector2Int? parentCell;
        public float costSoFar; // accumulated g-costs of all the connections leading up to this one
        public float estimatedTotalCost; // f-cost (= costSoFar + h-cost)
    }

    public List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal)
    {
        if (!IsValid(start))
        {
            Debug.LogError("StartcellPos is not valid");
            return new List<Vector2Int>();
        }
        if (!IsValid(goal))
        {
            Debug.LogError("GoalcellPos is not valid");
            return new List<Vector2Int>();
        }
        if (start == goal)
        {
            Debug.LogWarning("We are already at the destination");
            return new List<Vector2Int>();
        }
        if (m_World.Cells[start.x, start.y].cellType == GridWorld.CellType.wall
            || m_World.Cells[goal.x, goal.y].cellType == GridWorld.CellType.wall)
        {
            Debug.LogWarning("Start or Goal is is a wall");
            return new List<Vector2Int>();
        }

        List<CellRecord> closedCells = new List<CellRecord>();
        List<CellRecord> openCells = new List<CellRecord>();
        CellRecord currentCellRecord = new CellRecord();
        
        //setting values to start our A* pathfinding
        currentCellRecord.cellPos = start;
        currentCellRecord.parentCell = null;
        currentCellRecord.costSoFar = 0;
        currentCellRecord.estimatedTotalCost = CalcHeuristicCost(start, goal);
        openCells.Add(currentCellRecord);

        //keep searching until the goal if found
        while (openCells.Count > 0)
        {
            //find the cell with the lowest cost in the open list
            currentCellRecord.estimatedTotalCost = float.MaxValue;
            foreach(CellRecord cellRecord in openCells)
            {
                if (currentCellRecord.estimatedTotalCost > cellRecord.estimatedTotalCost)
                    currentCellRecord = cellRecord;
            }

            //checking if this cell is the goal if so exit the loop
            if (currentCellRecord.cellPos == goal)
                break;

            //removing the cell from the open list and adding it to the closed list
            openCells.Remove(currentCellRecord);
            closedCells.Add(currentCellRecord);

            //looping over all the neighbours of our current cell
            List<Vector2Int> neighbours = GenerateNeighbours(currentCellRecord.cellPos);
            foreach (Vector2Int cellPos in neighbours)
            {
                CellRecord newCellRecord = new CellRecord();
                newCellRecord.cellPos = cellPos;
                newCellRecord.parentCell = currentCellRecord.cellPos;
                //the connection cost between 2 cells is just 1
                newCellRecord.costSoFar = currentCellRecord.costSoFar + 1;
                newCellRecord.estimatedTotalCost = newCellRecord.costSoFar + CalcHeuristicCost(cellPos, goal);

                //if a cell with the same pos is in the open list with a lower costSoFar skip this cell
                if (openCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;

                //if a cell with the same pos is in the closed list and has a lower costSoFar
                //skip this cell, otherwise add the cell to the open list
                if (closedCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;
                
                openCells.Add(newCellRecord);                
            }
        }

        //if no path was found just return an empty list
        if (currentCellRecord.cellPos != goal)
            return new List<Vector2Int>();                    

        //tracing our path back
        List<Vector2Int> pathCells = new List<Vector2Int>();
        pathCells.Add(currentCellRecord.cellPos);

        while (currentCellRecord.parentCell != null)
        {
            currentCellRecord = closedCells.Find(x => x.cellPos == currentCellRecord.parentCell);
            pathCells.Add(currentCellRecord.cellPos);
        }
        //adding the start as well since it is not in the closedCell list
        pathCells.Add(start);

        //reversing our path so our start is at the front an goal at the back of the list
        pathCells.Reverse();

        //setting the path of the path visualizer and redrawing it
        m_PathVisualizer.Path = pathCells;
        m_PathVisualizer.StartPos = start;
        m_PathVisualizer.GoalPos = goal;
        m_PathVisualizer.SetVisualized();

        return pathCells;
    }

    private bool IsValid(Vector2Int cell)
    {
        if (cell.x >= m_SearchRect.x && cell.y >= m_SearchRect.y 
            && cell.x < m_SearchRect.z && cell.y < m_SearchRect.w)
            return true;
        return false;
    }

    //distance
    private float CalcHeuristicCost(Vector2 startCell, Vector2 goalCell)
    {
        float distance = (startCell - goalCell).magnitude;
        return distance;
    }

    //get the positions around one cell will return the 8 surrounding positions
    private List<Vector2Int> GenerateNeighbours(Vector2Int centerCellPos)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        for(int i = 0; i< 3; ++i)
        {
            for(int j=0; j<3; ++j)
            {
                //skip the center cell
                if (i == 1 && j == 1)
                    continue;

                Vector2Int neighbourPos = new Vector2Int(i + centerCellPos.x - 1, j + centerCellPos.y - 1);

                //also skip the cell if it is not valid
                if (!IsValid(neighbourPos))
                    continue;

                //add the neighbour if its a tile that can be walked upon
                if (m_World.Cells[neighbourPos.x,neighbourPos.y].cellType == GridWorld.CellType.ground)
                neighbours.Add(neighbourPos);
            }
        }       

        return neighbours;
    }

    public float CalculatePathWeight(List<Vector2Int> path)
    {
        //check if the given path is valid
        if (path.Count < 0)
            return 0.0f;

        float totalCostOfPath = 0;

        for(int i = 1; i < path.Count; i++)
        {
            totalCostOfPath += CalcHeuristicCost(path[i - 1], path[i]);
        }

        return totalCostOfPath;
    }

    public void SetSizeToWorld()
    {
        m_SearchRect = new Vector4(0, 0, m_World.WorldSize.x, m_World.WorldSize.y);
    }
}
