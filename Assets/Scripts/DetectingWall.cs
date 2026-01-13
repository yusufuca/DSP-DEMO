using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;
using System;
using UnityEngine.InputSystem;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

public class DetectingWall : MonoBehaviour
{
    [Header("Sfx")]
    public EventReference clapSFX;
    
    [Header("Prefabs")]
   
    public float maxDistance = 20f;
  
    [Header("Text")]
    public TextMeshProUGUI frontText;
    public TextMeshProUGUI backText;
    public TextMeshProUGUI leftText;
    public TextMeshProUGUI rightText;
    public TextMeshProUGUI upText;
    public TextMeshProUGUI downText;
    public TextMeshProUGUI reverbTimeText;
    public TextMeshProUGUI earlyDelayText;
    public TextMeshProUGUI lateDelayText;


    /*MATERIAL HARDNESS*/


    [Header("MaterialData")]

    public List<MaterialData> definedMaterials = new List<MaterialData> ();

    [System.Serializable]
    public struct MaterialData
    {
        public string tag;
        public float hardness;
        public float jagness;
    }

    public LayerMask WallLayer;
   
    private float[] distances = new float[6];
    private float[] totalHeight = new float[6];
    private float[] totalLength = new float[6];
    private float[] hardnesses = new float[6];
    private float[] jagnesses = new float[6];
    private Sensor[] sensors;


    private Dictionary<string, MaterialData> hardnessLookup = new Dictionary<string, MaterialData>();

    private struct Sensor
    {
        public string name;
        public Vector3 dir;
        public TextMeshProUGUI ui;
        public Color color;
    }
    private void Start()
    {
        sensors = new Sensor[]
        {
            new Sensor { name="Front", dir=Vector3.forward,  ui=frontText, color=Color.green },   // Index 0
            new Sensor { name="Back",  dir=Vector3.back, ui=backText,  color=Color.green },     // Index 1
            new Sensor { name="Right", dir=Vector3.right,    ui=rightText, color=Color.blue },    // Index 2
            new Sensor { name="Left",  dir=Vector3.left,   ui=leftText,  color=Color.blue}, // Index 3
            new Sensor { name="Up",    dir=Vector3.up,       ui=upText,    color=Color.yellow },  // Index 4
            new Sensor { name="Down",  dir=Vector3.down,      ui=downText,  color=Color.yellow }    // Index 5
        };

        foreach (var mat in definedMaterials)
        {
            if (!hardnessLookup.ContainsKey(mat.tag))
            {
                hardnessLookup.Add(mat.tag, mat);
            }
        }
    }
    private void Update()
    {
        DetectWall();
        if (Input.GetKeyDown(KeyCode.E))
        { 
            Clapping(); 
        }
        ParameterUpdater();
        
    }


    private void Clapping()
    {
        
        RuntimeManager.PlayOneShot(clapSFX);
    }

