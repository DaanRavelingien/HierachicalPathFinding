using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPreProcessor : MonoBehaviour
{
    [SerializeField]
    private int m_ClusterResolution = 3;

    private int m_EntranceWidth = 3;

    private int m_ClusterSize = 9;
    public int Clustersize
    {
        get { return m_ClusterSize; }
        private set { m_ClusterSize = value; }
    }

    private NodeGraph m_NodeGraph = new NodeGraph();
    public NodeGraph NodeGraph
    {
        get { return m_NodeGraph; }
        private set { m_NodeGraph = value; }
    }

    public class Cluster
    {
        //bottomLeft
        public Vector2Int pos;
        public List<NodeGraph.Node> nodes;
    }

    private Cluster[,] m_Clusters = null;
    public Cluster[,] Clusters
    {
        get { return m_Clusters; }
        private set { m_Clusters = value; }
    }

    public void PreProcessingGrid(GridWorld.Cell[,] Cells, int gridSize)
    {
        m_Clusters = new Cluster[m_ClusterResolution, m_ClusterResolution];

        m_ClusterSize = gridSize / m_ClusterResolution;

        //creating the clusters
        for (int w = 0; w<m_ClusterResolution; ++w)
        {
            for (int h = 0; h< m_ClusterResolution; ++h)
            {
                Cluster cluster = new Cluster();
                cluster.pos = new Vector2Int(w * m_ClusterSize, h * m_ClusterSize);
                cluster.nodes = new List<NodeGraph.Node>();
                m_Clusters[w, h] = cluster;
            }
        }

        CreateEntrances(Cells);
        CreateClusterConnections();
   
    }

    public void PreProcessCluster(Vector2Int cell, GridWorld.Cell[,] cells)
    {
        //resetting the cluster
        Vector2Int clusterIdx = GetClusterIdx(cell);
        Cluster currentCluster = m_Clusters[clusterIdx.x, clusterIdx.y];
        ResetCluster(clusterIdx);

        //recalculating the cluster
        //handeling the left cluster
        if (clusterIdx.x - 1 >= 0)
        {
            Cluster leftNeighbourCluster = m_Clusters[clusterIdx.x - 1, clusterIdx.y];
            HandleEntrancesLeft(currentCluster, leftNeighbourCluster, cells);
        }

        //handeling the right cluster
        if (clusterIdx.x + 1 < m_ClusterResolution)
        {
            Cluster rightNeighbourCluster = m_Clusters[clusterIdx.x + 1, clusterIdx.y];
            HandleEntrancesRight(currentCluster, rightNeighbourCluster, cells);
        }
        //handeling the bottom cluster
        if (clusterIdx.y - 1 >= 0) 
        {
            Cluster bottomNeighbourCluster = m_Clusters[clusterIdx.x, clusterIdx.y - 1];
            HandleEntrancesBottom(currentCluster, bottomNeighbourCluster, cells);
        }
        //handeling the top cluster
        if (clusterIdx.y + 1 < m_ClusterResolution) 
        {
            Cluster TopNeighbourCluster = m_Clusters[clusterIdx.x, clusterIdx.y + 1];
            HandleEntrancesTop(currentCluster, TopNeighbourCluster, cells);
        }
        
        //rerendering the graph visualizer 
        GetComponentInChildren<WorldGraphVisualizer>().SetVisualized();
    }

    private struct Entrance
    {
        public Vector2Int startCell;
        public int widht;
    }

    private void CreateEntrances(GridWorld.Cell[,] Cells)
    {
        //going through all the clusters to find the entrance
        for(int w = 0; w<m_ClusterResolution; ++w)
        {
            for(int h=0; h<m_ClusterResolution; ++h)
            {
                //so first we will check the borders and see where there are openings
                Cluster currentCluster = m_Clusters[w, h];

                //we will only look for entrances between the top and right neighbour since we 
                //go through the clusters from bottom left to the top right in the grid

                //RightBorder
                if(w+1 < m_ClusterResolution)
                {
                    Cluster rightNeighbourCluster = m_Clusters[w + 1, h];
                    HandleEntrancesRight(currentCluster, rightNeighbourCluster, Cells);
                }

                //TopBorder
                if (h + 1 < m_ClusterResolution)
                {
                    Cluster topNeighbourCluster = m_Clusters[w, h + 1];
                    HandleEntrancesTop(currentCluster, topNeighbourCluster, Cells);
                }
            }
        }
    }

    private void ResetCluster(Vector2Int clusterIdx)
    {
        //we will store our removed nodein here so we can later remove them from
        //our node graph
        List<NodeGraph.Node> nodesToRemove = new List<NodeGraph.Node>();

        Cluster currentCluster = m_Clusters[clusterIdx.x, clusterIdx.y];

        foreach(NodeGraph.Node node in currentCluster.nodes)
        {
            //check connected nodes and checking what we need to delete
            foreach(NodeGraph.Connection conection in m_NodeGraph.GetConnections(node))
            {
                //we will first check if it has more then one connection if so we can ignore this node
                if (conection.otherNode.connections.Count > 1)
                    continue;

                //removing the node from neighbouring clusters
                //left
                if(GetClusterIdx(conection.otherNode.pos).x == clusterIdx.x-1 )
                {
                    m_Clusters[clusterIdx.x - 1, clusterIdx.y].nodes.RemoveAll(x=> x.pos == conection.otherNode.pos);
                }
                //right
                if (GetClusterIdx(conection.otherNode.pos).x == clusterIdx.x + 1)
                {
                    m_Clusters[clusterIdx.x + 1, clusterIdx.y].nodes.RemoveAll(x => x.pos == conection.otherNode.pos);
                }
                //bottom
                if (GetClusterIdx(conection.otherNode.pos).y == clusterIdx.y - 1)
                {
                    m_Clusters[clusterIdx.x, clusterIdx.y - 1].nodes.RemoveAll(x => x.pos == conection.otherNode.pos);
                }
                //top
                if (GetClusterIdx(conection.otherNode.pos).y == clusterIdx.y + 1)
                {
                    m_Clusters[clusterIdx.x, clusterIdx.y + 1].nodes.RemoveAll(x => x.pos == conection.otherNode.pos);
                }

                //removing the node from the nodegraph
                nodesToRemove.Add(m_NodeGraph.GetNode(conection.otherNode));
            }
        }

        //removing the nodes from the node network
        nodesToRemove.AddRange(currentCluster.nodes);
        m_NodeGraph.RemoveNodes(nodesToRemove);
        //clearing all the nodes of this cluster
        m_Clusters[clusterIdx.x, clusterIdx.y].nodes.Clear();
    }

    private void HandleEntrancesRight(Cluster currentCluster, Cluster rightNeighbourCluster
        , GridWorld.Cell[,] Cells)
    {

        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            GridWorld.Cell currentClusterCell
                = Cells[currentCluster.pos.x + m_ClusterSize-1, currentCluster.pos.y + i];
            GridWorld.Cell neighbourClusterCell
                = Cells[currentCluster.pos.x + m_ClusterSize, currentCluster.pos.y + i];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == GridWorld.CellType.wall
                || neighbourClusterCell.cellType == GridWorld.CellType.wall)
            {
                //if we already had an entrance add the nodes to the clusters
                if (currentEntrance.widht > 0)
                {
                    //checking the width of the entrance if the
                    //width is to big add to nodes on the ends of the entrance
                    //otherwise add the center cell of the entrance
                    if(currentEntrance.widht == 1)
                    {
                        //if the width is 1 then just take the start node as entrance node
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + 1
                            , currentEntrance.startCell.y),
                            currentCluster, rightNeighbourCluster);
                    }
                    else if (currentEntrance.widht <= m_EntranceWidth)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                            new Vector2Int(currentEntrance.startCell.x + 1
                            , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                            currentCluster, rightNeighbourCluster);
                    }
                    else
                    {
                        //adding the bottom side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + 1
                            , currentEntrance.startCell.y),
                            currentCluster, rightNeighbourCluster);

                        //adding the top side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + currentEntrance.widht - 1),
                            new Vector2Int(currentEntrance.startCell.x + 1
                            , currentEntrance.startCell.y + currentEntrance.widht - 1),
                            currentCluster, rightNeighbourCluster);
                    }
                }


                currentEntrance.widht = 0;
                continue;
            }

            //if the cell is not a wall and we dont have
            //an entrance yet set it as the start of a new entrance
            if (currentEntrance.widht <= 0)
                currentEntrance.startCell = currentClusterCell.pos;

            //if we are already creating an entrance keep track of the width
            currentEntrance.widht++;
        }

        //checking if we still have an entrance that needs to be added
        if (currentEntrance.widht <= 0)
            return;

        if (currentEntrance.widht == 1)
        {
            //if the width is 1 then just take the start node as entrance node
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + 1
                , currentEntrance.startCell.y),
                currentCluster, rightNeighbourCluster);
        }
        else if (currentEntrance.widht <= m_EntranceWidth)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                new Vector2Int(currentEntrance.startCell.x + 1
                , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                currentCluster, rightNeighbourCluster);
        }
        else
        {
            //adding the bottom side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + 1
                , currentEntrance.startCell.y),
                currentCluster, rightNeighbourCluster);

            //adding the top side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + currentEntrance.widht - 1),
                new Vector2Int(currentEntrance.startCell.x + 1
                , currentEntrance.startCell.y + currentEntrance.widht - 1),
                currentCluster, rightNeighbourCluster);
        }
    }

    private void HandleEntrancesLeft(Cluster currentCluster, Cluster leftNeighbourCluster
        , GridWorld.Cell[,] Cells)
    {

        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            GridWorld.Cell currentClusterCell
                = Cells[currentCluster.pos.x, currentCluster.pos.y + i];
            GridWorld.Cell neighbourClusterCell
                = Cells[currentCluster.pos.x - 1, currentCluster.pos.y + i];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == GridWorld.CellType.wall
                || neighbourClusterCell.cellType == GridWorld.CellType.wall)
            {
                //if we already had an entrance add the nodes to the clusters
                if (currentEntrance.widht > 0)
                {
                    //checking the width of the entrance if the
                    //width is to big add to nodes on the ends of the entrance
                    //otherwise add the center cell of the entrance
                    if (currentEntrance.widht == 1)
                    {
                        //if the width is 1 then just take the start node as entrance node
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x - 1
                            , currentEntrance.startCell.y),
                            currentCluster, leftNeighbourCluster);
                    }
                    else if (currentEntrance.widht <= m_EntranceWidth)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                            new Vector2Int(currentEntrance.startCell.x - 1
                            , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                            currentCluster, leftNeighbourCluster);
                    }
                    else
                    {
                        //adding the bottom side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x - 1
                            , currentEntrance.startCell.y),
                            currentCluster, leftNeighbourCluster);

                        //adding the top side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + currentEntrance.widht - 1),
                            new Vector2Int(currentEntrance.startCell.x - 1
                            , currentEntrance.startCell.y + currentEntrance.widht - 1),
                            currentCluster, leftNeighbourCluster);
                    }
                }


                currentEntrance.widht = 0;
                continue;
            }

            //if the cell is not a wall and we dont have
            //an entrance yet set it as the start of a new entrance
            if (currentEntrance.widht <= 0)
                currentEntrance.startCell = currentClusterCell.pos;

            //if we are already creating an entrance keep track of the width
            currentEntrance.widht++;
        }

        //checking if we still have an entrance that needs to be added
        if (currentEntrance.widht <= 0)
            return;

        if (currentEntrance.widht == 1)
        {
            //if the width is 1 then just take the start node as entrance node
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x - 1
                , currentEntrance.startCell.y),
                currentCluster, leftNeighbourCluster);
        }
        else if (currentEntrance.widht <= m_EntranceWidth)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                new Vector2Int(currentEntrance.startCell.x - 1
                , currentEntrance.startCell.y + (m_EntranceWidth / 2)),
                currentCluster, leftNeighbourCluster);
        }
        else
        {
            //adding the bottom side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x - 1
                , currentEntrance.startCell.y),
                currentCluster, leftNeighbourCluster);

            //adding the top side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + currentEntrance.widht - 1),
                new Vector2Int(currentEntrance.startCell.x - 1
                , currentEntrance.startCell.y + currentEntrance.widht - 1),
                currentCluster, leftNeighbourCluster);
        }
    }

    private void HandleEntrancesTop(Cluster currentCluster, Cluster topNeighbourCluster
        , GridWorld.Cell[,] Cells)
    {
        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            GridWorld.Cell currentClusterCell
                = Cells[currentCluster.pos.x + i, currentCluster.pos.y + m_ClusterSize - 1];
            GridWorld.Cell neighbourClusterCell
                = Cells[currentCluster.pos.x + i, currentCluster.pos.y + m_ClusterSize];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == GridWorld.CellType.wall
                || neighbourClusterCell.cellType == GridWorld.CellType.wall)
            {
                //if we already had an entrance add the nodes to the clusters
                if (currentEntrance.widht > 0)
                {
                    //checking the width of the entrance if the
                    //width is to big add to nodes on the ends of the entrance
                    //otherwise add the center cell of the entrance

                    if (currentEntrance.widht == 1)
                    {
                        //if the width is 1 then just take the start node as entrance node
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + 1),
                            currentCluster, topNeighbourCluster);
                    }
                    else if (currentEntrance.widht <= m_EntranceWidth)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                            , currentEntrance.startCell.y + 1),
                            currentCluster, topNeighbourCluster);
                    }
                    else
                    {
                        //adding the bottom side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + 1),
                            currentCluster, topNeighbourCluster);

                        //adding the top side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                            , currentEntrance.startCell.y + 1),
                            currentCluster, topNeighbourCluster);
                    }
                }


                currentEntrance.widht = 0;
                continue;
            }

            //if the cell is not a wall and we dont have
            //an entrance yet set it as the start of a new entrance
            if (currentEntrance.widht <= 0)
            {
                currentEntrance.startCell = currentClusterCell.pos;
            }

            //if we are already creating an entrance keep track of the width
            currentEntrance.widht++;

        }

        //checking if we still have an entrance that needs to be added
        if (currentEntrance.widht <= 0)
            return;

        if (currentEntrance.widht == 1)
        {
            //if the width is 1 then just take the start node as entrance node
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + 1),
                currentCluster, topNeighbourCluster);
        }
        else if (currentEntrance.widht <= m_EntranceWidth)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                , currentEntrance.startCell.y + 1),
                currentCluster, topNeighbourCluster);
        }
        else
        {
            //adding the bottom side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + 1),
                currentCluster, topNeighbourCluster);

            //adding the top side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                , currentEntrance.startCell.y + 1),
                currentCluster, topNeighbourCluster);
        }
    }

    private void HandleEntrancesBottom(Cluster currentCluster, Cluster bottomNeighbourCluster
        , GridWorld.Cell[,] Cells)
    {
        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            GridWorld.Cell currentClusterCell
                = Cells[currentCluster.pos.x + i, currentCluster.pos.y];
            GridWorld.Cell neighbourClusterCell
                = Cells[currentCluster.pos.x + i, currentCluster.pos.y-1];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == GridWorld.CellType.wall
                || neighbourClusterCell.cellType == GridWorld.CellType.wall)
            {
                //if we already had an entrance add the nodes to the clusters
                if (currentEntrance.widht > 0)
                {
                    //checking the width of the entrance if the
                    //width is to big add to nodes on the ends of the entrance
                    //otherwise add the center cell of the entrance

                    if (currentEntrance.widht == 1)
                    {
                        //if the width is 1 then just take the start node as entrance node
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y - 1),
                            currentCluster, bottomNeighbourCluster);
                    }
                    else if (currentEntrance.widht <= m_EntranceWidth)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                            , currentEntrance.startCell.y - 1),
                            currentCluster, bottomNeighbourCluster);
                    }
                    else
                    {
                        //adding the bottom side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y -1),
                            currentCluster, bottomNeighbourCluster);

                        //adding the top side of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                            , currentEntrance.startCell.y - 1),
                            currentCluster, bottomNeighbourCluster);
                    }
                }


                currentEntrance.widht = 0;
                continue;
            }

            //if the cell is not a wall and we dont have
            //an entrance yet set it as the start of a new entrance
            if (currentEntrance.widht <= 0)
            {
                currentEntrance.startCell = currentClusterCell.pos;
            }

            //if we are already creating an entrance keep track of the width
            currentEntrance.widht++;

        }

        //checking if we still have an entrance that needs to be added
        if (currentEntrance.widht <= 0)
            return;

        if (currentEntrance.widht == 1)
        {
            //if the width is 1 then just take the start node as entrance node
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y - 1),
                currentCluster, bottomNeighbourCluster);
        }
        else if (currentEntrance.widht <= m_EntranceWidth)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + (m_EntranceWidth / 2)
                , currentEntrance.startCell.y - 1),
                currentCluster, bottomNeighbourCluster);
        }
        else
        {
            //adding the bottom side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y - 1),
                currentCluster, bottomNeighbourCluster);

            //adding the top side of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + currentEntrance.widht - 1
                , currentEntrance.startCell.y - 1),
                currentCluster, bottomNeighbourCluster);
        }
    }

    private void AddEntrancesToClusters(Vector2Int entrancePos1, Vector2Int entrancePos2
        , Cluster cluster1, Cluster cluster2)
    {
        NodeGraph.Node node1 = new NodeGraph.Node(entrancePos1);
        NodeGraph.Node node2 = new NodeGraph.Node(entrancePos2);

        if (!m_NodeGraph.Nodes.Exists(x=>x.pos == entrancePos1))
        { 
            cluster1.nodes.Add(m_NodeGraph.AddNode(node1));
        }

        if (!m_NodeGraph.Nodes.Exists(x => x.pos == entrancePos2))
        {
            cluster2.nodes.Add(m_NodeGraph.AddNode(node2));
        }

        m_NodeGraph.AddConnection(m_NodeGraph.GetNode(node1), m_NodeGraph.GetNode(node2), 1);
        m_NodeGraph.AddConnection(m_NodeGraph.GetNode(node2), m_NodeGraph.GetNode(node1), 1);
    }

    //get the cluster idx where the cell is in
    private Vector2Int GetClusterIdx(Vector2 cell)
    {
        Vector2Int clusterIdx = new Vector2Int();

        clusterIdx.x = (int)cell.x / m_ClusterSize;
        clusterIdx.y = (int)cell.y / m_ClusterSize;

        return clusterIdx;
    }

    private Vector2Int GetClusterIdx(Vector2Int cell)
    {
        Vector2Int clusterIdx = new Vector2Int();

        clusterIdx.x = cell.x / m_ClusterSize;
        clusterIdx.y = cell.y / m_ClusterSize;

        return clusterIdx;
    }


    private void CreateClusterConnections()
    {
        
    }
}
