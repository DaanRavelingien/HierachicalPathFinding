using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGraph
{
    public class Connection
    {
        public Connection(float weight, Node otherNode) 
        {
            this.weight = weight;
            this.otherNode = otherNode;
        }

        public float weight;
        public Node otherNode;
    }
    public class Node
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

    public Node AddNode(Node node)
    {
        m_Nodes.Add(node);
        return node;
    }

    public Node GetNode(Node node)
    {
        if(!IsValid(node))
        {
            Debug.LogWarning("Node does not exist");
            return null;
        }

        return m_Nodes.Find(x => x.pos == node.pos);
    }

    public List<Connection> GetConnections(Node node)
    {
        return GetNode(node).connections;
    }

    public void AddConnection(Node currentNode, Node otherNode, float weight)
    {
        //checking if the nodes exist
        if(!IsValid(currentNode) || !IsValid(otherNode))
        {
            Debug.LogWarning("Tried adding a connection to a node that doesnt exist");
            return;
        }    

        //add the connection to the other node
        Connection connection = new Connection(weight, otherNode);
        m_Connections.Add(connection);
        currentNode.connections.Add(connection);
    }

    public void RemoveNode(Node node)
    {
        //checking if the node exists
        if(!IsValid(node))
            return;

        //removing all connections to this node from other nodes
        foreach(Node otherNode in m_Nodes)
        {
            otherNode.connections.RemoveAll(x => x.otherNode.pos == node.pos);
        }
        //removing all connections to this node int the connection list
        m_Connections.RemoveAll(x => x.otherNode.pos == node.pos);

        //remove all the connections of this node
        foreach (Connection conections in GetNode(node).connections)
        {
            m_Connections.Remove(conections);
        }
        m_Nodes.Remove(node);       
    }

    public void RemoveNodes(List<Node> nodeIdx)
    {
        foreach(Node node in nodeIdx)
        {
            RemoveNode(node);
        }
    }

    public bool IsValid(Node node)
    {
        if (m_Nodes.Exists(x=> x.pos == node.pos))
            return true;
        return false;
    }
}
