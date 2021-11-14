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

    [SerializeField]
    private WorldGenerator m_WorldGenerator = null;

    [SerializeField]
    private GridPreProcessor m_GridPreProcessor = null;

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

        m_WorldGenerator.GenerateMaze(3);
        m_WorldGenerator.CreateBorder(6);

        m_GridPreProcessor.PreProcessingGrid(m_WorldCells, m_WorldSize.x);
    }
}
