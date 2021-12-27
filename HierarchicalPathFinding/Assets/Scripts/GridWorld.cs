using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWorld : MonoBehaviour
{
    [SerializeField]
    private Vector2Int m_WorldSize = new Vector2Int(5, 5);
    public Vector2Int WorldSize
    {
        get { return m_WorldSize; }
        private set { m_WorldSize = value; }
    }

    private WorldGenerator m_WorldGenerator = null;

    [SerializeField]
    private PathVisualizer m_PathVisualizer = null;
    public PathVisualizer PathVisualizer
    {
        get { return m_PathVisualizer; }
        private set { m_PathVisualizer = value; }
    }

    private Pathfinding m_Pathfinding = null;
    public Pathfinding GridPathFinding
    {
        get { return m_Pathfinding; }
        private set { m_Pathfinding = value; }
    }

    public enum CellType
    {
        ground = 0,
        wall = 1
    }

    public struct Cell
    {
        public Vector2Int pos;
        public CellType cellType;
    }

    private Cell[,] m_WorldCells;
    public Cell[,] Cells
    {
        get { return m_WorldCells; }
        private set { m_WorldCells = value; }
    }

    private void Awake()
    {
        //creating the array where we will store our world
        m_WorldCells = new Cell[m_WorldSize.x, m_WorldSize.y];

        //filling the cell array with just floor cells to start with
        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            {
                Cell cell = new Cell();

                //setting the pos based on the grid the tile map is on
                cell.pos = new Vector2Int(w, h);
                cell.cellType = CellType.ground;

                m_WorldCells[w, h] = cell;
            }
        }

        m_WorldGenerator = new WorldGenerator(this);
        m_WorldGenerator.GenerateMaze(3);
        m_WorldGenerator.CreateBorder(6);

        m_Pathfinding = new Pathfinding();
        m_Pathfinding.SetSizeToWorld(this);
        //for hirarchical pathfinding we need to pre process the world
        m_Pathfinding.PreProcessingGrid(this, Cells, WorldSize.x);

        //showing the abstract graph
        m_PathVisualizer.ShowPreProcessedGraph();
    }
    public void ToggleCell(Vector2 pos)
    {
        if(pos.x >= 0 || pos.y >= 0 || pos.x < m_WorldSize.x || pos.y < m_WorldSize.y)
        {
            Vector2Int cellPos = new Vector2Int((int)pos.x,(int)pos.y);
            Cell cell = m_WorldCells[cellPos.x, cellPos.y];

            if (cell.cellType == CellType.wall)
                m_WorldCells[cellPos.x, cellPos.y].cellType = CellType.ground;
            else if(cell.cellType == CellType.ground)
                m_WorldCells[cellPos.x, cellPos.y].cellType = CellType.wall;

            //rendering the world again so its correct
            GetComponent<WorldVisualizer>().SetVisualized();

            //recalculating the cluster where the cell was in
            m_Pathfinding.PreProcessCluster(this, cellPos, Cells);
        }
    }
}