    void ParameterUpdater()
    {


        float totalDistances = 0f;

        for (int i = 0; i < 6; i++) totalDistances += distances[i]/6;

        float totalHardness = 0f;

        for (int i = 0; i < 6; i++) totalHardness += hardnesses[i]/6;

        float totalJagness = 0f;

        for (int i = 0; i < 6; i++) totalJagness += jagnesses[i] / 6;

        float roomSize = 0f;

        for (int i = 0; i < 4; i++) roomSize += ((totalLength[i] + totalHeight[i]) /4);

        float minWallDist = maxDistance;
        /* REVERB TIME */


        //float reverbTime = (totalDistances * totalHardness);

        float reverbTime = roomSize * totalHardness;

        reverbTimeText.text = $"ReverbTime is: {reverbTime}"; 

        /* EARLY DELAY */

        for (int i = 0; i < 4; i++)
        {
            if (distances[i] > 0 && distances[i] < minWallDist)
            {
                minWallDist = distances[i];
                
            }
          
          
        }

        // float earlyDelay = distances.Where(d => d > 2).DefaultIfEmpty(maxDistance).Min();
        float earlyDelay = minWallDist;

        earlyDelayText.text = $"EarlyDelay is: {earlyDelay}";

        /* LATE DELAY */

        float lateDelay =(reverbTime * 0.5f);
        

        /* DIFFUSION */

        float diffusion = totalJagness;

        /* DENSITY */

        float density = totalJagness*totalHardness;

        


        /* HF DECAY */

        float hfDecayRatio = totalHardness;

        /* HF REFERENCE */

        float hfReference = totalHardness;

        /* HIGH CUT */

        float highCut = totalDistances;

        /* EARLY LATE MIX */

        float delayMix = totalDistances;

        /* REVERB ON OF */

        int onOF = 1;

        if (distances[4] > 0)
        {
            onOF = 1;
        }
        else
        {
            onOF = 0;   
        }
        lateDelayText.text = $"LateDleay is: {lateDelay} Diffusion: {diffusion} Density {density} HF Decay: {hfDecayRatio} HF Reference {hfReference} HighCut: {highCut}";

        ReverbManager.RevInstance.UpdateReverb(reverbTime,earlyDelay,lateDelay,onOF, diffusion,density,hfDecayRatio,hfReference,highCut,delayMix);


    }

