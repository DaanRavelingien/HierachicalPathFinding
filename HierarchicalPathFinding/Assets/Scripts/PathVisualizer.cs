using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField]
    GridWorld m_World = null;

    //path variables
    [SerializeField]
    TileBase m_PathTile = null;

    [SerializeField]
    TileBase m_PathEndTile = null;

    [SerializeField]
    GameObject m_PathLayer = null;

    //hirachical variables
    [SerializeField]
    GameObject m_GraphLayer = null;

    [SerializeField]
    GameObject m_ConnectionLinePrefab = null;

    [SerializeField]
    TileBase m_EntranceNodeTile = null;

    [SerializeField]
    TileBase m_ClusterBorderTile = null;

    List<GameObject> m_ConnectionLines = new List<GameObject>();

    public void ShowPath(List<Vector2Int> path)
    {
        //clearing the canvas
        m_PathLayer.GetComponent<Tilemap>().ClearAllTiles();

        //draw the path
        foreach (Vector2Int pathPoint in path)
        {
            m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(pathPoint.x, pathPoint.y, 0)
                , m_PathTile);
        }

        //draw the start and end node of our path
        m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(path[0].x, path[0].y, 0)
                , m_PathEndTile);
        m_PathLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(path[path.Count - 1].x
            , path[path.Count - 1].y, 0), m_PathEndTile);
    }

    public void ShowPreProcessedGraph()
    {
        foreach (Pathfinding.Cluster cluster in m_World.GridPathFinding.Clusters)
        {
            ShowCluster(cluster);
        }

        ShowClusterEntrances();
        ShowConnections();
    }

    private void ShowCluster(Pathfinding.Cluster cluster)
    {
        for (int w = 0; w < m_World.GridPathFinding.Clustersize; ++w)
        {
            for (int h = 0; h < m_World.GridPathFinding.Clustersize; ++h)
            {
                if (w <= 0 || h <= 0 ||
                    w >= m_World.GridPathFinding.Clustersize - 1 ||
                    h >= m_World.GridPathFinding.Clustersize - 1)
                {
                    m_GraphLayer.GetComponent<Tilemap>().SetTile(
                        new Vector3Int(cluster.pos.x + w, cluster.pos.y + h, 0)
                , m_ClusterBorderTile);
                }
            }
        }
    }

    private void ShowClusterEntrances()
    {
        for (int i = 0; i < m_World.GridPathFinding.AbstractWorldGraph.Nodes.Count; ++i)
        {
            NodeGraph.Node node = m_World.GridPathFinding.AbstractWorldGraph.Nodes[i];

            m_GraphLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(node.pos.x, node.pos.y, 0)
                , m_EntranceNodeTile);
        }
    }

    private void ShowConnections()
    {
        //disabeling all connections
        foreach (GameObject line in m_ConnectionLines)
            line.SetActive(false);

        //creating lines for the conections if there are not enough
        while (m_ConnectionLines.Count
            < m_World.GridPathFinding.AbstractWorldGraph.Connections.Count / 2)
        {
            GameObject connectionLine = Instantiate(m_ConnectionLinePrefab);
            connectionLine.transform.SetParent(m_GraphLayer.transform);
            connectionLine.SetActive(false);
            m_ConnectionLines.Add(connectionLine);
        }

        //the connections are doubled up to be in both directions so we will only draw half of them
        List<Vector3[]> singleConnections = new List<Vector3[]>();

        foreach (NodeGraph.Node node in m_World.GridPathFinding.AbstractWorldGraph.Nodes)
        {
            Vector3 pos1 = new Vector3(node.pos.x + 0.5f, node.pos.y + 0.5f, 0);

            foreach (NodeGraph.Connection connection
                in m_World.GridPathFinding.AbstractWorldGraph.GetConnections(node))
            {
                Vector3 pos2 = new Vector3(connection.otherNode.pos.x + 0.5f,
                    connection.otherNode.pos.y + 0.5f, 0);

                if (singleConnections.Exists(x => (x[0] == pos1 && x[1] == pos2)
                    || (x[0] == pos2 && x[1] == pos1)))
                    continue;

                Vector3[] points = { pos1, pos2 };
                singleConnections.Add(points);
            }
        }

        //setting the points to the line renderers
        for (int i = 0; i < singleConnections.Count; i++)
        {
            m_ConnectionLines[i].GetComponent<LineRenderer>().SetPositions(singleConnections[i]);
            m_ConnectionLines[i].SetActive(true);
        }
    }
}
