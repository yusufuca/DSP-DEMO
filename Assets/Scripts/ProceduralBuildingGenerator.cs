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
    public GameObject roomTypePrefab;
    private int roomType;


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

    public void GenerateHouse(Vector2Int pos, Transform roadParent)
    {
       
        int houseSpecificSeed = gameSeed + (pos.x * 1000) + (pos.y * 100);
        rng = new System.Random(houseSpecificSeed);


        pickedTiles.Clear();
        isDoorSpawned = false;
        RoomTypePicker();
        NextPrefabPicker();

   

        GenerateTileMap(pos, roadParent);

       

    }

    void GenerateTileMap(Vector2Int Pos, Transform roadParent) 
    {
       //FloorPlan

        Vector2Int currentPos = Pos;
        pickedTiles.Add(currentPos);

        int buildingSize = rng.Next(10, 50);
        Debug.Log($"RoomSize is: {buildingSize}");

        for (int i = 0; i < buildingSize; i++) 
        {
            Vector2Int direction = RandomDirection();
            currentPos += direction;
            pickedTiles.Add(currentPos);
        }
        foreach(Vector2Int tilepos in pickedTiles)
        {
            
           // SpawnFloor(tilepos);
           // SpawnCeiling(tilepos);

            CheckAndSpawn(tilepos,roadParent);
            
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

    void CheckAndSpawn(Vector2Int pos, Transform roadParent)
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
                SpawnDiagonalWalls(pos, 315, roadParent);
                blockUp = true;
                blockLeft = true;

                isDiagonal = true;
                rot = 315;
            }
            //up right corner
            if (!up && !right && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 45, roadParent);
                blockUp = true;
                blockRight = true;

                isDiagonal = true;
                rot = 45;
            }
            // down left corner
            if (!down && !left && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 225, roadParent);
                blockDown = true;
                blockLeft = true;

                isDiagonal = true;
                rot = 225;
            }

            // down right corner
            if (!down && !right && rng.Next(0, 100) < diagonalChance)
            {
                SpawnDiagonalWalls(pos, 135, roadParent);
                blockDown = true;
                blockRight = true;

                isDiagonal = true;
                rot = 135;
            }
        }
            if (isDiagonal)
            {
                SpawnDiagonalCeiling(pos, rot, roadParent);
                SpawnDiagonalFloor(pos, rot, roadParent);
            }
            else
            {
                SpawnCeiling(pos, roadParent);
                SpawnFloor(pos, roadParent);
            }

        



        //Straight Walls

       

        if (!left && !blockLeft) { SpawnWalls(pos, 270, ws * Vector3.left, roadParent); }
        if (!right && !blockRight) { SpawnWalls(pos, 90, ws * Vector3.right, roadParent); }
        if (!up && !blockUp) { SpawnWalls(pos, 0, ws*Vector3.forward, roadParent); }
        if(!down&& !blockDown) { SpawnWalls(pos,180, ws* Vector3.back, roadParent); }


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

            // wall or window picker
            if (rng.Next(0, 100) > 10)
            {
               if(roomType == 3)
                {
                    int randomWallPick = rng.Next(0,3);
                    if (randomWallPick == 0)
                    {
                        nextPrefab = wallWood;
                    }
                    if(randomWallPick == 1)
                    {
                        nextPrefab = wallConcrete;
                    }
                    if(randomWallPick == 2)
                    {
                        nextPrefab = wallMarble;
                    }
                }
                else
                {
                    nextPrefab = roomTypePrefab;
                }

                    
            }
            else
            {
                nextPrefab = wallWindowPrefab;
            }

        }
    }

    void RoomTypePicker()
    {
         roomType = rng.Next(0, 4);
        if(roomType == 0)
        {
            roomTypePrefab = wallWood;
        }
        if(roomType == 1)
        {
            roomTypePrefab = wallConcrete;
        }
        if(roomType == 2)
        {
            roomTypePrefab = wallMarble;
        }
    
    }

    private void SpawnWalls(Vector2Int tilepos, float rot,Vector3 offset, Transform roadParent)
    {

        


        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 2, tilepos.y * wallHeight);
        Vector3 finalPos = worldPos + offset;
        Quaternion rotation = Quaternion.Euler(0, rot, 0);
        Instantiate(nextPrefab, finalPos, rotation, roadParent);

     
        NextPrefabPicker();
        

    }
    private void SpawnDiagonalWalls(Vector2Int tilepos, float rot,Transform roadParent)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 2, tilepos.y * wallHeight);
        
        Quaternion rotation = Quaternion.Euler(0, rot, 0);

         Instantiate(wallDiagonalPrefab, worldPos, rotation, roadParent);
        
    }

    private void SpawnFloor(Vector2Int tilepos, Transform roadParent)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y * wallHeight);
        Instantiate(floorPrefab,worldPos, Quaternion.identity, roadParent);

        
    }
    private void SpawnCeiling(Vector2Int tilepos, Transform roadParent)
    {

        

        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, wallHeight, tilepos.y * wallHeight);
        Instantiate(ceilingPrefab, worldPos, Quaternion.identity, roadParent);

        


    }

    private void SpawnDiagonalFloor(Vector2Int tilepos, float rot, Transform roadParent)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, 0, tilepos.y*wallHeight);
        Quaternion rotation = Quaternion.Euler(0,rot, 0);   
        Instantiate(ceilingDiagonalPrefab, worldPos, rotation, roadParent); 

       
    }
    private void SpawnDiagonalCeiling(Vector2Int tilepos, float rot, Transform roadParent)
    {
        Vector3 worldPos = new Vector3(tilepos.x * wallWidth, wallHeight, tilepos.y * wallHeight);
        Quaternion rotation = Quaternion.Euler(0, rot, 0);
        Instantiate(ceilingDiagonalPrefab, worldPos, rotation, roadParent);

        
    }
    

}
