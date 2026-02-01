using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(RoomManager))]
public class RoomVisualizer : MonoBehaviour
{
    [Header("Visibility Toggles")]
    public bool showCells = true;
    public bool showPortals = true;
    public bool showHitboxes = true;
    public bool showBounds = false; // Dikdörtgen kutuyu görmek ister misin?
    public bool showLabels = true;

    [Header("Colors")]
    public Color cellColor = new Color(0, 1, 0, 0.3f);
    public Color portalColor = new Color(0, 1, 1, 0.5f);
    public Color hitboxColor = new Color(1, 0.5f, 0, 0.2f);
    public Color boundsColor = new Color(1, 1, 1, 0.1f);

    private RoomManager manager;

    void Start()
    {
        manager = GetComponent<RoomManager>();
    }

    void OnDrawGizmos()
    {
        if (manager == null) manager = GetComponent<RoomManager>();
        if (manager == null || manager.allRooms == null) return;

        float nodeSize = manager.nodeSize;

        foreach (var room in manager.allRooms)
        {
            if (room == null) continue;

            // 1. BOUNDS (Dikdörtgen Kutu)
            if (showBounds)
            {
                Gizmos.color = boundsColor;
                Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);
            }

            // 2. CELLS (Gerçek Oda Şekli)
            if (showCells)
            {
                Gizmos.color = cellColor;
                foreach (var cell in room.occupiedCells)
                {
                    Vector3 worldPos = new Vector3(cell.x * nodeSize, cell.y * nodeSize, cell.z * nodeSize);
                    Gizmos.DrawCube(worldPos, Vector3.one * (nodeSize * 0.9f));
                }
            }

            // 3. PORTALS & HITBOXES
            if (room.portals != null && (showPortals || showHitboxes))
            {
                foreach (var portal in room.portals)
                {
                    // Portal Görseli (Mavi Çerçeve)
                    if (showPortals)
                    {
                        Gizmos.color = portalColor;
                        Gizmos.matrix = Matrix4x4.TRS(portal.position, portal.rotation, portal.size);
                        Gizmos.DrawWireCube(Vector3.zero, Vector3.one); // Portalın kendi çerçevesi
                        Gizmos.matrix = Matrix4x4.identity;

                        // Yön Oku
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawRay(portal.position, portal.rotation * Vector3.forward * 1.5f);
                    }

                    // Hitbox Görseli (Turuncu Alan) - GÜNCELLENDİ
                    if (showHitboxes)
                    {
                        Gizmos.color = hitboxColor;

                        // --- DOĞRU ÇİZİM MANTIĞI ---
                        // 1. Matrisi Portalın olduğu yere taşı ve döndür (TRS: Translation, Rotation, Scale=1)
                        // Scale'i 1 tutuyoruz çünkü boyutu Bounds'un kendisi belirleyecek.
                        Gizmos.matrix = Matrix4x4.TRS(portal.position, portal.rotation, Vector3.one);

                        // 2. Şimdi Local Bounds'u çiz.
                        // portal.triggerZone.center artık (0, 0, shift) verisini tutuyor.
                        // Matris bizi zaten portalın göbeğine getirdiği için, bu center bizi doğru yere öteleyecek.
                        Gizmos.DrawCube(portal.triggerZone.center, portal.triggerZone.size);

                        Gizmos.color = new Color(1, 0, 0, 0.5f); // Kırmızı Çerçeve
                        Gizmos.DrawWireCube(portal.triggerZone.center, portal.triggerZone.size);

                        // 3. Matrisi sıfırla ki diğer çizimler bozulmasın
                        Gizmos.matrix = Matrix4x4.identity;
                    }
                }
            }

            // 4. LABELS (Bilgi Yazıları)
#if UNITY_EDITOR
            if (showLabels)
            {
                string info = $"{room.roomID}\nVol: {room.volume:F0}";
                // Yazıyı odanın tepesine koy (Center + Height/2)
                Vector3 labelPos = room.bounds.center + Vector3.up * (room.bounds.extents.y + 1f);
                Handles.Label(labelPos, info);
            }
#endif
        }
    }
}