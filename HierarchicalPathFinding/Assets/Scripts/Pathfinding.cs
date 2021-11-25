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
        m_SearchRect = new Vector4(0, 0, m_World.WorldSize.x, m_World.WorldSize.y);
    }

    struct CellRecord
    {
        public Vector2Int cellPos;
        public Vector2Int? parentCell;
        public float costSoFar; // accumulated g-costs of all the connections leading up to this one
        public float estimatedTotalCost; // f-cost (= costSoFar + h-cost)
    }

    private Vector2Int m_StartCellPos = new Vector2Int();
    public Vector2Int StartPos
    {
        get { return m_StartCellPos; }
        set { m_StartCellPos = value; }
    }

    private Vector2Int m_GoalCellPos = new Vector2Int();
    public Vector2Int GoalPos
    {
        get { return m_GoalCellPos; }
        set { m_GoalCellPos = value; }
    }

    public List<Vector2Int> FindPathAStar()
    {
        if (!IsValid(StartPos))
        {
            Debug.LogError("StartcellPos is not valid");
            return new List<Vector2Int>();
        }
        if (!IsValid(GoalPos))
        {
            Debug.LogError("GoalcellPos is not valid");
            return new List<Vector2Int>();
        }
        if (StartPos == GoalPos)
        {
            Debug.LogWarning("We are already at the destination");
            return new List<Vector2Int>();
        }
        if (m_World.Cells[StartPos.x, StartPos.y].cellType == GridWorld.CellType.wall
            || m_World.Cells[GoalPos.x, GoalPos.y].cellType == GridWorld.CellType.wall)
        {
            Debug.LogError("Start or Goal is is a wall");
            return new List<Vector2Int>();
        }

        List<CellRecord> closedCells = new List<CellRecord>();
        List<CellRecord> openCells = new List<CellRecord>();
        CellRecord currentCellRecord = new CellRecord();
        
        //setting values to start our A* pathfinding
        currentCellRecord.cellPos = StartPos;
        currentCellRecord.parentCell = null;
        currentCellRecord.costSoFar = 0;
        currentCellRecord.estimatedTotalCost = CalcHeuristicCost(StartPos, GoalPos);
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
            if (currentCellRecord.cellPos == GoalPos)
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
                newCellRecord.estimatedTotalCost = newCellRecord.costSoFar + CalcHeuristicCost(cellPos, GoalPos);

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
        if (currentCellRecord.cellPos != GoalPos)
            return new List<Vector2Int>();                    

        //tracing our path back
        List<Vector2Int> pathCells = new List<Vector2Int>();
        pathCells.Add(currentCellRecord.cellPos);

        while (currentCellRecord.parentCell != null)
        {
            currentCellRecord = closedCells.Find(x => x.cellPos == currentCellRecord.parentCell);
            pathCells.Add(currentCellRecord.cellPos);
        }

        //reversing our path so our start is at the front an goal at the back of the list
        pathCells.Reverse();

        //setting the path of the path visualizer and redrawing it
        m_PathVisualizer.Path = pathCells;
        m_PathVisualizer.StartPos = StartPos;
        m_PathVisualizer.GoalPos = GoalPos;
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
}
