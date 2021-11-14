using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGraph
{
    public struct Connection
    {
        public Connection(float weight, Node left, Node right) 
        {
            this.weight = weight;
            this.leftNode = left;
            this.rightNode = right;
        }

        public float weight;
        public Node leftNode;
        public Node rightNode;
    }
    public struct Node
    {
        public Node(Vector2Int pos)
        {
            this.pos = pos;
            this.connections = new List<Connection>();
        }

        public Vector2Int pos;
        public List<Connection> connections;
    }

    private List<Node> m_Nodes = new List<Node>();
    public List<Node> Nodes
    {
        get { return m_Nodes; }
        private set { m_Nodes = value; }
    }

    private List<Connection> m_Connections = new List<Connection>();

    public List<Connection> Connections
    {
        get { return m_Connections; }
        private set { m_Connections = value; }
    }

    public void AddNode(Node node)
    {
        m_Nodes.Add(node);
    }

    public void AddConnection(Connection connection)
    {
        m_Connections.Add(connection);
    }
}
