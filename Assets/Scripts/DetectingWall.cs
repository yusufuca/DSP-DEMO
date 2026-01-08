using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;
using System;

public class DetectingWall : MonoBehaviour
{
    [Header("Sfx")]
    public EventReference clapSFX;
    
    [Header("Prefabs")]
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
        if (Input.GetKeyDown(KeyCode.E))
        { 
            Clapping(); 
        }
    }

    private void Clapping()
    {
        
        RuntimeManager.PlayOneShot(clapSFX);
    }

    void DetectWall() 
    {
       Vector3 origin = transform.position + (Vector3.up);
        float fDistance = maxDistance;
        float bdistance = maxDistance;
        float rDistance = maxDistance;
        float lDistance =  maxDistance;
        //   Front

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hitFront, maxDistance))
        {
          
            fDistance = hitFront.distance;
            
            if (hitFront.collider.CompareTag(solidWall))
            {
                frontText.text = "Front SolidWall Detected Distance: " + fDistance;
                Debug.Log("SolidWall Detected front. Distance =" + fDistance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(diagonalWall))
            {
                frontText.text = "Front DiagonalWall Detected Distance: " + fDistance;
                Debug.Log("DiagonalWall Detected front. Distance =" + fDistance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(windowWall))
            {
                frontText.text = "Front Window Detected Distance: " + fDistance;
                Debug.Log("WindowWall Detected front. Distance =" + fDistance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }
            else if (hitFront.collider.CompareTag(doorWall))
            {
                frontText.text = "Front Door Detected Distance: " + fDistance;
                Debug.Log("DoorWall Detected front. Distance =" + fDistance);
                Debug.DrawLine(origin, hitFront.point, Color.green);
            }

        }
        else 
        {
            fDistance = maxDistance;
            frontText.text = "Front Distance: Null";
            Debug.Log("Nothing front");
            Debug.DrawLine(origin, hitFront.point, Color.green);
        }
        
        // Back


        if (Physics.Raycast(origin, -transform.forward, out RaycastHit hitBack, maxDistance))
        {
             bdistance = hitBack.distance;
            

            if (hitBack.collider.CompareTag(solidWall))
            {
                backText.text = "Back SolidWall Detected Distance: " + bdistance;
                Debug.Log("SolidWall Detected back. Distance =" + bdistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(diagonalWall))
            {
                backText.text = "Back Diagonal Detected Distance: " + bdistance;
                Debug.Log("DiagonalWall Detected back. Distance =" + bdistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(windowWall))
            {
                backText.text = "Back Window Detected Distance: " + bdistance;
                Debug.Log("WindowWall Detected back. Distance =" + bdistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(doorWall))
            {
                backText.text = "Back Door Detected Distance: " + bdistance;
                Debug.Log("DoorWall Detected back. Distance =" + bdistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }

        }
        else
        {
            bdistance = maxDistance;
            backText.text = "Back Distance: Null";
            Debug.Log("Nothing back");
            Debug.DrawLine(origin, hitBack.point, Color.red);
        }

        // Right


        if (Physics.Raycast(origin, transform.right, out RaycastHit hitRight, maxDistance))
        {
            rDistance = hitRight.distance;
            
            if (hitRight.collider.CompareTag(solidWall))
            {
                rightText.text = "Right SolidWall Detected Distance: " + rDistance;
                Debug.Log("SolidWall Detected right. Distance =" + rDistance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(diagonalWall))
            {
                rightText.text = "Right DiagonalWall Detected Distance: " + rDistance;
                Debug.Log("DiagonalWall Detected right. Distance =" + rDistance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(windowWall))
            {
                rightText.text = "Right Window Detected Distance: " + rDistance;
                Debug.Log("WindowWall Detected right. Distance =" + rDistance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }
            else if (hitRight.collider.CompareTag(doorWall))
            {
                rightText.text = "Right Door Detected Distance: " + rDistance;
                Debug.Log("DoorWall Detected right. Distance =" + rDistance);
                Debug.DrawLine(origin, hitRight.point, Color.blue);
            }

        }
        else
        {
            rDistance = maxDistance;
            rightText.text = "Right Distance: Null";
            Debug.Log("Nothing right");
            Debug.DrawLine(origin, hitRight.point, Color.blue);
        }

        //  Left

        if (Physics.Raycast(origin, -transform.right, out RaycastHit hitLeft, maxDistance))
        {
            lDistance = hitLeft.distance;
            
            if (hitLeft.collider.CompareTag(solidWall))
            {
                leftText.text = "Left SolidWall Detected Distance: " + lDistance;
                Debug.Log("SolidWall Detected Left. Distance =" + lDistance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(diagonalWall))
            {
                leftText.text = "Left DiagonalWall Detected Distance: " + lDistance;
                Debug.Log("DiagonalWall Detected Left. Distance =" + lDistance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(windowWall))
            {
                leftText.text = "Left Window Detected Distance: " + lDistance;
                Debug.Log("WindowWall Detected Left. Distance =" + lDistance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }
            else if (hitLeft.collider.CompareTag(doorWall))
            {
                leftText.text = "Left Door Detected Distance: " + lDistance;
                Debug.Log("DoorWall Detected Left. Distance =" + lDistance);
                Debug.DrawLine(origin, hitLeft.point, Color.magenta);
            }

        }
        else
        {
            lDistance = maxDistance;
            Debug.Log("Nothing Left");
            leftText.text = "Left Distance: Null";
            Debug.DrawLine(origin, hitLeft.point, Color.magenta);
        }
        float avRoomSize = (fDistance + bdistance + lDistance + rDistance) / 4;

        RuntimeManager.StudioSystem.setParameterByName("aRoomSize", avRoomSize);
    }

}
