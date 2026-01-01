using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralBuildingGenerator : MonoBehaviour
{
    // Variables 

    [Header("Prefabs")]
    public GameObject floor;
    public GameObject wallSolid;
    public GameObject wallDiagonal;
    public GameObject wallWindow;
    public GameObject wallDoor;
    public GameObject ceiling;

    [Header("Settings")]
    public int buildingSize = 10;
    public int wallHeight = 4;
    public int tileSize = 4;

    public int gameSeed = 0;
    private int currentSeed;

    private HashSet<Vector2Int> pickedTiles = new HashSet<Vector2Int>();
    private System.Random rng;




    // Generate Seed

    public void resetSeed()
    {
        if(gameSeed == 0)
        {
           currentSeed = Random.Range(1, 1000000);

        }
        else
        {
            currentSeed = gameSeed;
        }
    }
    // Generate House


    // Drunk Walker House Shape Creation

    // Check and Spawn Walls 

    // Spawner Methods 
}
