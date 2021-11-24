using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//pathfinding on a grid
public class Pathfinding : MonoBehaviour
{
    [SerializeField]
    GridWorld m_World = null;

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

    public List<Vector2Int> FindPathAStar(Vector2Int startCellPos, Vector2Int goalCellPos)
    {
        if (!IsValid(startCellPos))
            Debug.LogError("StartcellPos is not valid");
        if (!IsValid(goalCellPos))
            Debug.LogError("GoalcellPos is not valid");
        if (startCellPos == goalCellPos)
            Debug.LogWarning("We are already at the destination");
        if (m_World.Cells[startCellPos.x, startCellPos.y].cellType == GridWorld.CellType.wall
            || m_World.Cells[goalCellPos.x, goalCellPos.y].cellType == GridWorld.CellType.wall)
            Debug.LogError("Start or Goal is blocked");


        List<CellRecord> closedCells = new List<CellRecord>();
        List<CellRecord> openCells = new List<CellRecord>();
        CellRecord currentCellRecord = new CellRecord();
        
        //setting values to start our A* pathfinding
        currentCellRecord.cellPos = startCellPos;
        currentCellRecord.parentCell = null;
        currentCellRecord.costSoFar = 0;
        currentCellRecord.estimatedTotalCost = CalcHeuristicCost(startCellPos, goalCellPos);
        openCells.Add(currentCellRecord);

        //keep searching until the goal if found
        while (openCells.Count > 0)
        {
            //find the cell with the lowest cost in the open list
            foreach(CellRecord cellRecord in openCells)
            {
                if (currentCellRecord.estimatedTotalCost > cellRecord.estimatedTotalCost)
                    currentCellRecord = cellRecord;
            }

            //checking if this cell is the goal if so exit the loop
            if (currentCellRecord.cellPos == goalCellPos)
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
                newCellRecord.estimatedTotalCost = newCellRecord.costSoFar + CalcHeuristicCost(cellPos, goalCellPos);

                //if a cell with the same pos is in the open list with a lower costSoFar skip this cell
                if (openCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;

                //if a cell with the same pos is in the closed list and has a lower costSoFar
                //skip this cell, otherwise add the cell to the open list
                if (closedCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;
                else
                    openCells.Add(newCellRecord);                
            }
        }

        //if no path was found just return an empty list
        if (currentCellRecord.cellPos != goalCellPos)
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
