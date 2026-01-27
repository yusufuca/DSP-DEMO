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
    public static DetectingWall DetectInstance { get; private set; }
    private void Awake()
    {
       
        if (DetectInstance != null && DetectInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DetectInstance = this;
        }
        if (databaseFile != null)
        {
            foreach (var mat in databaseFile.allMaterials)
            {
                if (!hardnessLookup.ContainsKey(mat.tag))
                {
             
                    hardnessLookup.Add(mat.tag, mat);
                }
            }
        }
    }

    [Header("Settings")]

    public float maxDistance = 20f;
    public float nodeSize = 2f;
    public LayerMask WallLayer;
    public float reverbSizePar = 0.025f;

    [Header("Text")]
    public TextMeshProUGUI frontText;
    public TextMeshProUGUI backText;
    public TextMeshProUGUI leftText;
    public TextMeshProUGUI rightText;
    public TextMeshProUGUI upText;
    public TextMeshProUGUI downText;

    [Header("Database")]
    public MaterialDatabase databaseFile;

 
    [Header("MaterialData")]

    public Dictionary<string, MaterialDatabase.MaterialData> hardnessLookup = new Dictionary<string, MaterialDatabase.MaterialData>();


 

   

    public  float[] distances = new float[6];
    public  float[] hardnesses = new float[6];
    public float[] jagnesses = new float[6];



    public  float dEarlyDelay;
    public float dLateDelay;
    public float dFeedBack;

    public Vector3[] wallOrigins = new Vector3[6];
    public Vector3[] wallNormals = new Vector3[6];

    private Quaternion[] wallRotations = new Quaternion[6];
    private Sensor[] sensors;

 

  



  

    public Vector3 gridAnchorPoint = Vector3.zero;
    public bool hasGridAnchor = false;
  
  




    private float speedOfSound = 343f;





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

       
    }
    private void Update()
    {
        DetectWall();
        DelayParameters();
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Clapping();
        }

    }

    public bool GetMaterialInfo(string tag, out MaterialDatabase.MaterialData data)
    {
        return hardnessLookup.TryGetValue(tag, out data);
    }

    private void Clapping()
    {

        RuntimeManager.PlayOneShot(ReverbManager.RevInstance.clapSFX);
    }

   
  
    void DetectWall()
    {
        for (int i = 0; i < sensors.Length; i++)
        {
            Vector3 origin = transform.position + (Vector3.up);
            Sensor s = sensors[i];
            distances[i] = maxDistance;
            wallNormals[i] = Vector3.up;

            Vector3 currentWorldDir = transform.TransformDirection(s.dir);
            Vector3 rawDir = currentWorldDir;

            if (s.name != "Up" && s.name != "Down")
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

                wallOrigins[i] = hit.point;
           
                wallNormals[i] = hit.normal;
                wallRotations[i] = hit.transform.rotation;
                Vector3 checkPosOrigin = hit.transform.position;
    ;

               

                Quaternion wallRot = hit.transform.rotation;

                if (!hasGridAnchor)
                {
                    if (distances[0] > 0)
                    {
                        gridAnchorPoint = wallOrigins[0];
                        hasGridAnchor = true;
                    }
                }

                if (hardnessLookup.TryGetValue(hitTag, out MaterialDatabase.MaterialData data))
                {
                    hardnesses[i] = data.hardness;
                    jagnesses[i] = data.jagness;
                }

                Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);

                s.ui.text = $"{s.name} {hitTag} Detected. Distance: {distances[i]:F2} Hardness: {hardnesses[i]} Jagness: {jagnesses[i]}";
            }
            else
            {
                hardnesses[i] = 0;
                distances[i] = 0;
                jagnesses[i] = 0;
                wallOrigins[i] = Vector3.zero;
                wallNormals[i] = Vector3.zero;


                s.ui.text = $"{s.name} Distance: {distances[i]}";
                Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);
     
            }

        }


    }
  
    
  
    public void DelayParameters()
    {
        float minDistance = 99f;
        float maxDistance = 0f;
        float totalHardness = 0f;
        int validWalls = 0;

        for (int i = 0; i < 6; i++)
        {
            float dist = distances[i];
            if (dist > 0)
            {
                if (dist < minDistance) minDistance = dist;

                if (dist > maxDistance) maxDistance = dist;

                totalHardness += hardnesses[i];
                validWalls++;
            }
            
            // early delay
            if (minDistance < 999f)
            {
                dEarlyDelay = (minDistance * 2f / speedOfSound) * 1000f;
            }
            else
            {
                dEarlyDelay = 0f;
            }

            // late delay

            if (maxDistance > 0f)
            {
                dLateDelay = (maxDistance * 2f / speedOfSound) * 1000f;
            }
            else
            {
                dLateDelay = 0f;
            }

            if (validWalls > 0)
            {
                float avgHardness = totalHardness / validWalls;
               
                dFeedBack = avgHardness * 0.8f;
            }
            else
            {
                dFeedBack = 0f;
            }
        }

    }
}
    
   

  
  
