using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGraphVisualizer : MonoBehaviour
{
    [SerializeField]
    GameObject m_GraphLayer = null;

    [SerializeField]
    GridPreProcessor m_GridPreProcessor = null;

    [SerializeField]
    GameObject m_ConnectionLinePrefab = null;

    List<GameObject> m_ConnectionLines = new List<GameObject>();

    [SerializeField]
    TileBase m_EntranceNodeTile = null;

    [SerializeField]
    TileBase m_ClusterBorderTile = null;

    private bool m_HasVisualized = false;
    public void SetVisualized()
    {
        m_HasVisualized = false;
    }

    private void Update()
    {
        if (m_HasVisualized || m_GridPreProcessor.Clusters == null)
            return;

        ShowPreProcessedGraph();

        m_HasVisualized = true;
    }

    private void ShowPreProcessedGraph()
    {
        foreach(GridPreProcessor.Cluster cluster in m_GridPreProcessor.Clusters)
        {
            ShowCluster(cluster);
        }

        ShowNodes();
        DrawConnections();
    }

    private void ShowCluster(GridPreProcessor.Cluster cluster)
    {
        for(int w = 0; w<m_GridPreProcessor.Clustersize; ++w)
        {
            for(int h = 0; h<m_GridPreProcessor.Clustersize; ++h)
            {
                if(w <= 0 || h<=0 || w>=m_GridPreProcessor.Clustersize-1 || h>= m_GridPreProcessor.Clustersize-1)
                {
                    m_GraphLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(cluster.pos.x + w, cluster.pos.y + h, 0)
                , m_ClusterBorderTile);
                }
            }
        }
    }

    private void ShowNodes()
    {
        for(int i = 0; i<m_GridPreProcessor.NodeGraph.Nodes.Count; ++i)
        {
            NodeGraph.Node node = m_GridPreProcessor.NodeGraph.Nodes[i];

            m_GraphLayer.GetComponent<Tilemap>().SetTile(new Vector3Int(node.pos.x, node.pos.y, 0)
                , m_EntranceNodeTile);
        }
    }

    private void DrawConnections()
    {
        //creating lines for the conections if there are not enough
        while (m_ConnectionLines.Count <= m_GridPreProcessor.NodeGraph.Connections.Count)
        {
            GameObject connectionLine = Instantiate(m_ConnectionLinePrefab);
            connectionLine.transform.SetParent(m_GraphLayer.transform);
            m_ConnectionLines.Add(connectionLine);
        }

        for(int i = 0; i< m_GridPreProcessor.NodeGraph.Connections.Count; i++)
        {
            NodeGraph.Connection connection = m_GridPreProcessor.NodeGraph.Connections[i];

            Vector3 pos1 = new Vector3(connection.leftNode.pos.x +0.5f, connection.leftNode.pos.y+0.5f, 0);
            Vector3 pos2 = new Vector3(connection.rightNode.pos.x+0.5f, connection.rightNode.pos.y+0.5f, 0);

            Vector3[] positions = { pos1, pos2 };

            m_ConnectionLines[i].GetComponent<LineRenderer>().SetPositions(positions);
        }
    }
}
