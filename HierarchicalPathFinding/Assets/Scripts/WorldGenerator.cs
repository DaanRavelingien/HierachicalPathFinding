using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField]
    private Tilemap m_BaseTileMap = null;

    [SerializeField]
    private Tilemap m_BeautyTileMap = null;

    [SerializeField]
    private List<string> m_TileNames = new List<string>();

    [SerializeField]
    private List<TileBase> m_TileBases = new List<TileBase>();

    private Dictionary<string, TileBase> m_Tiles = new Dictionary<string, TileBase>();

    [SerializeField]
    private Vector2Int m_WorldSize = new Vector2Int(5, 5);

    [SerializeField]
    private int m_WallThickness = 3;

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

    private Cell[,] m_Grid;
    public Cell[,] Grid
    {
        get { return m_Grid; }
        private set { m_Grid = value; }
    }

    private void Start()
    {
        GenerateMap();
        CreateBorder(m_WallThickness * 2);

        m_GridPreProcessor.PreProcessingGrid(m_Grid, m_WorldSize.x);

        //putting the list of tile names and tile bases in a dict for easier use
        if (m_TileNames.Count != m_TileBases.Count)
            Debug.LogError("The amount of tile names and tile bases should be the same");
        for (int i = 0; i < m_TileNames.Count; ++i)
        {
            m_Tiles.Add(m_TileNames[i], m_TileBases[i]);
        }

        SetTileMapBase();
        SetTileMapBeauty();
    }

    // Start is called before the first frame update
    private void Awake()
    {

    }

    private void GenerateMap()
    {
        //starting with a new slate to generate the map
        m_BaseTileMap.ClearAllTiles();
        //creating the array where we will store our world
        m_Grid = new Cell[m_WorldSize.x, m_WorldSize.y];

        Grid grid = m_BaseTileMap.layoutGrid;

        //filling the cell array with just floor cells to start with
        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            {
                Cell cell = new Cell();

                //setting the pos based on the grid the tile map is on for later uses
                cell.pos = new Vector2Int(w, h);
                cell.cellType = CellType.ground;

                m_Grid[w, h] = cell;
            }
        }

                //simple algorithm to generate a maze ish structure
        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            {
                //generating a gird spaced out 1 from each other
                //0 0 0 0 0
                //0 1 0 1 0
                //0 0 0 0 0
                //0 1 0 1 0
                //0 0 0 0 0 

                if (w % (m_WallThickness * 2) == m_WallThickness + (m_WallThickness / 2)
                    && h % (m_WallThickness * 2) == m_WallThickness + (m_WallThickness / 2))
                {
                    MakeWall(m_WallThickness, new Vector2Int(w, h));

                    int offset = m_WallThickness;
                    int rand = Random.Range(0, 4);
                    
                    switch (rand)
                    {
                        case 0:
                            MakeWall(m_WallThickness, new Vector2Int(w + offset, h));
                            break;
                        case 1:
                            MakeWall(m_WallThickness, new Vector2Int(w - offset, h));
                            break;
                        case 2:
                            MakeWall(m_WallThickness, new Vector2Int(w, h + offset));
                            break;
                        case 3:
                            MakeWall(m_WallThickness, new Vector2Int(w, h - offset));
                            break;
                    }
                }
            }
        }
    }

    //the size has to be an odd nr
    private void MakeWall(int size, Vector2Int cellPos)
    {
        if (size % 2 == 0)
        {
            Debug.LogError("Please use an odd number for the size.");
            return;
        }

        Cell wallCell = new Cell();
        wallCell.cellType = CellType.wall;

        for(int w = 0; w<size; w++)
        {
            int widhtOffset = cellPos.x - size / 2 + w;
             
            for(int h = 0; h<size; h++)
            {
                int heightOffset = cellPos.y - size / 2 + h;

                wallCell.pos = m_Grid[widhtOffset, heightOffset].pos;
                m_Grid[widhtOffset, heightOffset] = wallCell;
            }
        }
    }

    private void CreateBorder(int width)
    {
        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            { 
                if(w < width || w >= m_WorldSize.x-width 
                    || h<width || h>=m_WorldSize.y-width )
                {
                    m_Grid[w, h].cellType = CellType.wall;
                }
            }
        }
    }

    private void SetTileMapBase()
    {
        //creating the floor tiles
        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            {
                TileBase tile = null;

                switch(m_Grid[w,h].cellType)
                {
                    case CellType.ground:
                        m_Tiles.TryGetValue("GroundBaseTile", out tile);
                        break;
                    case CellType.wall:
                        m_Tiles.TryGetValue("WallBaseTile", out tile);
                        break;
                    default:
                        break;
                }

                if (tile == null)
                {
                    Debug.LogError("Could not find tile with key");
                }

                m_BaseTileMap.SetTile(new Vector3Int(w, h, 0), tile);
            }
        }
    }

    private void SetTileMapBeauty()
    {
        int brickChance = 5;

        for (int w = 0; w < m_WorldSize.x; w++)
        {
            for (int h = 0; h < m_WorldSize.y; h++)
            {
                TileBase tile = null;

                switch (m_Grid[w, h].cellType)
                {
                    //spawning some decorative brickwork on the ground
                    case CellType.ground:
                        int rand = Random.Range(0, 100);
                        if (brickChance > rand)
                        {
                            rand = Random.Range(0, 6);
                            m_Tiles.TryGetValue("GroundBricks" + rand, out tile);
                        }
                        break;
                    
                    
                    case CellType.wall:
                        int wallcaseNr = WallDecider(new Vector2Int(w, h));
                        
                        if (wallcaseNr == 0)
                            continue;

                        switch(wallcaseNr)
                        {
                            // a 1 stands for a wall 0 for ground
                            //1 1 1
                            //1 1 1
                            //1 1 1
                            case 255:
                                break;
                            //1 1 1
                            //1 1 1
                            //1 0 0
                            case 231:
                            //1 1 1
                            //1 1 1
                            //0 0 1
                            case 215:
                            //1 1 1
                            //1 1 1
                            //0 0 0
                            case 199:
                                m_Tiles.TryGetValue("WallTile0", out tile);
                                break;
                            //1 1 0
                            //1 1 1
                            //1 1 1
                            case 253:
                                m_Tiles.TryGetValue("WallTile1", out tile);
                                break;
                            //0 1 1 
                            //1 1 1
                            //1 1 1
                            case 251:
                                m_Tiles.TryGetValue("WallTile2", out tile);
                                break;
                            //1 0 0
                            //1 1 1
                            //1 1 1
                            case 252:
                            //0 0 1
                            //1 1 1
                            //1 1 1
                            case 250:
                            //0 0 0
                            //1 1 1
                            //1 1 1
                            case 248:
                                m_Tiles.TryGetValue("WallTile3", out tile);
                                break;
                            //1 1 1
                            //1 1 1
                            //1 1 0
                            case 239:
                                m_Tiles.TryGetValue("WallTile4", out tile);
                                break;
                            //1 1 1
                            //1 1 1
                            //0 1 1
                            case 223:
                                m_Tiles.TryGetValue("WallTile5", out tile);
                                break;
                            //1 1 0
                            //1 1 0
                            //1 1 1
                            case 189:
                            //1 1 1
                            //1 1 0
                            //1 1 0
                            case 175:
                            //1 1 0
                            //1 1 0
                            //1 1 0
                            case 173:
                                m_Tiles.TryGetValue("WallTile6", out tile);
                                break;
                            //0 0 0
                            //1 1 0
                            //1 1 0
                            case 168:
                                m_Tiles.TryGetValue("WallTile7", out tile);
                                break;
                            //1 1 0
                            //1 1 0
                            //0 0 0
                            case 133:
                                m_Tiles.TryGetValue("WallTile8", out tile);
                                break;
                            //0 1 1
                            //0 1 1
                            //1 1 1
                            case 123:
                            //1 1 1
                            //0 1 1
                            //0 1 1
                            case 95:
                            //0 1 1
                            //0 1 1
                            //0 1 1
                            case 91:
                                m_Tiles.TryGetValue("WallTile9", out tile);
                                break;
                            //0 0 0
                            //0 1 1
                            //0 1 1
                            case 88:
                                m_Tiles.TryGetValue("WallTile10", out tile);
                                break;
                            //0 1 1
                            //0 1 1
                            //0 0 0
                            case 67:
                                m_Tiles.TryGetValue("WallTile11", out tile);
                                break;
                            default:
                                Debug.LogError("unknown wall configuration: " + wallcaseNr);
                                break;
                        }

                        break;
                    default:
                        break;
                }

                m_BeautyTileMap.SetTile(new Vector3Int(w, h, 0), tile);
            }
        }
    }

    private int WallDecider(Vector2Int cellPos)
    {
        //if the wall is on the border dont check it
        if(cellPos.x < m_WallThickness 
            || cellPos.x >= m_WorldSize.x - m_WallThickness
            || cellPos.y < m_WallThickness 
            || cellPos.y >= m_WorldSize.y - m_WallThickness)
        {
            return 0;
        }

        byte caseNr = 0b_0000_0000;

        if (m_Grid[cellPos.x, cellPos.y + 1].cellType == CellType.wall)
            caseNr += 0b_0000_0001;
        if (m_Grid[cellPos.x + 1, cellPos.y + 1].cellType == CellType.wall)
            caseNr += 0b_0000_0010;
        if (m_Grid[cellPos.x - 1, cellPos.y + 1].cellType == CellType.wall)
            caseNr += 0b_0000_0100;
        if (m_Grid[cellPos.x, cellPos.y - 1].cellType == CellType.wall)
            caseNr += 0b_0000_1000;
        if (m_Grid[cellPos.x + 1, cellPos.y - 1].cellType == CellType.wall)
            caseNr += 0b_0001_0000;
        if (m_Grid[cellPos.x - 1, cellPos.y - 1].cellType == CellType.wall)
            caseNr += 0b_0010_0000;
        if (m_Grid[cellPos.x + 1, cellPos.y].cellType == CellType.wall)
            caseNr += 0b_0100_0000;
        if (m_Grid[cellPos.x - 1, cellPos.y].cellType == CellType.wall)
            caseNr += 0b_1000_0000;

        return caseNr;
    }
}
