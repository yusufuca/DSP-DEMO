using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DetectingWall : MonoBehaviour
{
    [Header("Player")]
    

    public string solidWall = "wallSolid";
    public string diagonalWall = "wallDiagonal";
    public string windowWall = "wallWindow";
    public string doorWall = "wallDoor";
    public float maxDistance = 20f;
    
    
    [Header("Text")]
    public TextMeshProUGUI frontText;
    public TextMeshProUGUI backText;
    public TextMeshProUGUI leftText;
    public TextMeshProUGUI rightText;

  


    private void Update()
    {
        DetectWall();
    }

    void DetectWall() 
    {
       Vector3 origin = transform.position + (Vector3.up);

        //   Front

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hitFront, maxDistance))
        {
           float distance = hitFront.distance;
            
            
            if (hitFront.collider.CompareTag(solidWall))
            {
                frontText.text = "Front SolidWall Detected Distance: " + distance;
                Debug.Log("SolidWall Detected front. Distance =" + distance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(diagonalWall))
            {
                frontText.text = "Front DiagonalWall Detected Distance: " + distance;
                Debug.Log("DiagonalWall Detected front. Distance =" + distance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(windowWall))
            {
                frontText.text = "Front Window Detected Distance: " + distance;
                Debug.Log("WindowWall Detected front. Distance =" + distance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(doorWall))
            {
                frontText.text = "Front Door Detected Distance: " + distance;
                Debug.Log("DoorWall Detected front. Distance =" + distance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }

        }
        else 
        {
            frontText.text = "Front Distance: Null";
            Debug.Log("Nothing front");
            Debug.DrawLine(origin, hitFront.point, Color.green);
        }
        
        // Back


        if (Physics.Raycast(origin, -transform.forward, out RaycastHit hitBack, maxDistance))
        {
            float distance = hitBack.distance;
            

            if (hitBack.collider.CompareTag(solidWall))
            {
                backText.text = "Back SolidWall Detected Distance: " + distance;
                Debug.Log("SolidWall Detected back. Distance =" + distance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(diagonalWall))
            {
                backText.text = "Back Diagonal Detected Distance: " + distance;
                Debug.Log("DiagonalWall Detected back. Distance =" + distance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(windowWall))
            {
                backText.text = "Back Window Detected Distance: " + distance;
                Debug.Log("WindowWall Detected back. Distance =" + distance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(doorWall))
            {
                backText.text = "Back Door Detected Distance: " + distance;
                Debug.Log("DoorWall Detected back. Distance =" + distance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }

        }
        else
        {
            backText.text = "Back Distance: Null";
            Debug.Log("Nothing back");
            Debug.DrawLine(origin, hitBack.point, Color.red);
        }

        // Right


        if (Physics.Raycast(origin, transform.right, out RaycastHit hitRight, maxDistance))
        {
            float distance = hitRight.distance;
            
            if (hitRight.collider.CompareTag(solidWall))
            {
                rightText.text = "Right SolidWall Detected Distance: " + distance;
                Debug.Log("SolidWall Detected right. Distance =" + distance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(diagonalWall))
            {
                rightText.text = "Right DiagonalWall Detected Distance: " + distance;
                Debug.Log("DiagonalWall Detected right. Distance =" + distance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(windowWall))
            {
                rightText.text = "Right Window Detected Distance: " + distance;
                Debug.Log("WindowWall Detected right. Distance =" + distance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(doorWall))
            {
                rightText.text = "Right Door Detected Distance: " + distance;
                Debug.Log("DoorWall Detected right. Distance =" + distance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }

        }
        else
        {
            rightText.text = "Right Distance: Null";
            Debug.Log("Nothing right");
            Debug.DrawLine(origin, hitRight.point, Color.blue);
        }

        //  Left

        if (Physics.Raycast(origin, -transform.right, out RaycastHit hitLeft, maxDistance))
        {
            float distance = hitLeft.distance;
            
            if (hitLeft.collider.CompareTag(solidWall))
            {
                leftText.text = "Left SolidWall Detected Distance: " + distance;
                Debug.Log("SolidWall Detected Left. Distance =" + distance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(diagonalWall))
            {
                leftText.text = "Left DiagonalWall Detected Distance: " + distance;
                Debug.Log("DiagonalWall Detected Left. Distance =" + distance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(windowWall))
            {
                leftText.text = "Left Window Detected Distance: " + distance;
                Debug.Log("WindowWall Detected Left. Distance =" + distance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(doorWall))
            {
                leftText.text = "Left Door Detected Distance: " + distance;
                Debug.Log("DoorWall Detected Left. Distance =" + distance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }

        }
        else
        {
            Debug.Log("Nothing Left");
            leftText.text = "Left Distance: Null";
            Debug.DrawLine(origin, hitLeft.point, Color.magenta);
        }

    }

}
