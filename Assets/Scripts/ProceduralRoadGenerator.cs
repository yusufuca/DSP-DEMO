using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralRoadGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject roadPrefab;
    public GameObject startRoad;
    public Transform player;

    [Header("Configs")]
    public float roadSize = 50f;



    private float zPositiveThreshold = 20;
    private float zNegativeThreshold = -20;

    private float xPositiveThreshold = 20;
    private float xNegativeThreshold = -20;

    private Queue<GameObject> roadQueue = new Queue<GameObject>();
    private HashSet<Vector3Int> roadPos = new HashSet<Vector3Int>();
    public int maxActiveRoads = 2;

    void Update()
    {
        zPosCheck();
        xPosCheck();

    }


    void Converter(float xPos, float yPos)
    {
        var houseGen = ProceduralBuildingGenerator.HouseInstance;

        int xHousePos = Mathf.RoundToInt(xPos / houseGen.wallWidth);
        int yHousePos = Mathf.RoundToInt(yPos / houseGen.wallWidth);

        Vector2Int housePos = new Vector2Int(xHousePos, yHousePos);
        houseGen.GenerateHouse(housePos);
    }

    public void zPosCheck()
    {
        float currentX = Mathf.Round(player.position.x / roadSize) * roadSize;
        if (player.position.z > zPositiveThreshold)
        {
            float spawnZ = zPositiveThreshold + 30f;

            SpawnRoad(currentX, spawnZ);
            Converter(currentX, spawnZ);
            zPositiveThreshold += roadSize;
            zNegativeThreshold += roadSize;
        }
        else if (player.position.z < zNegativeThreshold)
        {
            float spawnZ = zNegativeThreshold - 30f;
            SpawnRoad(currentX, spawnZ);
            Converter(currentX, spawnZ);
            zNegativeThreshold -= roadSize;
            zPositiveThreshold -= roadSize;
        }
    }

    public void xPosCheck()
    {
        float currentZ = Mathf.Round(player.position.z / roadSize) * roadSize;


        if (player.position.x > xPositiveThreshold)
        {
            float spawnX = xPositiveThreshold + 30f;

            SpawnRoad(spawnX, currentZ);
            Converter(spawnX, currentZ);
            xPositiveThreshold += roadSize;
            xNegativeThreshold += roadSize;
        }
        else if (player.position.x < xNegativeThreshold)
        {
            float spawnX = xNegativeThreshold - 30f;
            SpawnRoad(spawnX, currentZ);
            Converter(spawnX, currentZ);
            xNegativeThreshold -= roadSize;
            xPositiveThreshold -= roadSize;
        }
    }


    public void SpawnRoad(float xPos, float zPos)
    {
        Vector3Int currentRoadPos = new Vector3Int (Mathf.RoundToInt(xPos),0,Mathf.RoundToInt(zPos));
        roadPos.Add(currentRoadPos);
        
        GameObject newRoad = Instantiate(roadPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity);

        roadQueue.Enqueue(newRoad);

        if (roadQueue.Count > maxActiveRoads )
        {
            GameObject oldRoad = roadQueue.Dequeue();
            Destroy(oldRoad);
        }

    }
}