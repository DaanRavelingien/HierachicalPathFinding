using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator
{
    private GridWorld m_GridWorld = null;
    public WorldGenerator(GridWorld world)
    {
        m_GridWorld = world;
    }

    public void GenerateMaze(int wallThickness)
    {
        //simple algorithm to generate a maze ish structure
        for (int w = 0; w < m_GridWorld.WorldSize.x; w++)
        {
            for (int h = 0; h < m_GridWorld.WorldSize.y; h++)
            {
                //generating a gird spaced out 1 from each other
                //0 0 0 0 0
                //0 1 0 1 0
                //0 0 0 0 0
                //0 1 0 1 0
                //0 0 0 0 0

                if (w % (wallThickness * 2) == wallThickness + (wallThickness / 2)
                    && h % (wallThickness * 2) == wallThickness + (wallThickness / 2))
                {
                    MakeWall(wallThickness, new Vector2Int(w, h));

                    int offset = wallThickness;
                    int rand = Random.Range(0, 4);
                    
                    switch (rand)
                    {
                        case 0:
                            MakeWall(wallThickness, new Vector2Int(w + offset, h));
                            break;
                        case 1:
                            MakeWall(wallThickness, new Vector2Int(w - offset, h));
                            break;
                        case 2:
                            MakeWall(wallThickness, new Vector2Int(w, h + offset));
                            break;
                        case 3:
                            MakeWall(wallThickness, new Vector2Int(w, h - offset));
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

        GridWorld.Cell wallCell = new GridWorld.Cell();
        wallCell.cellType = GridWorld.CellType.wall;

        for(int w = 0; w<size; w++)
        {
            int widhtOffset = cellPos.x - size / 2 + w;
             
            for(int h = 0; h<size; h++)
            {
                int heightOffset = cellPos.y - size / 2 + h;

                wallCell.pos = m_GridWorld.Cells[widhtOffset, heightOffset].pos;
                m_GridWorld.Cells[widhtOffset, heightOffset] = wallCell;
            }
        }
    }

    public void CreateBorder(int width)
    {
        for (int w = 0; w < m_GridWorld.WorldSize.x; w++)
        {
            for (int h = 0; h < m_GridWorld.WorldSize.y; h++)
            { 
                if(w < width || w >= m_GridWorld.WorldSize.x-width 
                    || h<width || h>= m_GridWorld.WorldSize.y-width )
                {
                    m_GridWorld.Cells[w, h].cellType = GridWorld.CellType.wall;
                }
            }
        }
    }
}
