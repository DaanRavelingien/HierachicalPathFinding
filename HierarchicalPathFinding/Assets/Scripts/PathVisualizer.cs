using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField]
    TileBase m_PathTile = null;

    [SerializeField]
    TileBase m_StartTile = null;

    [SerializeField]
    TileBase m_GoalTile = null;

    [SerializeField]
    GameObject m_PathLayer = null;

    private bool m_HasVisualized = true;
    public void SetVisualized()
    {
        m_HasVisualized = false;
    }

    private List<Vector2Int> m_Path = new List<Vector2Int>();
    public List<Vector2Int> Path
    {
        private get { return m_Path; }
        set { m_Path = value; }
    }
    private Vector2Int m_StartCellPos = new Vector2Int();
    public Vector2Int StartPos
    {
        private get { return m_StartCellPos; }
        set { m_StartCellPos = value; }
    }

    private Vector2Int m_GoalCellPos = new Vector2Int();
    public Vector2Int GoalPos
    {
        private get { return m_GoalCellPos; }
        set { m_GoalCellPos = value; }
    }

    private void Update()
    {
        if (m_HasVisualized)
            return;

        ShowPath();

        m_HasVisualized = true;
    }

    private void ShowPath()
    {
        //clearing the canvas
        m_PathLayer.GetComponent<Tilemap>().ClearAllTiles();

        //draw the path
        foreach (Vector2Int pathPoint in Path)
        {
            m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(pathPoint.x, pathPoint.y, 0)
                , m_PathTile);
        }

        //draw the start and end node of our path
        m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(m_StartCellPos.x, m_StartCellPos.y, 0)
                , m_StartTile);
        m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(m_GoalCellPos.x, m_GoalCellPos.y, 0)
                , m_GoalTile);
    }
}
