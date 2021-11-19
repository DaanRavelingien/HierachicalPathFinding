using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInteractionScript : MonoBehaviour
{
    [SerializeField]
    GridWorld m_World = null;

    [SerializeField]
    Camera m_Camera = null;

    private void Update()
    {
        //left mouse button
        //toggle tile between wall and ground
        if(Input.GetMouseButtonUp(0))
        {
            Vector2 clickedPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);

            m_World.ToggleCell(clickedPos);
        }
        //right mouse button
        if (Input.GetMouseButtonUp(1))
        {

        }
        //middle moust button
        if(Input.GetMouseButtonUp(2))
        {

        }

    }


}
