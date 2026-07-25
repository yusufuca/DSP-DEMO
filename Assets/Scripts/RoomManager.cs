using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Hitbox Settings")]
    [Range(1f, 10f)] public float hitboxDepth = 4.0f;
    [Range(0f, 2f)] public float hitboxInnerPadding = 0.5f;
    [Range(2f, 10f)] public float hitboxHeight = 4.0f;
    [Range(0f, 2f)] public float hitboxWidthPadding = 0.2f;

    [Header("Room Detection Settings")]
    [Range(0, 3)] public int roomExpansionSteps = 1;

    [Header("Portal Scanner (Barkod Ayarları)")]
    [Tooltip("Tarama hassasiyeti (Metre). 0.1 = Her 10cm'de bir ışın atar. Ne kadar düşükse o kadar hassas.")]
    [Range(0.05f, 0.5f)] public float scanStepSize = 0.1f;

    [Tooltip("Işın başlangıç noktası kaydırma. Eksi değer: İçeriden dışarı atar (Önerilen).")]
    [Range(-1.0f, 0.5f)] public float portalRayOffset = -0.1f;

    [Tooltip("Işın menzili. Duvarı delip geçecek kadar olmalı.")]
    public float portalRayLength = 2.0f;

    [System.Serializable]
    public class PortalData
    {
        public Vector3 position;
        public Vector3 size;
        public Quaternion rotation;
        public Bounds triggerZone;
    }

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
        public List<PortalData> portals = new List<PortalData>();
    }

    public Dictionary<Vector3Int, RoomData> cellToRoomMap = new Dictionary<Vector3Int, RoomData>();
    public List<RoomData> allRooms = new List<RoomData>();

    public float nodeSize
    {
        get { return DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.nodeSize : 2.0f; }
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void OnValidate()
    {
        if (allRooms != null)
        {
            foreach (var room in allRooms)
            {
                if (room.portals != null)
                {
                    foreach (var portal in room.portals)
                        PortalScanner.UpdatePortalHitbox(portal, hitboxDepth, hitboxInnerPadding, hitboxHeight, hitboxWidthPadding);
                }
            }
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
        foreach (var cell in newRoom.occupiedCells)
        {
            if (!cellToRoomMap.ContainsKey(cell)) cellToRoomMap.Add(cell, newRoom);
            else cellToRoomMap[cell] = newRoom;
        }

        if (!allRooms.Contains(newRoom)) allRooms.Add(newRoom);

        LayerMask wallLayer = DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.WallLayer : LayerMask.GetMask("Wall");
        PortalScanner.ScanRoomPortals(newRoom, wallLayer);

        Debug.Log($"[RoomManager] Room Saved: {newRoom.roomID} | Cells: {newRoom.occupiedCells.Count}");
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