using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPreProcessor : MonoBehaviour
{
    [SerializeField]
    private int m_ClusterResolution = 3;

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

    public struct Cluster
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

    private void Awake()
    {
        m_Clusters = new Cluster[m_ClusterResolution, m_ClusterResolution];
    }

    public void PreProcessingGrid(WorldGenerator.Cell[,] grid, int gridSize)
    {
        int entranceMinWidth = 3;

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

        CreateEntrances(grid, entranceMinWidth);
        CreateClusterConnections();
   
    }

    private struct Entrance
    {
        public Vector2Int startCell;
        public int widht;
    }

    private void CreateEntrances(WorldGenerator.Cell[,] grid, int entranceminWidht)
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
                    HandleEntrancesRight(currentCluster, rightNeighbourCluster, entranceminWidht, grid);
                }

                //TopBorder
                if (h + 1 < m_ClusterResolution)
                {
                    Cluster topNeighbourCluster = m_Clusters[w, h + 1];
                    HandleEntrancesTop(currentCluster, topNeighbourCluster, entranceminWidht, grid);
                }
            }
        }
    }

    private void HandleEntrancesRight(Cluster currentCluster, Cluster rightNeighbourCluster,
        int entranceminWidht, WorldGenerator.Cell[,] grid)
    {

        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            WorldGenerator.Cell currentClusterCell
                = grid[currentCluster.pos.x + m_ClusterSize-1, currentCluster.pos.y + i];
            WorldGenerator.Cell neighbourClusterCell
                = grid[currentCluster.pos.x + m_ClusterSize, currentCluster.pos.y + i];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == WorldGenerator.CellType.wall
                || neighbourClusterCell.cellType == WorldGenerator.CellType.wall)
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
                    else if (currentEntrance.widht <= entranceminWidht)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                            , currentEntrance.startCell.y + (entranceminWidht / 2)),
                            new Vector2Int(currentEntrance.startCell.x + 1
                            , currentEntrance.startCell.y + (entranceminWidht / 2)),
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
        else if (currentEntrance.widht <= entranceminWidht)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x
                , currentEntrance.startCell.y + (entranceminWidht / 2)),
                new Vector2Int(currentEntrance.startCell.x + 1
                , currentEntrance.startCell.y + (entranceminWidht / 2)),
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

    private void HandleEntrancesTop(Cluster currentCluster, Cluster topNeighbourCluster,
        int entranceminWidht, WorldGenerator.Cell[,] grid)
    {
        Entrance currentEntrance = new Entrance();

        //going through the cells bordering the 2 clusters
        for (int i = 0; i < m_ClusterSize; ++i)
        {
            WorldGenerator.Cell currentClusterCell
                = grid[currentCluster.pos.x + i, currentCluster.pos.y + m_ClusterSize-1];
            WorldGenerator.Cell neighbourClusterCell
                = grid[currentCluster.pos.x + i, currentCluster.pos.y + m_ClusterSize];

            //check if the current cell is a wall or if the neighbouring cell is a wall
            if (currentClusterCell.cellType == WorldGenerator.CellType.wall
                || neighbourClusterCell.cellType == WorldGenerator.CellType.wall)
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
                    else if (currentEntrance.widht <= entranceminWidht)
                    {
                        //adding the middle of the entrance to the cluster
                        AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (entranceminWidht / 2)
                            , currentEntrance.startCell.y),
                            new Vector2Int(currentEntrance.startCell.x + (entranceminWidht / 2)
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
        else if (currentEntrance.widht <= entranceminWidht)
        {
            //adding the middle of the entrance to the cluster
            AddEntrancesToClusters(new Vector2Int(currentEntrance.startCell.x + (entranceminWidht / 2)
                , currentEntrance.startCell.y),
                new Vector2Int(currentEntrance.startCell.x + (entranceminWidht / 2)
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

    private void AddEntrancesToClusters(Vector2Int entrancePos1, Vector2Int entrancePos2, Cluster cluster1, Cluster cluster2)
    {
        NodeGraph.Node node1 = new NodeGraph.Node(entrancePos1);
        m_NodeGraph.AddNode(node1);
        cluster1.nodes.Add(node1);

        NodeGraph.Node node2 = new NodeGraph.Node(entrancePos2);
        m_NodeGraph.AddNode(node2);
        cluster2.nodes.Add(node2);

        NodeGraph.Connection connection = new NodeGraph.Connection(1, node1, node2);
        m_NodeGraph.AddConnection(connection);

        node1.connections.Add(connection);
        node2.connections.Add(connection);
    }



    private void CreateClusterConnections()
    {

    }
}
