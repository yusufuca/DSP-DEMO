using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Hitbox Settings (Tweak Here)")]
    [Tooltip("Hitbox'ın kapıdan dışarı ne kadar uzayacağı (Derinlik)")]
    [Range(1f, 10f)] public float hitboxDepth = 4.0f;

    [Tooltip("Hitbox'ın kapıdan içeri ne kadar gireceği (Hata payı)")]
    [Range(0f, 2f)] public float hitboxInnerPadding = 0.5f;

    [Tooltip("Hitbox'ın yüksekliği")]
    [Range(2f, 10f)] public float hitboxHeight = 4.0f;

    [Tooltip("Hitbox'ın genişliğine eklenecek ekstra pay")]
    [Range(0f, 2f)] public float hitboxWidthPadding = 0.2f;

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

    [Header("Debug Visuals")]
    public bool showDebug = true;
    public Color portalColor = new Color(0, 1, 1, 0.5f);
    public Color roomBoundsColor = new Color(0, 1, 0, 0.1f);
    public Color hitboxColor = new Color(1, 0.5f, 0, 0.4f); // Turuncu

    public float nodeSize
    {
        get { return DetectingWall.DetectInstance != null ? DetectingWall.DetectInstance.nodeSize : 2.0f; }
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- SLIDER DEĞİŞİNCE HITBOX'LARI GÜNCELLE ---
    private void OnValidate()
    {
        // Editörde slider ile oynadığında hitboxları yeniden hesapla
        if (allRooms != null)
        {
            foreach (var room in allRooms)
            {
                if (room.portals != null)
                {
                    foreach (var portal in room.portals)
                    {
                        PortalScanner.UpdatePortalHitbox(portal, hitboxDepth, hitboxInnerPadding, hitboxHeight, hitboxWidthPadding);
                    }
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

        // İlk tarama (Varsayılan değerlerle)
        PortalScanner.ScanRoomPortals(newRoom, wallLayer);

        // Sonra bizim slider ayarlarıyla güncelle
        foreach (var portal in newRoom.portals)
            PortalScanner.UpdatePortalHitbox(portal, hitboxDepth, hitboxInnerPadding, hitboxHeight, hitboxWidthPadding);

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
                    // Portal Çerçevesi
                    Gizmos.color = portalColor;
                    Gizmos.matrix = Matrix4x4.TRS(portal.position, portal.rotation, portal.size);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = Matrix4x4.identity;

                    // Yön Oku
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(portal.position, portal.rotation * Vector3.forward * 1.5f);

                    // HITBOX (Turuncu Kutu)
                    Gizmos.color = hitboxColor;
                    // Matrix kullanarak rotasyonlu Bounds çiziyoruz ki kutu yamuk durmasın
                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(portal.triggerZone.center, portal.rotation, portal.triggerZone.size);
                    Gizmos.matrix = rotationMatrix;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }
}