using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectingWall : MonoBehaviour
{
    [Header("Player")]
    

    public string solidWall = "wallSolid";
    public string diagonalWall = "wallDiagonal";
    public string windowWall = "wallWindow";
    public string doorWall = "wallDoor";
    public float maxDistance = 20f;

    private void Update()
    {
        DetectWall();
    }

    void DetectWall() 
    {
       Vector3 origin = transform.position + (Vector3.up);



        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, maxDistance))
        {
           float distance = hit.distance;
            if (hit.collider.CompareTag(solidWall))
            {
                Debug.Log("SolidWall Detected. Distance =" + distance);
                Debug.DrawLine(origin, hit.point, Color.green);
            }
            else if (hit.collider.CompareTag(diagonalWall))
            {
                Debug.Log("DiagonalWall Detected. Distance =" + distance);
                Debug.DrawLine(origin, hit.point, Color.green);
            }
            else if (hit.collider.CompareTag(windowWall))
            {
                Debug.Log("WindowWall Detected. Distance =" + distance);
                Debug.DrawLine(origin, hit.point, Color.green);
            }
            else if (hit.collider.CompareTag(doorWall))
            {
                Debug.Log("DoorWall Detected. Distance =" + distance);
                Debug.DrawLine(origin, hit.point, Color.green);
            }

        }
        else { Debug.Log("Nothing");
            Debug.DrawLine(origin, hit.point, Color.green);
        }
    }

}