    void DetectWall()
    {
        for (int i = 0; i < sensors.Length; i++)
        {
            Vector3 origin = transform.position + (Vector3.up);
            Sensor s = sensors[i];
            distances[i] = maxDistance;
            
            Vector3 currentWorldDir = transform.TransformDirection(s.dir);
            Vector3 rawDir= currentWorldDir;

            if(s.name!="Up" && s.name != "Down")
            {
                if (Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.z))
                {
                    currentWorldDir = new Vector3(Mathf.Sign(rawDir.x), 0, 0);
                }
                else
                {
                    currentWorldDir = new Vector3(0, 0, Mathf.Sign(rawDir.z));
                }
            }


            if (Physics.Raycast(origin, currentWorldDir, out RaycastHit hit, maxDistance))
            {
                
                distances[i] = hit.distance;
                string hitTag = hit.collider.tag;
                
                Vector3 checkPosOrigin = hit.transform.position;
                Vector3 wallRight = hit.transform.right;
                Vector3 wallUp = hit.transform.up;

              
                

                float upNeighbors = CheckNeighbors(checkPosOrigin, wallUp);
                float downNeighbors = CheckNeighbors(checkPosOrigin, -wallUp);
                float rightNeighbors = CheckNeighbors(checkPosOrigin, wallRight);
                float leftNeighbors = CheckNeighbors(checkPosOrigin,-wallRight);

                totalHeight[i] = 4f + upNeighbors + downNeighbors;
                totalLength[i] = 4f + rightNeighbors + leftNeighbors;

                if (hardnessLookup.TryGetValue(hitTag, out MaterialData data))
                {
                    hardnesses[i] = data.hardness;
                    jagnesses[i] = data.jagness;
                }

                Debug.DrawRay(origin, currentWorldDir*maxDistance, s.color);
              
                s.ui.text = $"{s.name}{hitTag}Detected Distance: {distances[i]:F2}Hardness:  {hardnesses[i]} Jagness: {jagnesses[i]} Height:{totalHeight[i]}m Length:{totalLength[i]}m";
            }
            else
            {
                hardnesses[i] = 0;
                distances[i] = 0;
                jagnesses[i ] = 0;
                s.ui.text = $"{s.name} Distance: {distances[i]}";
                Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);
                // Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);
            }
            
        }

        float CheckNeighbors(Vector3 checkPosOrigin,Vector3 checkDirection)
        {
          
            Vector3 currentCheckPos = checkPosOrigin;

            float extraLength = 0f;
            
            int limitCheck = 0;
            while (limitCheck < 20)
            {
               limitCheck++;
                
                Vector3 boxSize = new Vector3(1.9f, 1.9f, 0.9f);
                currentCheckPos += checkDirection * 4.0f;
                Collider[] neighbors = Physics.OverlapBox(currentCheckPos, boxSize, Quaternion.identity, WallLayer);

                if (neighbors.Length > 0) 
                {
                    extraLength += 4f;
                }
                else
                {
                    break;
                }
                
            }
            return extraLength;
            // Debug.Log($"{s.name} neighborCount: {neighbors.Length}");
        }
    }
   

  /*  void DetectWall()
    {
        Vector3 origin = transform.position + (Vector3.up);
        fDistance = maxDistance;
        bDistance = maxDistance;
        rDistance = maxDistance;
        lDistance = maxDistance;
        uDistance = maxDistance;
        dDistance = maxDistance;
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
            fDistance = 0;
            frontText.text = "Front Distance: Null";
            Debug.Log("Nothing front");
            Debug.DrawLine(origin, hitFront.point, Color.green);

        }

        // Back


        if (Physics.Raycast(origin, -transform.forward, out RaycastHit hitBack, maxDistance))
        {
            bDistance = hitBack.distance;


            if (hitBack.collider.CompareTag(solidWall))
            {
                backText.text = "Back SolidWall Detected Distance: " + bDistance;
                Debug.Log("SolidWall Detected back. Distance =" + bDistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(diagonalWall))
            {
                backText.text = "Back Diagonal Detected Distance: " + bDistance;
                Debug.Log("DiagonalWall Detected back. Distance =" + bDistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(windowWall))
            {
                backText.text = "Back Window Detected Distance: " + bDistance;
                Debug.Log("WindowWall Detected back. Distance =" + bDistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }
            else if (hitBack.collider.CompareTag(doorWall))
            {
                backText.text = "Back Door Detected Distance: " + bDistance;
                Debug.Log("DoorWall Detected back. Distance =" + bDistance);
                Debug.DrawLine(origin, hitBack.point, Color.red);
            }

        }
        else
        {
            bDistance = 0;
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
            rDistance = 0;
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
            lDistance = 0;
            Debug.Log("Nothing Left");
            leftText.text = "Left Distance: Null";
            Debug.DrawLine(origin, hitLeft.point, Color.magenta);
        }

        // UP

        if (Physics.Raycast(origin, transform.up, out RaycastHit hitUp, maxDistance))
        {
            uDistance = hitUp.distance;

            if (hitUp.collider.CompareTag(floor))
            {
                upText.text = "Up Floor Detected Distance: " + uDistance;
                
                Debug.DrawLine(origin, hitUp.point, Color.magenta);
            }
            
            else if (hitUp.collider.CompareTag(ceiling))
            {
                upText.text = "Up Ceiling Detected Distance: " + uDistance;
                Debug.DrawLine(origin, hitUp.point, Color.magenta);
            }

        }
        else
        {
            uDistance = 0;
            
            upText.text = "Up Distance: Null";
            Debug.DrawLine(origin, hitUp.point, Color.magenta);
        }

        // DOWN

        if (Physics.Raycast(origin, -transform.up, out RaycastHit hitDown, maxDistance))
        {
            dDistance = hitDown.distance;

            if (hitDown.collider.CompareTag(plane))
            {
                downText.text = "Down plane Detected Distance: " + dDistance;

                Debug.DrawLine(origin, hitDown.point, Color.white);
            }

            else if (hitDown.collider.CompareTag(ceiling))
            {
                downText.text = "Down Ceiling Detected Distance: " + dDistance;
                Debug.DrawLine(origin, hitDown.point, Color.magenta);
            }
            else if (hitDown.collider.CompareTag(floor))
            {
                downText.text = "Down Floor Detected Distance: " + dDistance;
                Debug.DrawLine(origin, hitDown.point, Color.magenta);
            }

        }
        else
        {
            dDistance = 0;

            downText.text = "Down Distance: Null";
            Debug.DrawLine(origin, hitDown.point, Color.magenta);
        }



    }
  */

}
