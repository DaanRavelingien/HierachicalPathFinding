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
        //disabeling all connections
        foreach (GameObject line in m_ConnectionLines)
            line.SetActive(false);

        //creating lines for the conections if there are not enough
        while (m_ConnectionLines.Count < m_GridPreProcessor.NodeGraph.Connections.Count/2)
        {
            GameObject connectionLine = Instantiate(m_ConnectionLinePrefab);
            connectionLine.transform.SetParent(m_GraphLayer.transform);
            connectionLine.SetActive(false);
            m_ConnectionLines.Add(connectionLine);
        }

        //the connections are doubled up to be in both directions so we will only draw half of them
        List<Vector3[]> singleConnections = new List<Vector3[]>();

        foreach(NodeGraph.Node node in m_GridPreProcessor.NodeGraph.Nodes)
        {
            Vector3 pos1 = new Vector3(node.pos.x + 0.5f, node.pos.y + 0.5f, 0);

            foreach (NodeGraph.Connection connection in m_GridPreProcessor.NodeGraph.GetConnections(node))
            {
                Vector3 pos2 = new Vector3(connection.otherNode.pos.x + 0.5f, connection.otherNode.pos.y + 0.5f, 0);

                if (singleConnections.Exists(x => (x[0] == pos1 && x[1] == pos2) 
                    || (x[0] == pos2 && x[1] == pos1)))
                    continue;

                Vector3[] points = { pos1, pos2 };
                singleConnections.Add(points);
            }
        }

        //setting the points to the line renderers
        for(int i = 0; i<singleConnections.Count; i++)
        {
            m_ConnectionLines[i].GetComponent<LineRenderer>().SetPositions(singleConnections[i]);
            m_ConnectionLines[i].SetActive(true);
        }
    }
}
