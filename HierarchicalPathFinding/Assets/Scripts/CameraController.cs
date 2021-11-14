using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera m_Camera = null;

    [SerializeField]
    private float m_ZoomSpeed = 0.1f;

    [SerializeField]
    private float m_CameraSpeed = 10.0f;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    private void Update()
    {
        //zooming for the camera
        m_Camera.orthographicSize += -Input.mouseScrollDelta.y * m_ZoomSpeed;
        if (m_Camera.orthographicSize < 0.1f)
            m_Camera.orthographicSize = 0.1f;


        //camera movement
        Vector3 cameraMovement = new Vector3(0, 0, 0);
        if(Input.GetKey(KeyCode.W))
            cameraMovement.y += Time.deltaTime * m_CameraSpeed;
        if(Input.GetKey(KeyCode.S))
            cameraMovement.y -= Time.deltaTime * m_CameraSpeed;
        if (Input.GetKey(KeyCode.A))
            cameraMovement.x -= Time.deltaTime * m_CameraSpeed;
        if (Input.GetKey(KeyCode.D))
            cameraMovement.x += Time.deltaTime * m_CameraSpeed;

        m_Camera.transform.position += cameraMovement;
    }
}
