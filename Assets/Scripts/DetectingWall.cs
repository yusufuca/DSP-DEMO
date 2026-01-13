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
using Unity.Mathematics;

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


    public bool isComplexShape = false;
    public string shapeType = "box";

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
        AnalyzeShape();
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

        reverbTimeText.text = $"ReverbTime is: {reverbTime} Room Shape is {shapeType}"; 

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

                Quaternion wallRot = hit.transform.rotation;
              
                

                float upNeighbors = CheckNeighbors(checkPosOrigin, wallUp, wallRot);
                float downNeighbors = CheckNeighbors(checkPosOrigin, -wallUp,wallRot);
                float rightNeighbors = CheckNeighbors(checkPosOrigin, wallRight, wallRot);
                float leftNeighbors = CheckNeighbors(checkPosOrigin,-wallRight, wallRot);

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

        
    }
    float CheckNeighbors(Vector3 checkPosOrigin, Vector3 checkDirection, Quaternion boxRot)
    {

        Vector3 currentCheckPos = checkPosOrigin;

        float extraLength = 0f;

        int limitCheck = 0;
        Vector3 boxSize = new Vector3(1.9f, 1.9f, 0.9f);

        while (limitCheck < 20)
        {
            limitCheck++;
            currentCheckPos += checkDirection * 4.0f;


            Collider[] neighbors = Physics.OverlapBox(currentCheckPos, boxSize, boxRot, WallLayer);

            bool foundValidNeighbor = false;

            foreach (Collider col in neighbors)
            {

                float angleDiff = Quaternion.Angle(boxRot, col.transform.rotation);

                if (angleDiff < 5f)
                {
                    foundValidNeighbor = true;
                    break;


                }

            }

            if (foundValidNeighbor)
            {

                extraLength += 4f;
                DrawDebugBox(currentCheckPos, boxSize, boxRot, Color.green);



            }
            else
            {
                //DrawRotatedBox(currentCheckPos, boxSize, boxRot, Color.red);
                break;
            }

        }
        return extraLength;

    }
    void DrawDebugBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color)
    {
       
        Vector3 size = halfExtents * 2f;

      
        Vector3[] points = new Vector3[8];
        points[0] = center + rotation * new Vector3(size.x, size.y, size.z) * 0.5f;
        points[1] = center + rotation * new Vector3(-size.x, size.y, size.z) * 0.5f;
        points[2] = center + rotation * new Vector3(-size.x, -size.y, size.z) * 0.5f;
        points[3] = center + rotation * new Vector3(size.x, -size.y, size.z) * 0.5f;
        points[4] = center + rotation * new Vector3(size.x, size.y, -size.z) * 0.5f;
        points[5] = center + rotation * new Vector3(-size.x, size.y, -size.z) * 0.5f;
        points[6] = center + rotation * new Vector3(-size.x, -size.y, -size.z) * 0.5f;
        points[7] = center + rotation * new Vector3(size.x, -size.y, -size.z) * 0.5f;

       
        Debug.DrawLine(points[0], points[1], color); Debug.DrawLine(points[1], points[2], color);
        Debug.DrawLine(points[2], points[3], color); Debug.DrawLine(points[3], points[0], color);

        Debug.DrawLine(points[4], points[5], color); Debug.DrawLine(points[5], points[6], color);
        Debug.DrawLine(points[6], points[7], color); Debug.DrawLine(points[7], points[4], color);

        Debug.DrawLine(points[0], points[4], color); Debug.DrawLine(points[1], points[5], color);
        Debug.DrawLine(points[2], points[6], color); Debug.DrawLine(points[3], points[7], color);
    }

    void AnalyzeShape()
    {

        float roomDepth = distances[0] + distances[1];
        float roomWidth = distances[2] + distances[3];

        float frontWallLength = totalLength[0];
        float backWallLength = totalLength[1];
        float rightWallLength = totalLength[2];
        float leftWallLength = totalLength[3];

        float gapThreshold = 2f;

        bool gapOnLeft = (roomDepth > leftWallLength+gapThreshold);
        bool gapOnRight = (roomDepth > rightWallLength + gapThreshold);
        bool gapOnFront = (roomWidth > frontWallLength+gapThreshold);
        bool gapOnBack = (roomWidth > backWallLength + gapThreshold);

        int gapCount = 0;
        if (gapOnLeft) gapCount++;
        if (gapOnRight) gapCount++;
        if (gapOnFront) gapCount++;
        if (gapOnBack) gapCount++;

        if (gapCount == 0)
        {
            shapeType = "SimpleBox";
            isComplexShape=false;
                
        }
        else if (gapCount == 1)
        {
            shapeType = "L Shaped Room";
            isComplexShape=true;
        }
        else if (gapCount >= 2)
        {
            if (gapOnLeft && gapOnRight)
            {
                shapeType = "T Shaped Room";
                isComplexShape=true;
            }
            else 
            {
                shapeType = "W Shaped Room";
            }
        }
    }



}
