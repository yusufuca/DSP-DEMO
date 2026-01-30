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
        if (width < 0.8f) return;

        RoomManager.PortalData portal = new RoomManager.PortalData();
        portal.position = (start + end) / 2f;

        // Zemin
        float floorY = portal.position.y - 1.5f;
        if (Physics.Raycast(portal.position, Vector3.down, out RaycastHit hitDown, 5f, wallLayer))
            floorY = hitDown.point.y;

        portal.position.y = floorY + 1.0f; // Portal Merkezi
        portal.size = new Vector3(width, 2.0f, 0.5f);
        portal.rotation = Quaternion.LookRotation(facingDir);

        // İlk hesaplama (Default değerlerle)
        // Sonra RoomManager zaten UpdatePortalHitbox çağırıp düzeltecek
        UpdatePortalHitbox(portal, 4.0f, 0.5f, 4.0f, 0.2f);

        room.portals.Add(portal);
    }
    public static void UpdatePortalHitbox(RoomManager.PortalData portal, float depth, float innerPadding, float height, float widthPadding)
    {
        // 1. Toplam Derinlik: İçeri Pay + Dışarı Pay
        float totalDepth = innerPadding + depth;

        // 2. Merkez Kaydırma (Center Shift)
        // Hitbox'ın merkezi, Portal merkezinden dışarı doğru kaymalı.
        // Ne kadar? -> (Toplam Derinlik / 2) - İçeri Pay
        // Örn: Derinlik 4, İçeri 0.5 -> Toplam 4.5 -> Yarısı 2.25 -> Shift = 2.25 - 0.5 = 1.75 birim dışarı.

        float centerShift = (totalDepth / 2.0f) - innerPadding;
        Vector3 forward = portal.rotation * Vector3.forward;
        Vector3 newCenter = portal.position + (forward * centerShift);

        // Yüksekliği de yerden başlatmak yerine merkezden başlatıp büyütüyoruz
        // Ama Y eksenini portal merkezinde (yerden 1m yukarı) tutarsak, height 4 olunca yere -1, tavana +3 gider.
        // Bu yüzden Y'yi biraz yukarı kaldırabiliriz veya portal merkezine sabitleyebiliriz.
        // Şimdilik portal merkezine (yerden 1m) sabitliyoruz, height artarsa yere ve tavana eşit büyür.

        portal.triggerZone = new Bounds(newCenter, new Vector3(portal.size.x + widthPadding, height, totalDepth));
    }
}