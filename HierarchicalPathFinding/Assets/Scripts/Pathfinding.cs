using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//pathfinding on a grid
public class Pathfinding
{
    //A* pathfinding variables
    //-----------------------------

    Vector4 m_SearchRect = new Vector4();
    public Vector4 SearchRect
    {
        get { return m_SearchRect; }
        set { m_SearchRect = value; }
    }

    struct CellRecord
    {
        public Vector2Int cellPos;
        public Vector2Int? parentCell;
        public float costSoFar; // accumulated g-costs of all the connections leading up to this one
        public float estimatedTotalCost; // f-cost (= costSoFar + h-cost)
    }

    struct NodeRecord
    {
        public NodeGraph.Node node;
        public NodeGraph.Node parentNode;
        public float costSoFar; // accumulated g-costs of all the connections leading up to this one
        public float estimatedTotalCost; // f-cost (= costSoFar + h-cost)
    }

    //hirachical path finding variables
    //---------------------------------
    private int m_ClusterResolution = 27;

    private int m_EntranceWidth = 3;

    private int m_ClusterSize = 9;
    public int Clustersize
    {
        get { return m_ClusterSize; }
        private set { m_ClusterSize = value; }
    }

    private NodeGraph m_AbstractWorldGraph = new NodeGraph();
    public NodeGraph AbstractWorldGraph
    {
        get { return m_AbstractWorldGraph; }
        private set { m_AbstractWorldGraph = value; }
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

    private struct Entrance
    {
        public Vector2Int startCell;
        public int widht;
    }

    //general pathfinding functions
    //-----------------------------
    public void SetSizeToWorld(GridWorld world)
    {
        m_SearchRect = new Vector4(0, 0, world.WorldSize.x, world.WorldSize.y);
    }

    private bool IsValid(Vector2Int cell)
    {
        if (cell.x >= m_SearchRect.x && cell.y >= m_SearchRect.y
            && cell.x < m_SearchRect.z && cell.y < m_SearchRect.w)
            return true;
        return false;
    }

    //A* pathfinding functions
    //------------------------
    public List<Vector2Int> FindPathAStarOnGrid(GridWorld world, Vector2Int startPos, Vector2Int goalPos)
    {
        if (!IsValid(startPos))
        {
            return new List<Vector2Int>();
        }
        if (!IsValid(goalPos))
        {
            return new List<Vector2Int>();
        }
        if (startPos == goalPos)
        {
            return new List<Vector2Int>();
        }
        if (world.Cells[startPos.x, startPos.y].cellType == GridWorld.CellType.wall
            || world.Cells[goalPos.x, goalPos.y].cellType == GridWorld.CellType.wall)
        {
            return new List<Vector2Int>();
        }

        List<CellRecord> closedCells = new List<CellRecord>();
        List<CellRecord> openCells = new List<CellRecord>();
        CellRecord currentCellRecord = new CellRecord();


        //setting values to start our A* pathfinding
        currentCellRecord.cellPos = startPos;
        currentCellRecord.parentCell = null;
        currentCellRecord.costSoFar = 0;
        currentCellRecord.estimatedTotalCost = CalcHeuristicCost(startPos, goalPos);
        openCells.Add(currentCellRecord);

        //keep searching until the goal if found
        while (openCells.Count > 0)
        {
            //find the cell with the lowest cost in the open list
            currentCellRecord.estimatedTotalCost = float.MaxValue;
            foreach(CellRecord cellRecord in openCells)
            {
                if (currentCellRecord.estimatedTotalCost > cellRecord.estimatedTotalCost)
                    currentCellRecord = cellRecord;
            }

            //checking if this cell is the goal if so exit the loop
            if (currentCellRecord.cellPos == goalPos)
                break;

            //removing the cell from the open list and adding it to the closed list
            openCells.Remove(currentCellRecord);
            closedCells.Add(currentCellRecord);

            //looping over all the neighbours of our current cell
            List<Vector2Int> neighbours = GenerateNeighbours(world, currentCellRecord.cellPos);
            foreach (Vector2Int cellPos in neighbours)
            {
                CellRecord newCellRecord = new CellRecord();
                newCellRecord.cellPos = cellPos;
                newCellRecord.parentCell = currentCellRecord.cellPos;
                //the connection cost between 2 cells is just 1
                newCellRecord.costSoFar = currentCellRecord.costSoFar + 1;
                newCellRecord.estimatedTotalCost = newCellRecord.costSoFar + CalcHeuristicCost(cellPos, goalPos);

                //if a cell with the same pos is in the open list with a lower costSoFar skip this cell
                if (openCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;

                //if a cell with the same pos is in the closed list and has a lower costSoFar
                //skip this cell, otherwise add the cell to the open list
                if (closedCells.Exists(x => x.cellPos == newCellRecord.cellPos
                     && x.costSoFar <= newCellRecord.costSoFar))
                    continue;
                
                openCells.Add(newCellRecord);                
            }
        }

        //if no path was found just return an empty list
        if (currentCellRecord.cellPos != goalPos)
            return new List<Vector2Int>();                    

        //tracing our path back
        List<Vector2Int> pathCells = new List<Vector2Int>();
        pathCells.Add(currentCellRecord.cellPos);

        while (currentCellRecord.parentCell != null)
        {
            currentCellRecord = closedCells.Find(x => x.cellPos == currentCellRecord.parentCell);
            pathCells.Add(currentCellRecord.cellPos);
        }
        //adding the start as well since it is not in the closedCell list
        pathCells.Add(startPos);

        //reversing our path so our start is at the front an goal at the back of the list
        pathCells.Reverse();

        return pathCells;
    }

    public List<NodeGraph.Node> FindPathAStarOnGraph(NodeGraph graph, NodeGraph.Node startNode, NodeGraph.Node goalNode)
    {
        if (!graph.IsValid(startNode))
        {
            return new List<NodeGraph.Node>();
        }
        if (!graph.IsValid(goalNode))
        {
            return new List<NodeGraph.Node>();
        }
        if (startNode.pos == goalNode.pos)
        {
            return new List<NodeGraph.Node>();
        }

        List<NodeRecord> closedList = new List<NodeRecord>();
        List<NodeRecord> openList = new List<NodeRecord>();
        NodeRecord currentNodeRecord = new NodeRecord();

        //setting values to start our A* pathfinding
        currentNodeRecord.node = startNode;
        currentNodeRecord.parentNode = null;
        currentNodeRecord.costSoFar = 0;
        currentNodeRecord.estimatedTotalCost = CalcHeuristicCost(startNode.pos, goalNode.pos);
        openList.Add(currentNodeRecord);

        //keep searching until the goal is found
        while (openList.Count > 0)
        {
            //find the node with the lowest cost in the open list
            currentNodeRecord.estimatedTotalCost = float.MaxValue;
            foreach (NodeRecord nodeRecord in openList)
            {
                if (currentNodeRecord.estimatedTotalCost > nodeRecord.estimatedTotalCost)
                    currentNodeRecord = nodeRecord;
            }

            //checking if this node is the goal if so exit the loop
            if (currentNodeRecord.node == goalNode)
                break;

            //removing the node from the open list and adding it to the closed list
            openList.Remove(currentNodeRecord);
            closedList.Add(currentNodeRecord);

            //looping over all the connected nodes of our current node
            foreach (NodeGraph.Connection connection in currentNodeRecord.node.connections)
            {
                NodeRecord newNodeRecord = new NodeRecord();
                newNodeRecord.node = connection.otherNode;
                newNodeRecord.parentNode = currentNodeRecord.node;
                newNodeRecord.costSoFar = currentNodeRecord.costSoFar + connection.weight;
                newNodeRecord.estimatedTotalCost = newNodeRecord.costSoFar + CalcHeuristicCost(connection.otherNode.pos, goalNode.pos);

                //if a node with the same pos is in the open list with a lower costSoFar skip this node
                if (openList.Exists(x => x.node.pos == newNodeRecord.node.pos
                     && x.costSoFar <= newNodeRecord.costSoFar))
                    continue;

                //if a node with the same pos is in the closed list and has a lower costSoFar
                //skip this node, otherwise add the node to the open list
                if (closedList.Exists(x => x.node.pos == newNodeRecord.node.pos
                     && x.costSoFar <= newNodeRecord.costSoFar))
                    continue;

                openList.Add(newNodeRecord);
            }
        }

        //if no path was found just return an empty list
        if (currentNodeRecord.node.pos != goalNode.pos)
            return new List<NodeGraph.Node>();

        //tracing our path back
        List<NodeGraph.Node> pathPositions = new List<NodeGraph.Node>();
        pathPositions.Add(currentNodeRecord.node);

        while (currentNodeRecord.parentNode != null)
        {
            currentNodeRecord = closedList.Find(x => x.node.pos == currentNodeRecord.parentNode.pos);
            pathPositions.Add(currentNodeRecord.node);
        }
        //adding the start as well since it is not in the closedlist
        //pathPositions.Add(startNode);

        //reversing our path so our start is at the front an goal at the back of the list
        pathPositions.Reverse();

        return pathPositions;
    }

    //distance
    private float CalcHeuristicCost(Vector2 startCell, Vector2 goalCell)
    {
        float distance = (startCell - goalCell).magnitude;
        return distance;
    }

    //get the positions around one cell will return the 8 surrounding positions
    private List<Vector2Int> GenerateNeighbours(GridWorld world, Vector2Int centerCellPos)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        for(int i = 0; i< 3; ++i)
        {
            for(int j=0; j<3; ++j)
            {
                //skip the center cell
                if (i == 1 && j == 1)
                    continue;

                Vector2Int neighbourPos = new Vector2Int(i + centerCellPos.x - 1, j + centerCellPos.y - 1);

                //also skip the cell if it is not valid
                if (!IsValid(neighbourPos))
                    continue;

                //add the neighbour if its a tile that can be walked upon
                if (world.Cells[neighbourPos.x,neighbourPos.y].cellType == GridWorld.CellType.ground)
                neighbours.Add(neighbourPos);
            }
        }       

        return neighbours;
    }

    public float CalculatePathWeight(List<Vector2Int> path)
    {
        //check if the given path is valid
        if (path.Count < 0)
            return 0.0f;

        float totalCostOfPath = 0;

        for(int i = 1; i < path.Count; i++)
        {
            totalCostOfPath += CalcHeuristicCost(path[i - 1], path[i]);
        }

        return totalCostOfPath;
    }

    //Hirarchical Pathfinding functions
    //---------------------------------
    public List<Vector2Int> FindPathHirarchicalAStar(GridWorld world, Vector2Int start, Vector2Int goal)
    {
        //checking if we have a pre processed graph of our world
        if(m_AbstractWorldGraph.Nodes.Count == 0)
        {
            Debug.LogError("No Abstract graph to pathfind on was found");
            return new List<Vector2Int>();
        }

        //adding the start and goal node to the graph
        NodeGraph.Node startNode = new NodeGraph.Node(start);
        NodeGraph.Node goalNode = new NodeGraph.Node(goal);
        m_AbstractWorldGraph.AddNode(startNode);
        m_AbstractWorldGraph.AddNode(goalNode);

        Cluster startCluster = m_Clusters[GetClusterIdx(start).x,GetClusterIdx(start).y];
        Cluster goalCluster = m_Clusters[GetClusterIdx(goal).x, GetClusterIdx(goal).y];

        //if both the start and end node are in the same cluster then just do A* pathfinding
        if (startCluster == goalCluster)
            return FindPathAStarOnGrid(world, start, goal);

        //connect the start node to all the entrances in the start cluster
        //set the limit of pathfinding to the start cluster
        m_SearchRect = new Vector4(startCluster.pos.x, startCluster.pos.y
            , startCluster.pos.x + m_ClusterSize, startCluster.pos.y + m_ClusterSize);

        foreach (NodeGraph.Node entrance in startCluster.nodes)
        {
            //check if the start node can reach the entrance node
            List<Vector2Int> path = FindPathAStarOnGrid(world, start, entrance.pos);
            if (path.Count > 0)
            {
                float weight = CalculatePathWeight(path);
                m_AbstractWorldGraph.AddConnection(startNode, entrance, weight);
                m_AbstractWorldGraph.AddConnection(entrance, startNode, weight);
            }
        }

        //connect the goal node to all the entrances in the goal cluster
        //set the limit of pathfinding to the goal cluster
        m_SearchRect = new Vector4(goalCluster.pos.x, goalCluster.pos.y
            , goalCluster.pos.x + m_ClusterSize, goalCluster.pos.y + m_ClusterSize);

        foreach (NodeGraph.Node entrance in goalCluster.nodes)
        {
            //check if the goal node can reach the entrance node
            List<Vector2Int> path = FindPathAStarOnGrid(world, goal, entrance.pos);
            if (path.Count > 0)
            {
                float weight = CalculatePathWeight(path);
                m_AbstractWorldGraph.AddConnection(goalNode, entrance, weight);
                m_AbstractWorldGraph.AddConnection(entrance, goalNode, weight);
            }
        }

        //setting the pathfinding size back to the size of the world
        SetSizeToWorld(world);

        //do an A* search though the abstract world graph
        List<NodeGraph.Node> graphPath = FindPathAStarOnGraph(m_AbstractWorldGraph, startNode, goalNode);

        //removing the start and goal node from the graph once we got the path
        m_AbstractWorldGraph.RemoveNode(startNode);
        m_AbstractWorldGraph.RemoveNode(goalNode);

        //converting the path through the graph to a path on the grid
        List<Vector2Int> gridPath = new List<Vector2Int>();

        for(int i = 0; i<graphPath.Count-1;++i)
        {
            List<Vector2Int> path = FindPathAStarOnGrid(world, graphPath[i].pos, graphPath[i + 1].pos);
            foreach(Vector2Int pos in path)
            {
                gridPath.Add(pos);
            }
            //always removing the last element to avoid duplicates
            gridPath.RemoveAt(gridPath.Count - 1);
        }
        //adding the goal bakc to the list since we always remove the last element
        gridPath.Add(goal);

        return gridPath;
    }

    public void PreProcessingGrid(GridWorld world, GridWorld.Cell[,] Cells, int gridSize)
    {
        m_Clusters = new Cluster[m_ClusterResolution, m_ClusterResolution];

        m_ClusterSize = gridSize / m_ClusterResolution;

        //creating the clusters
        for (int w = 0; w < m_ClusterResolution; ++w)
        {
            for (int h = 0; h < m_ClusterResolution; ++h)
            {
                Cluster cluster = new Cluster();
                cluster.pos = new Vector2Int(w * m_ClusterSize, h * m_ClusterSize);
                cluster.nodes = new List<NodeGraph.Node>();
                m_Clusters[w, h] = cluster;
            }
        }

        CreateEntrances(Cells);

        for (int w = 0; w < m_ClusterResolution; ++w)
        {
            for (int h = 0; h < m_ClusterResolution; ++h)
            {
                CreateClusterConnections(world, new Vector2Int(w, h));
            }
        }
    }

    public void PreProcessCluster(GridWorld world, Vector2Int cell, GridWorld.Cell[,] cells)
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

        //handeling the inner connections of our cluster
        CreateClusterConnections(world, clusterIdx);
    }

