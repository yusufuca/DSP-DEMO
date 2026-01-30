using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [System.Serializable]
    public class PortalData
    {
        public Vector3 position;
        public Vector3 size;
        public Quaternion rotation;
        public Bounds triggerZone; // EKSİK OLAN BUYDU, EKLENDİ
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

    // Debug Görselleri
    [Header("Debug Visuals")]
    public bool showDebug = true;
    public Color portalColor = new Color(0, 1, 1, 0.5f);
    public Color roomBoundsColor = new Color(0, 1, 0, 0.1f);

    public float nodeSize
    {
        get { return DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.nodeSize : 2.0f; }
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
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

        // Odayı kaydederken Portalları tara
        LayerMask wallLayer = DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.WallLayer : LayerMask.GetMask("Wall");
        PortalScanner.ScanRoomPortals(newRoom, wallLayer);

        Debug.Log($"[RoomManager] Oda Kaydedildi: {newRoom.roomID} | Portal: {newRoom.portals.Count}");
    }

    public Vector3Int WorldToGrid(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x / nodeSize),
            Mathf.RoundToInt(pos.y / nodeSize),
            Mathf.RoundToInt(pos.z / nodeSize)
        );
    }

    // Görsel Debug Çizimi
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        foreach (var room in allRooms)
        {
            if (room == null) continue;

            Gizmos.color = roomBoundsColor;
            Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);

            if (room.portals != null)
            {
                foreach (var portal in room.portals)
                {
                    Gizmos.color = portalColor;
                    Gizmos.matrix = Matrix4x4.TRS(portal.position, portal.rotation, portal.size);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = Matrix4x4.identity;

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(portal.position, portal.rotation * Vector3.forward * 1.5f);

                    // Trigger Zone
                    Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
                    Gizmos.DrawWireCube(portal.triggerZone.center, portal.triggerZone.size);
                }
            }
        }
    }
}