using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralBuildingGenerator : MonoBehaviour
{
    public static ProceduralBuildingGenerator HouseInstance { get; private set; }

    private void Awake()
    {
        if (HouseInstance != null && HouseInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            HouseInstance = this;
        }
    }

    public bool EnableDiagonals = true;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    public GameObject wallSolidPrefab;
    public GameObject wallDiagonalPrefab;
    public GameObject wallWindowPrefab;
    public GameObject wallDoorPrefab;
    public GameObject ceilingDiagonalPrefab;


    [Header("Concrete")]
    public GameObject wallConcrete;
    public GameObject wallDiagonalConcrete;

    [Header("Wood")]
    
    public GameObject wallWood;
    public GameObject wallDiagonalWood;
    
    
    [Header("Marble")]
    
    public GameObject wallMarble;
    public GameObject wallDiagonalMarble;

    [Header("Jagged")]

    public GameObject wallJagged;
    public GameObject wallDiagonalJagged;





    public GameObject nextPrefab;
    
    public int roomSize = 10;
    public int wallHeight = 4;
    public int wallWidth = 4;
    public int wallDepth = 2;
    public int houseCount = 0;
    public int maxHouseCount = 5;
    [Header("GameSeed")]
    public int gameSeed = 0;
    private int generatedSeed;
    
    System.Random rng;

    [Range(0, 100)] public int diagonalChance = 50; 

    private HashSet<Vector2Int> pickedTiles = new HashSet<Vector2Int>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private bool isDoorSpawned = false;

    private void Start()
    {
        GenerateSeed();
       

    }

    void GenerateSeed()
    {
        if (gameSeed == 0)
        {
         gameSeed = Random.Range(1, 1000000);
           
        }
       
       
            Debug.Log("Current seed is " + gameSeed);
        

            
    }

    public void GenerateHouse(Vector2Int pos)
    {
       
        int houseSpecificSeed = gameSeed + (pos.x * 1000) + (pos.y * 100);
        rng = new System.Random(houseSpecificSeed);


       
            ClearHouse();
      
        isDoorSpawned = false;
        NextPrefabPicker();
        GenerateTileMap(pos);
        

    }

    void GenerateTileMap(Vector2Int Pos) 
    {
       //FloorPlan

        Vector2Int currentPos = Pos;
        pickedTiles.Add(currentPos);

        for (int i = 0; i < roomSize; i++) 
        {
            Vector2Int direction = RandomDirection();
            currentPos += direction;
            pickedTiles.Add(currentPos);
        }
        foreach(Vector2Int tilepos in pickedTiles)
        {
            
           // SpawnFloor(tilepos);
           // SpawnCeiling(tilepos);

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
        int ws = wallWidth /2;
        
        bool left = pickedTiles.Contains(pos + Vector2Int.left);
        bool right = pickedTiles.Contains(pos + Vector2Int.right);
        bool up = pickedTiles.Contains(pos+Vector2Int.up);
        bool down = pickedTiles.Contains(pos+Vector2Int.down);

        bool blockLeft = false;
        bool blockRight= false;
        bool blockUp= false;
        bool blockDown = false;

        bool isDiagonal = false;
        float rot = 0f;

        //Diagonal Walls

        if (EnableDiagonals)
        {
            //up left corner
            if (!up && !left && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 315);
                blockUp = true;
                blockLeft = true;

                isDiagonal = true;
                rot = 315;
            }
            //up right corner
            if (!up && !right && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 45);
                blockUp = true;
                blockRight = true;

                isDiagonal = true;
                rot = 45;
            }
            // down left corner
            if (!down && !left && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 225);
                blockDown = true;
                blockLeft = true;

                isDiagonal = true;
                rot = 225;
            }

            // down right corner
            if (!down && !right && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 135);
                blockDown = true;
                blockRight = true;

                isDiagonal = true;
                rot = 135;
            }
        }
            if (isDiagonal)
            {
                SpawnDiagonalCeiling(pos, rot);
                SpawnDiagonalFloor(pos, rot);
            }
            else
            {
                SpawnCeiling(pos);
                SpawnFloor(pos);
            }

        



        //Straight Walls

       

        if (!left && !blockLeft) { SpawnWalls(pos, 270, ws * Vector3.left); }
        if (!right && !blockRight) { SpawnWalls(pos, 90, ws * Vector3.right); }
        if (!up && !blockUp) { SpawnWalls(pos, 0, ws*Vector3.forward); }
        if(!down&& !blockDown) { SpawnWalls(pos,180, ws* Vector3.back); }


    }

    void NextPrefabPicker()
    {


        if (!isDoorSpawned)
        {
            nextPrefab = wallDoorPrefab;
            isDoorSpawned = true;
        }
        else
        {


            if (rng.Next(0, 100) > 10)
            {
                nextPrefab = wallSolidPrefab;
            }
            else
            {
                nextPrefab = wallWindowPrefab;
            }

        }
    }

    private void SpawnWalls(Vector2Int tilepos, float rot,Vector3 offset)
    {

        


        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 2, tilepos.y * wallHeight);
        Vector3 finalPos = worldPos + offset;
        Quaternion rotation = Quaternion.Euler(0, rot, 0);
        GameObject obj = Instantiate(nextPrefab, finalPos, rotation, transform);
        spawnedObjects.Add(obj);

        NextPrefabPicker();
        

    }
    private void SpawnDiagonalWalls(Vector2Int tilepos, float rot)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 2, tilepos.y * wallHeight);
        
        Quaternion rotation = Quaternion.Euler(0, rot, 0);

        GameObject obj = Instantiate(wallDiagonalPrefab, worldPos, rotation, transform);
        spawnedObjects.Add(obj);
    }

    private void SpawnFloor(Vector2Int tilepos)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y * wallHeight);
        GameObject obj = Instantiate(floorPrefab,worldPos, Quaternion.identity, transform);

        spawnedObjects.Add(obj);
    }
    private void SpawnCeiling(Vector2Int tilepos)
    {

        

        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, wallHeight, tilepos.y * wallHeight);
        GameObject obj = Instantiate(ceilingPrefab, worldPos, Quaternion.identity, transform);

        spawnedObjects.Add(obj);


    }

    private void SpawnDiagonalFloor(Vector2Int tilepos, float rot)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y*wallHeight);
        Quaternion rotation = Quaternion.Euler(0,rot, 0);   
        GameObject obj = Instantiate(ceilingDiagonalPrefab, worldPos, rotation, transform); 

        spawnedObjects.Add(obj);
    }
    private void SpawnDiagonalCeiling(Vector2Int tilepos, float rot)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, wallHeight, tilepos.y * wallHeight);
        Quaternion rotation = Quaternion.Euler(0, rot, 0);
        GameObject obj = Instantiate(ceilingDiagonalPrefab, worldPos, rotation, transform);

        spawnedObjects.Add(obj);
    }
    public void ClearHouse()
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
