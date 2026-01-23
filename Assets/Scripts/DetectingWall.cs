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
    public TextMeshProUGUI roomDataText;
    public TextMeshProUGUI roomModeText;


    /*MATERIAL HARDNESS*/


    [Header("MaterialData")]

    public List<MaterialData> definedMaterials = new List<MaterialData>();

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
    private Vector3[] wallOrigins = new Vector3[6];
    private Quaternion[] wallRotations = new Quaternion[6];
    private Sensor[] sensors;

    private float totalRoomVolume = 0f;
    private float totalRoomHardness = 0f;
    private float totalRoomJagness = 0f;
    Queue<Vector3> wallQueue = new Queue<Vector3>();

    HashSet<Vector3> visited = new HashSet<Vector3>();
    Collider[] resultsBuffer = new Collider[10];
    public float scanInterval = 1f;
    private float scanTimer = 0f;


    private Dictionary<string, MaterialData> hardnessLookup = new Dictionary<string, MaterialData>();

    private Vector3 gridAnchorPoint = Vector3.zero;
    private bool hasGridAnchor = false;
    private float nodeSize = 2f;
    public float reverbSizePar = 0.025f;


    private float dEarlyDelay;
    private float dLateDelay;
    private float dFeedBack;

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
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            if (distances[4] > 0 && hasGridAnchor)
            {
                FloodFill();
            }
            scanTimer = 0;
        }

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
    void ParameterUpdater()
    {
        DelayParameters();

        float totalDistances = 0f;

        for (int i = 0; i < 6; i++) totalDistances += distances[i] / 6;

        float totalHardness = 0f;

        for (int i = 0; i < 6; i++) totalHardness += hardnesses[i] / 6;

        float totalJagness = 0f;

        for (int i = 0; i < 6; i++) totalJagness += jagnesses[i] / 6;

        float mainRoomVolume = totalRoomVolume * (distances[4] + distances[5]);


        float roomSize = 0f;

        for (int i = 0; i < 4; i++) roomSize += ((totalLength[i] + totalHeight[i]) / 4);

        float totalVolume = (roomSize * 4f);

        float minWallDist = maxDistance;
        /* REVERB TIME */



        float reverbTime = (mainRoomVolume * totalRoomHardness) * reverbSizePar;



        /* EARLY DELAY */

        for (int i = 0; i < 4; i++)
        {
            if (distances[i] > 0 && distances[i] < minWallDist)
            {
                minWallDist = distances[i];

            }
        }


        float earlyDelay = minWallDist;



        /* LATE DELAY */

        float lateDelay = (reverbTime * 0.5f);


        /* DIFFUSION */

        float diffusion = totalRoomJagness;

        /* DENSITY */

        float density = totalJagness * totalHardness;




        /* HF DECAY */

        float hfDecayRatio = totalRoomHardness;

        /* HF REFERENCE */

        float hfReference = totalRoomHardness;

        /* HIGH CUT */

        float highCut = totalRoomVolume * reverbSizePar;

        /* EARLY LATE MIX */

        float delayMix = reverbTime * reverbSizePar;

        /* REVERB ON OF */

        int onOF = 1;

        if (distances[4] > 0)
        {
            onOF = 1;
            reverbTimeText.text = $"Reverb Time is: {reverbTime} Early Delay is: {earlyDelay} LateDelay is: {lateDelay} Diffusion: {diffusion}";
            roomModeText.text = $"HF Decay: {hfDecayRatio} HF Reference {hfReference} HighCut: {highCut}";
            roomDataText.text = $"TotalRoomVolume: {totalRoomVolume} Avarage Room Hardness: {totalRoomHardness} Avarage Room jagness: {totalRoomJagness}";
        }
        else
        {
            onOF = 0;
            reverbTimeText.text = $"Early Delay: {dEarlyDelay} Late Delay: {dLateDelay} FeedBack is: {dFeedBack}";
        }


        ReverbManager.RevInstance.UpdateReverb(reverbTime, earlyDelay, lateDelay, onOF, diffusion, density, hfDecayRatio, hfReference, highCut, delayMix,dEarlyDelay,dLateDelay,dFeedBack);


    }

    void DetectWall()
    {
        for (int i = 0; i < sensors.Length; i++)
        {
            Vector3 origin = transform.position + (Vector3.up);
            Sensor s = sensors[i];
            distances[i] = maxDistance;

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

                wallOrigins[i] = hit.transform.position;
                wallRotations[i] = hit.transform.rotation;
                Vector3 checkPosOrigin = hit.transform.position;
                Vector3 wallRight = hit.transform.right;
                Vector3 wallUp = hit.transform.up;

                Vector3 wallLeftCorner = hit.transform.right;

                Quaternion wallRot = hit.transform.rotation;

                if (!hasGridAnchor)
                {
                    if (distances[0] > 0)
                    {
                        gridAnchorPoint = wallOrigins[0];
                        hasGridAnchor = true;
                    }
                }





                float upNeighbors = CheckNeighbors(checkPosOrigin, wallUp, wallRot, Color.green, false);
                float downNeighbors = CheckNeighbors(checkPosOrigin, -wallUp, wallRot, Color.green, false);
                float rightNeighbors = CheckNeighbors(checkPosOrigin, wallRight, wallRot, Color.green, false);
                float leftNeighbors = CheckNeighbors(checkPosOrigin, -wallRight, wallRot, Color.green, false);







                totalHeight[i] = 4f + upNeighbors + downNeighbors;
                totalLength[i] = 4f + rightNeighbors + leftNeighbors;

                if (hardnessLookup.TryGetValue(hitTag, out MaterialData data))
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
                totalLength[i] = 0;
                s.ui.text = $"{s.name} Distance: {distances[i]}";
                Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);
                // Debug.DrawRay(origin, currentWorldDir * maxDistance, s.color);
            }

        }


    }
    float CheckNeighbors(Vector3 checkPosOrigin, Vector3 checkDirection, Quaternion boxRot, Color debugColor, bool ignoreRot)
    {

        Vector3 currentCheckPos = checkPosOrigin;

        float extraLength = 0f;

        int limitCheck = 0;


        Vector3 boxSize = new Vector3(1.9f, 1.9f, 0.9f);

        while (limitCheck < 20)
        {
            limitCheck++;
            currentCheckPos += checkDirection * 4f;


            Collider[] neighbors = Physics.OverlapBox(currentCheckPos, boxSize, boxRot, WallLayer);

            bool foundValidNeighbor = false;

            foreach (Collider col in neighbors)
            {


                if (ignoreRot)
                {
                    foundValidNeighbor = true;
                    break;
                }


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
                //DrawDebugBox(currentCheckPos, boxSize, boxRot, debugColor);



            }
            else
            {
                //DrawRotatedBox(currentCheckPos, boxSize, boxRot, Color.red);
                break;
            }

        }
        return extraLength;

    }

    Vector3 SnapToGrid()
    {

        Vector3 rawPoisition = transform.position;
        Vector3 anchor = hasGridAnchor ? gridAnchorPoint : rawPoisition;
        float x = Mathf.Round((rawPoisition.x - anchor.x) / nodeSize) * nodeSize + anchor.x;
        float z = Mathf.Round((rawPoisition.z - anchor.z) / nodeSize) * nodeSize + anchor.z;

        float y = rawPoisition.y;

        return new Vector3(x, y, z);

    }

    bool IsWall(Vector3 position)
    {

        float halfSizeRef = (nodeSize / 2f) * 0.85f;
        Vector3 halfsize = new Vector3(halfSizeRef, halfSizeRef, halfSizeRef);

        return Physics.CheckBox(position, halfsize, Quaternion.identity, WallLayer);
    }

    void FloodFill()
    {
        wallQueue.Clear();
        visited.Clear();

        float accumulatedHardness = 0f;
        float accumulatedJagness = 0f;
        int totalWallsTouched = 0;



        Vector3 rawStart = SnapToGrid();
        Vector3 safeStart = rawStart;
        bool foundSafeSpot = true;
        if (IsWall(rawStart))
        {
            foundSafeSpot = false;
            Vector3[] emergencyDirs = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right };
            foreach (Vector3 dir in emergencyDirs)
            {
                Vector3 neighbor = rawStart + (dir * (nodeSize * 0.25f));
                if (!IsWall(neighbor))
                {
                    safeStart = neighbor;
                    foundSafeSpot = true;
                    break;
                }
            }

        }
        if (!foundSafeSpot) return;

        wallQueue.Enqueue(safeStart);
        visited.Add(safeStart);

        int volumeCounter = 0;
        float debugSize = (nodeSize / 2f) * 0.9f;
        while (wallQueue.Count > 0 && volumeCounter < 200)
        {
            Vector3 currentPos = wallQueue.Dequeue();
            volumeCounter++;

            DrawDebugBox(currentPos, new Vector3(debugSize, debugSize, debugSize), Quaternion.identity, Color.yellow);

            Vector3[] neighbors = new Vector3[]
            {
                currentPos + Vector3.forward * nodeSize,
                currentPos + Vector3.back * nodeSize,
                currentPos + Vector3.right * nodeSize,
                currentPos + Vector3.left * nodeSize
            };

            foreach (var target in neighbors)
            {
                if (!visited.Contains(target))
                {
                    if (!IsWall(target))
                    {
                        visited.Add(target);
                        wallQueue.Enqueue(target);
                    }
                    else
                    {

                        float h = 0f;
                        float j = 0f;
                        if (GetMaterialData(target, out h, out j))
                        {
                            accumulatedHardness += h;
                            accumulatedJagness += j;
                            totalWallsTouched++;
                        }
                    }


                }


            }



        }
        float voxelVolume = nodeSize * nodeSize;
        totalRoomVolume = volumeCounter * voxelVolume;
        if (totalWallsTouched > 0)
        {
            totalRoomHardness = accumulatedHardness / totalWallsTouched;
            totalRoomJagness = accumulatedJagness / totalWallsTouched;
        }
        else
        {
            totalRoomHardness = 0f;
            totalRoomJagness = 0f;
        }






    }

    bool GetMaterialData(Vector3 wallPosition, out float hardness, out float jagness)
    {
        hardness = 0f;
        jagness = 0f;

        float halfSize = (nodeSize / 2) * 0.95f;
        Vector3 boxSize = new Vector3(halfSize, halfSize, halfSize);

        int count = Physics.OverlapBoxNonAlloc(wallPosition, boxSize, resultsBuffer, Quaternion.identity, WallLayer);
        DrawDebugBox(wallPosition, boxSize, Quaternion.identity, Color.red);

        float totalLocalHardness = 0f;
        float totalLocalJagness = 0f;
        int validMaterials = 0;

        for (int i = 0; i < count; i++)
        {
            string tag = resultsBuffer[i].tag;

            if (hardnessLookup.TryGetValue(tag, out MaterialData data))
            {
                totalLocalHardness += data.hardness;
                totalLocalJagness += data.jagness;
                validMaterials++;
            }
        }
        if (validMaterials > 0)
        {
            hardness = totalLocalHardness / validMaterials;
            jagness = totalLocalJagness / validMaterials;
            return true;
        }
        else
        {
            return false;
        }

    }

    void DelayParameters()
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
    
   

  
  
