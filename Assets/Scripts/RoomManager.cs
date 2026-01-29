using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

   
    [System.Serializable]
    public class RoomData
    {
        public string roomID;            
        public Vector3 centerPoint;      
        public float volume;            
        public float hardness;           
        public float jagness;
        public Bounds bounds;
        public HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    }

    public Dictionary<Vector3Int, RoomData> cellToRoomMap = new Dictionary<Vector3Int, RoomData>();

    public List<RoomData> allRooms = new List<RoomData>();

   
    public float nodeSize
    {
        get
        {
            return DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.nodeSize : 1.0f;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool TryGetRoomAt(Vector3 worldPos, out RoomData room)
    {
        Vector3Int gridPos = WorldToGrid(worldPos);
        return cellToRoomMap.TryGetValue(gridPos, out room);
    }


    public void RegisterRoom(RoomData newRoom)
    {
        if (newRoom == null) return;

         
        Vector3Int minGrid = WorldToGrid(newRoom.bounds.min);
        Vector3Int maxGrid = WorldToGrid(newRoom.bounds.max);

     
        for (int x = minGrid.x; x <= maxGrid.x; x++)
        {
            for (int y = minGrid.y; y <= maxGrid.y; y++)
            {
                for (int z = minGrid.z; z <= maxGrid.z; z++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);

                    
                    if (cellToRoomMap.ContainsKey(cell))
                    {
                        cellToRoomMap[cell] = newRoom;
                    }
                    else
                    {
                        cellToRoomMap.Add(cell, newRoom);
                    }

              
                    if (!newRoom.occupiedCells.Contains(cell))
                    {
                        newRoom.occupiedCells.Add(cell);
                    }
                }
            }
        }

        if (!allRooms.Contains(newRoom))
        {
            allRooms.Add(newRoom);
        }

        Debug.Log($"[RoomManager] Oda Kaydedildi (Bounds ile): {newRoom.roomID} | Hacim: {newRoom.volume:F1}");
    }


    public void InvalidateRoomAt(Vector3 worldPos)
    {
        if (TryGetRoomAt(worldPos, out RoomData room))
        {
            foreach (var cell in room.occupiedCells)
            {
                cellToRoomMap.Remove(cell);
            }
            allRooms.Remove(room);
            Debug.Log($"[RoomManager] Oda Silindi: {room.roomID}");
        }
    }


    public Vector3Int WorldToGrid(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x / nodeSize),
            Mathf.RoundToInt(pos.y / nodeSize),
            Mathf.RoundToInt(pos.z / nodeSize)
        );
    }
  
}