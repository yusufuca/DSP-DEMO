using UnityEngine;
using System.Collections.Generic;

public static class PortalScanner
{
    // FloodFill'de Expand(2f) yaptığımız için her kenarda 1 birim şişme var.
    private const float EXPANSION_MARGIN = 1.0f;

    public static void ScanRoomPortals(RoomManager.RoomData room, LayerMask wallLayer)
    {
        room.portals.Clear();

        float stepSize = 1.0f; // 1 metrede bir tara (Daha hassas olsun)

        // 1. ZEMİNİ BUL (Center'dan aşağı ray at)
        float floorY = room.bounds.min.y;
        if (Physics.Raycast(room.centerPoint, Vector3.down, out RaycastHit floorHit, 100f, wallLayer))
        {
            floorY = floorHit.point.y;
        }
        else
        {
            // Ray bulamazsa bounds'u daraltarak tahmin et
            floorY = room.bounds.min.y + EXPANSION_MARGIN;
        }

        // Tarama yüksekliği: Zeminden 1.5m yukarı
        float scanHeight = floorY + 1.5f;

        // Gerçek Duvar Sınırları (Şişirmeyi geri alıyoruz)
        float minX = room.bounds.min.x + EXPANSION_MARGIN;
        float maxX = room.bounds.max.x - EXPANSION_MARGIN;
        float minZ = room.bounds.min.z + EXPANSION_MARGIN;
        float maxZ = room.bounds.max.z - EXPANSION_MARGIN;

        // --- 4 DUVARI TARA ---

        // Güney Duvarı (Z = minZ) | X boyunca ilerle | Geriye (Back) bak
        ScanEdge(room, new Vector3(minX, scanHeight, minZ), Vector3.right, (maxX - minX), Vector3.back, wallLayer, stepSize);

        // Kuzey Duvarı (Z = maxZ) | X boyunca ilerle | İleriye (Forward) bak
        ScanEdge(room, new Vector3(minX, scanHeight, maxZ), Vector3.right, (maxX - minX), Vector3.forward, wallLayer, stepSize);

        // Batı Duvarı (X = minX) | Z boyunca ilerle | Sola (Left) bak
        ScanEdge(room, new Vector3(minX, scanHeight, minZ), Vector3.forward, (maxZ - minZ), Vector3.left, wallLayer, stepSize);

        // Doğu Duvarı (X = maxX) | Z boyunca ilerle | Sağa (Right) bak
        ScanEdge(room, new Vector3(maxX, scanHeight, minZ), Vector3.forward, (maxZ - minZ), Vector3.right, wallLayer, stepSize);
    }

    private static void ScanEdge(RoomManager.RoomData room, Vector3 start, Vector3 moveDir, float length, Vector3 lookDir, LayerMask wallLayer, float step)
    {
        int steps = Mathf.CeilToInt(length / step);
        Vector3 gapStart = Vector3.zero;
        bool isGap = false;

        // İçeriden dışarı bakacağımız için origin'i biraz içeri çekelim (0.5m)
        // lookDir dışarıyı gösteriyor, biz tersine gidip içeri giriyoruz.
        Vector3 insetOffset = -lookDir * 0.5f;

        for (int i = 0; i <= steps; i++)
        {
            Vector3 currentPos = start + (moveDir * (i * step));
            Vector3 rayOrigin = currentPos + insetOffset;

            // 1.5m uzağa ray at (0.5m içerideyiz + 1m duvar kalınlığı için)
            bool hitWall = Physics.Raycast(rayOrigin, lookDir, 2.0f, wallLayer);

            // Debug çizgileri (Scene'de görmek için)
            // Debug.DrawRay(rayOrigin, lookDir * 2.0f, hitWall ? Color.red : Color.green, 10f);

            if (!hitWall)
            {
                // BOŞLUK
                if (!isGap)
                {
                    gapStart = currentPos;
                    isGap = true;
                }
            }
            else
            {
                // DUVAR
                if (isGap)
                {
                    // Boşluk bitti, kaydet
                    FinalizePortal(room, gapStart, currentPos - (moveDir * (step * 0.5f)), lookDir, wallLayer);
                    isGap = false;
                }
            }
        }

        // Köşeye gelince hala boşluksa kapat
        if (isGap)
        {
            FinalizePortal(room, gapStart, start + (moveDir * length), lookDir, wallLayer);
        }
    }

    private static void FinalizePortal(RoomManager.RoomData room, Vector3 start, Vector3 end, Vector3 facingDir, LayerMask wallLayer)
    {
        float width = Vector3.Distance(start, end);
        if (width < 0.8f) return; // Çok dar delikleri yoksay

        RoomManager.PortalData portal = new RoomManager.PortalData();
        portal.position = (start + end) / 2f; // Yatay merkez

        // Yükseklik Taraması
        float floorY = portal.position.y - 1.5f;
        float ceilY = portal.position.y + 1.5f;

        // Merkezden yukarı ve aşağı ray atarak tavan/zemin bul
        if (Physics.Raycast(portal.position, Vector3.up, out RaycastHit hitUp, 5f, wallLayer))
            ceilY = hitUp.point.y;

        if (Physics.Raycast(portal.position, Vector3.down, out RaycastHit hitDown, 5f, wallLayer))
            floorY = hitDown.point.y;

        float height = ceilY - floorY;

        // Yüksekliği düzelt (Pivot merkezde)
        portal.position.y = floorY + (height / 2f);

        portal.size = new Vector3(width, height, 0.5f);
        portal.rotation = Quaternion.LookRotation(facingDir);

        // Trigger Zone: Kapıdan dışarı ve içeri 1'er metre taşan bir kutu
        portal.triggerZone = new Bounds(portal.position, new Vector3(width, height, 2.0f));

        room.portals.Add(portal);
    }
}