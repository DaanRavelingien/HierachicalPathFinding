using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInteractionScript : MonoBehaviour
{
    [SerializeField]
    GridWorld m_World = null;

    [SerializeField]
    Camera m_Camera = null;

    private Vector2Int? m_PathStart = null;
    private Vector2Int? m_PathGoal = null;

    private void Update()
    {
        //left mouse button
        //toggle tile between wall and ground
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 clickedPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);

            m_World.ToggleCell(clickedPos);
            m_World.PathVisualizer.ShowPreProcessedGraph();
            if (m_PathStart.HasValue && m_PathGoal.HasValue)
                m_World.PathVisualizer.ShowPath(m_World.GridPathFinding.FindPathHirarchicalAStar(m_World,
                    m_PathStart.Value,
                    m_PathGoal.Value));
        }
        //right mouse button
        if (Input.GetMouseButtonUp(1))
        {
            Vector2 clickedPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            m_PathGoal = new Vector2Int((int)clickedPos.x, (int)clickedPos.y);
            if (m_PathStart.HasValue && m_PathGoal.HasValue)
                m_World.PathVisualizer.ShowPath(m_World.GridPathFinding.FindPathHirarchicalAStar(m_World,
                    m_PathStart.Value,
                    m_PathGoal.Value));
        }
        //middle moust button
        if (Input.GetMouseButtonUp(2))
        {
            Vector2 clickedPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            m_PathStart = new Vector2Int((int)clickedPos.x, (int)clickedPos.y);
            if (m_PathStart.HasValue && m_PathGoal.HasValue)
                m_World.PathVisualizer.ShowPath(m_World.GridPathFinding.FindPathHirarchicalAStar(m_World,
                    m_PathStart.Value,
                    m_PathGoal.Value));
        }
    }
}
