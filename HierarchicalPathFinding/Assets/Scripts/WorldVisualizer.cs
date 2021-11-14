using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldVisualizer : MonoBehaviour
{
    [SerializeField]
    GridWorld m_GridWorld = null;

    [SerializeField]
    private Tilemap m_BaseTileMap = null;

    [SerializeField]
    private Tilemap m_BeautyTileMap = null;

    [SerializeField]
    private List<string> m_TileNames = new List<string>();

    [SerializeField]
    private List<TileBase> m_TileBases = new List<TileBase>();

    private Dictionary<string, TileBase> m_Tiles = new Dictionary<string, TileBase>();

    private bool m_HasVisualized = false;

    private void Awake()
    {
        //putting the list of tile names and tile bases in a dict for easier use
        if (m_TileNames.Count != m_TileBases.Count)
            Debug.LogError("The amount of tile names and tile bases should be the same");
        for (int i = 0; i < m_TileNames.Count; ++i)
        {
            m_Tiles.Add(m_TileNames[i], m_TileBases[i]);
        }
    }

    public void Update()
    {
        if (m_HasVisualized || m_GridWorld.Cells == null)
            return;

        //starting with a new slate to generate the map
        m_BaseTileMap.ClearAllTiles();
        m_BeautyTileMap.ClearAllTiles();

        SetTileMapBase();
        SetTileMapBeauty();

        m_HasVisualized = true;
    }

    private void SetTileMapBase()
    {
        //creating the floor tiles
        for (int w = 0; w < m_GridWorld.WorldSize.x; w++)
        {
            for (int h = 0; h < m_GridWorld.WorldSize.y; h++)
            {
                TileBase tile = null;

                switch (m_GridWorld.Cells[w, h].cellType)
                {
                    case GridWorld.CellType.ground:
                        m_Tiles.TryGetValue("GroundBaseTile", out tile);
                        break;
                    case GridWorld.CellType.wall:
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

        for (int w = 0; w < m_GridWorld.WorldSize.x; w++)
        {
            for (int h = 0; h < m_GridWorld.WorldSize.y; h++)
            {
                TileBase tile = null;

                switch (m_GridWorld.Cells[w, h].cellType)
                {
                    //spawning some decorative brickwork on the ground
                    case GridWorld.CellType.ground:
                        int rand = Random.Range(0, 100);
                        if (brickChance > rand)
                        {
                            rand = Random.Range(0, 6);
                            m_Tiles.TryGetValue("GroundBricks" + rand, out tile);
                        }
                        break;


                    case GridWorld.CellType.wall:
                        int wallcaseNr = WallTileDecider(new Vector2Int(w, h));

                        if (wallcaseNr == 0)
                            continue;

                        switch (wallcaseNr)
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

    private int WallTileDecider(Vector2Int cellPos)
    {
        //if the wall is on the border dont check it
        if (cellPos.x < 0+1 || cellPos.x >= m_GridWorld.WorldSize.x-1
            || cellPos.y < 0+1 || cellPos.y >= m_GridWorld.WorldSize.y-1)
        {
            return 0;
        }

        byte caseNr = 0b_0000_0000;
        GridWorld.Cell[,] cells = m_GridWorld.Cells;

        if (cells[cellPos.x, cellPos.y + 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0000_0001;
        if (cells[cellPos.x + 1, cellPos.y + 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0000_0010;
        if (cells[cellPos.x - 1, cellPos.y + 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0000_0100;
        if (cells[cellPos.x, cellPos.y - 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0000_1000;
        if (cells[cellPos.x + 1, cellPos.y - 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0001_0000;
        if (cells[cellPos.x - 1, cellPos.y - 1].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0010_0000;
        if (cells[cellPos.x + 1, cellPos.y].cellType == GridWorld.CellType.wall)
            caseNr += 0b_0100_0000;
        if (cells[cellPos.x - 1, cellPos.y].cellType == GridWorld.CellType.wall)
            caseNr += 0b_1000_0000;

        return caseNr;
    }
}
