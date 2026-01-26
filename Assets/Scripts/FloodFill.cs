using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodFill : MonoBehaviour
{
    public static FloodFill FillInstance { get; private set; }
    private void Awake()
    {

        if (FillInstance != null && FillInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            FillInstance = this;
        }
    }

    private DetectingWall detect;
    
    public float scanInterval = 1f;
    private float scanTimer = 0f;

    public  float totalRoomVolume = 0f;
    public  float totalRoomHardness = 0f;
    public  float totalRoomJagness = 0f;

    public Queue<Vector3> wallQueue = new Queue<Vector3>();

    public HashSet<Vector3> visited = new HashSet<Vector3>();


 

    Collider[] resultsBuffer = new Collider[10];

    private void Start()
    {
        detect = DetectingWall.DetectInstance;
    }

    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            if (detect.distances[4] > 0 && detect.hasGridAnchor)
            {
                Fill();
            }
            scanTimer = 0;
        }
    }

    Vector3 SnapToGrid()
    {
        float nodeSize = detect.nodeSize;
        Vector3 rawPoisition = transform.position;
        Vector3 anchor = detect.hasGridAnchor ? detect.gridAnchorPoint : rawPoisition;
        float x = Mathf.Round((rawPoisition.x - anchor.x) / nodeSize) * nodeSize + anchor.x;
        float z = Mathf.Round((rawPoisition.z - anchor.z) / nodeSize) * nodeSize + anchor.z;

        float y = rawPoisition.y;

        return new Vector3(x, y, z);

    }
    bool IsWall(Vector3 position)
    {
        float nodeSize = detect.nodeSize;
        float halfSizeRef = (nodeSize / 2f) * 0.85f;
        Vector3 halfsize = new Vector3(halfSizeRef, halfSizeRef, halfSizeRef);

        return Physics.CheckBox(position, halfsize, Quaternion.identity, detect.WallLayer);
    }

    private void Fill()
    {
        wallQueue.Clear();
        visited.Clear();
        float nodeSize = detect.nodeSize;
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

        float halfSize = (detect.nodeSize / 2) * 0.95f;
        Vector3 boxSize = new Vector3(halfSize, halfSize, halfSize);

        int count = Physics.OverlapBoxNonAlloc(wallPosition, boxSize, resultsBuffer, Quaternion.identity, detect.WallLayer);
        DrawDebugBox(wallPosition, boxSize, Quaternion.identity, Color.red);

        float totalLocalHardness = 0f;
        float totalLocalJagness = 0f;
        int validMaterials = 0;

        for (int i = 0; i < count; i++)
        {
            string tag = resultsBuffer[i].tag;

            if (detect.GetMaterialInfo(tag, out MaterialDatabase.MaterialData data))
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

}