    private void CreateEntrances(GridWorld.Cell[,] Cells)
    {
        //going through all the clusters to find the entrance
        for (int w = 0; w < m_ClusterResolution; ++w)
        {
            for (int h = 0; h < m_ClusterResolution; ++h)
            {
                //so first we will check the borders and see where there are openings
                Cluster currentCluster = m_Clusters[w, h];

                //we will only look for entrances between the top and right neighbour since we 
                //go through the clusters from bottom left to the top right in the grid

                //RightBorder
                if (w + 1 < m_ClusterResolution)
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

        foreach (NodeGraph.Node node in currentCluster.nodes)
        {
            //check connected nodes and checking what we need to delete
            foreach (NodeGraph.Connection conection in m_AbstractWorldGraph.GetConnections(node))
            {
                //we will first check if it has more then one connection if so we can ignore this node
                if (conection.otherNode.connections.Count > 1)
                    continue;

                //removing the node from neighbouring clusters
                //left
                if (GetClusterIdx(conection.otherNode.pos).x == clusterIdx.x - 1)
                {
                    m_Clusters[clusterIdx.x - 1, clusterIdx.y].nodes.RemoveAll(x => x.pos == conection.otherNode.pos);
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
                nodesToRemove.Add(m_AbstractWorldGraph.GetNode(conection.otherNode));
            }
        }

        //removing the nodes from the node network
        nodesToRemove.AddRange(currentCluster.nodes);
        m_AbstractWorldGraph.RemoveNodes(nodesToRemove);
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
                = Cells[currentCluster.pos.x + m_ClusterSize - 1, currentCluster.pos.y + i];
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
                = Cells[currentCluster.pos.x + i, currentCluster.pos.y - 1];

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

        if (!m_AbstractWorldGraph.Nodes.Exists(x => x.pos == entrancePos1))
        {
            cluster1.nodes.Add(m_AbstractWorldGraph.AddNode(node1));
        }

        if (!m_AbstractWorldGraph.Nodes.Exists(x => x.pos == entrancePos2))
        {
            cluster2.nodes.Add(m_AbstractWorldGraph.AddNode(node2));
        }

        m_AbstractWorldGraph.AddConnection(m_AbstractWorldGraph.GetNode(node1), m_AbstractWorldGraph.GetNode(node2), 1);
        m_AbstractWorldGraph.AddConnection(m_AbstractWorldGraph.GetNode(node2), m_AbstractWorldGraph.GetNode(node1), 1);
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

    private struct innerClusterConnection
    {
        public NodeGraph.Node node1;
        public NodeGraph.Node node2;
        public float weight;
    }
    private void CreateClusterConnections(GridWorld world, Vector2Int clusterIdx)
    {
        Cluster cluster = m_Clusters[clusterIdx.x, clusterIdx.y];

        //limiting the pathfinding to the size of the cluster
        m_SearchRect = new Vector4(cluster.pos.x, cluster.pos.y
            , cluster.pos.x + m_ClusterSize, cluster.pos.y + m_ClusterSize);

        List<innerClusterConnection> checkedNodeConnections = new List<innerClusterConnection>();

        //going through all the possible node connections and deciding their weights
        foreach (NodeGraph.Node node in cluster.nodes)
        {
            foreach (NodeGraph.Node otherNode in cluster.nodes)
            {
                //check if the node is itself
                if (node.pos == otherNode.pos)
                    continue;

                //check if the connection was already checked
                if (checkedNodeConnections.Exists(x =>
                 (x.node1.pos == node.pos && x.node2.pos == otherNode.pos)
                 || (x.node1.pos == otherNode.pos && x.node2.pos == node.pos)))
                    continue;



                //calculate the weight of the node connection
                List<Vector2Int> path = FindPathAStarOnGrid(world, node.pos, otherNode.pos);
                float pathWeight = CalculatePathWeight(path);

                //check if a path is possible
                if (pathWeight <= 0)
                    continue;

                //add the connection to the already checked ones
                innerClusterConnection checkedConnection = new innerClusterConnection();
                checkedConnection.node1 = node;
                checkedConnection.node2 = otherNode;
                checkedConnection.weight = pathWeight;
                checkedNodeConnections.Add(checkedConnection);
            }
        }

        //adding the connections to the world graph for this cluster
        foreach (innerClusterConnection innerConnection in checkedNodeConnections)
        {
            //add connection in both directions
            m_AbstractWorldGraph.AddConnection(innerConnection.node1, innerConnection.node2, innerConnection.weight);
            m_AbstractWorldGraph.AddConnection(innerConnection.node2, innerConnection.node1, innerConnection.weight);
        }


        //setting the pathfinding size back to the size of the world
        SetSizeToWorld(world);
    }
}
