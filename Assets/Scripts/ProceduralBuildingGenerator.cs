using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralBuildingGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    public GameObject wallSolidPrefab;
    public GameObject wallDiagonalPrefab;
    public GameObject wallWindowPrefab;
    public GameObject wallDoorPrefab;

    public int roomSize = 10;
    public int wallHeight = 4;
    public int wallWidth = 4;
    public int wallDepth = 2;

    [Header("GameSeed")]
    public int gameSeed = 0;
    private int generatedSeed;
    
    System.Random rng;

    private HashSet<Vector2Int> pickedTiles = new HashSet<Vector2Int>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Start()
    {
        GenerateSeed();
        GenerateHouse();
            
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F)) 
        {
            GenerateHouse();
           
        }
    }
    void GenerateSeed()
    {
        if (gameSeed == 0)
        {
         gameSeed = Random.Range(1, 1000000);
            Debug.Log("Current seed is " + gameSeed);
        }
        else
        {
            Debug.Log("Current seed is " + gameSeed);
        }

            rng = new System.Random(gameSeed);
    }

    void GenerateHouse()
    {
        ClearHouse();
        GenerateTileMap();
        

    }

    void GenerateTileMap()
    {
       //FloorPlan

        Vector2Int currentPos = Vector2Int.zero;
        pickedTiles.Add(currentPos);

        for (int i = 0; i < roomSize; i++) 
        {
            Vector2Int direction = RandomDirection();
            currentPos += direction;
            pickedTiles.Add(currentPos);
        }
        foreach(Vector2Int tilepos in pickedTiles)
        {
            SpawnFloor(tilepos);
            CheckAndSpawn(tilepos);
            
        }

    }

    private Vector2Int RandomDirection()
    {
        int pickTile = rng.Next(0, 4);

        if (pickTile == 0) return Vector2Int.up;
        if (pickTile == 1) return Vector2Int.down;
        if (pickTile == 2) return Vector2Int.left;
        else return Vector2Int.right;
                
    }

    void CheckAndSpawn(Vector2Int pos)
    {
        int ws = wallWidth / 2;
        
        bool left = pickedTiles.Contains(pos + Vector2Int.left);
        bool right = pickedTiles.Contains(pos + Vector2Int.right);
        bool up = pickedTiles.Contains(pos+Vector2Int.up);
        bool down = pickedTiles.Contains(pos+Vector2Int.down);


        if (!left) { SpawnWalls(pos, 270, ws * Vector3.left); }
        if (!right) { SpawnWalls(pos, 90, ws * Vector3.right); }
        if (!up) { SpawnWalls(pos, 0, ws*Vector3.forward); }
        if(!down) { SpawnWalls(pos,180, ws* Vector3.back); }


    }



    private void SpawnWalls(Vector2Int tilepos, float rot,Vector3 offset)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y * wallHeight);
        Vector3 finalPos = worldPos + offset;
        Quaternion rotation = Quaternion.Euler(0, rot, 0);
        GameObject obj = Instantiate(wallSolidPrefab, finalPos, rotation, transform);
        spawnedObjects.Add(obj);
    }

    private void SpawnFloor(Vector2Int tilepos)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y * wallHeight);
        GameObject obj = Instantiate(floorPrefab,worldPos, Quaternion.identity, transform);

        spawnedObjects.Add(obj);
    }

    void ClearHouse()
    {
        // Destroy all the old game objects
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
        pickedTiles.Clear();
    }

}
