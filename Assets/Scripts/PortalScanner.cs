using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class PortalScanner
{
    // Bir portal parçası: Başlangıç ve Bitiş noktası belli olan bir çizgi
    public class PortalSegment
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3Int direction;
    }

    public static void ScanRoomPortals(RoomManager.RoomData room, LayerMask wallLayer)
    {
        room.portals.Clear();

        // --- AYARLAR ---
        float nodeSize = RoomManager.Instance.nodeSize;
        float stepSize = RoomManager.Instance.scanStepSize; // Örn: 0.1f (10cm)
        float rayOffset = RoomManager.Instance.portalRayOffset; // Örn: -0.1f (Geri çekilme)
        float rayLength = RoomManager.Instance.portalRayLength; // Örn: 2.0f

        // Yükseklik: Odanın tam ortası
        float rayHeight = room.bounds.center.y;

        List<PortalSegment> rawSegments = new List<PortalSegment>();
        Vector3Int[] directions = { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };

        // --- ADIM 1: SINIRLARI BUL VE TARA (BARCODE SCAN) ---
        foreach (var cell in room.occupiedCells)
        {
            Vector3 cellCenter = new Vector3(cell.x * nodeSize, rayHeight, cell.z * nodeSize);

            foreach (var dir in directions)
            {
                // Yanımızdaki hücre odada YOKSA -> Sınırdır.
                if (!room.occupiedCells.Contains(cell + dir))
                {
                    // --- BU KENARI TARA ---
                    ScanEdgeLinear(cellCenter, dir, nodeSize, rayHeight, stepSize, rayOffset, rayLength, wallLayer, rawSegments);
                }
            }
        }

        // --- ADIM 2: PARÇALARI BİRLEŞTİR (MERGE) ---
        MergeSegmentsAndCreatePortals(room, rawSegments);
    }

    // Bir kenarı baştan sona milim milim tarar
    private static void ScanEdgeLinear(Vector3 cellCenter, Vector3Int dir, float nodeSize, float yPos, float step, float offset, float length, LayerMask layer, List<PortalSegment> segments)
    {
        Vector3 dirVec = new Vector3(dir.x, 0, dir.z);

        // Yönün sağı (Tarama Hattı)
        Vector3 rightVec = Vector3.Cross(Vector3.up, dirVec);

        // Kenarın başlangıcı (Sol) ve Bitişi (Sağ)
        // Kenar uzunluğu nodeSize kadardır. Merkezden sola ve sağa nodeSize/2 gideriz.
        Vector3 edgeCenter = cellCenter + (dirVec * (nodeSize * 0.5f)); // Kenarın tam ortası (Sınır çizgisi)

        // Raycast Başlangıç Hattı (Offset eklenmiş hali - İçeri çekilmiş hat)
        Vector3 scanLineCenter = edgeCenter + (dirVec * offset);

        Vector3 startPoint = scanLineCenter - (rightVec * (nodeSize * 0.5f));
        Vector3 endPoint = scanLineCenter + (rightVec * (nodeSize * 0.5f));

        // --- TARAMA DÖNGÜSÜ ---
        bool inGap = false;
        Vector3 gapStart = Vector3.zero;

        // Kaç adım atacağız?
        int steps = Mathf.CeilToInt(nodeSize / step);

        for (int i = 0; i <= steps; i++)
        {
            // Lerp ile hat üzerinde yürü
            float t = (float)i / steps;
            Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, t);

            // Raycast At
            bool hitWall = Physics.Raycast(currentPos, dirVec, length, layer);

            // DURUM MAKİNESİ (State Machine)
            if (!hitWall)
            {
                // Duvar YOK -> Boşluk
                if (!inGap)
                {
                    // Boşluk yeni başladı
                    inGap = true;
                    gapStart = currentPos;
                }
            }
            else
            {
                // Duvar VAR -> Kapalı
                if (inGap)
                {
                    // Boşluk bitti, kaydet!
                    inGap = false;
                    segments.Add(new PortalSegment
                    {
                        start = gapStart,
                        end = currentPos, // Bittiği yer
                        direction = dir
                    });
                }
            }
        }

        // Kenar bittiğinde hala boşluktaysak, kenar sonuna kadar kaydet
        if (inGap)
        {
            segments.Add(new PortalSegment
            {
                start = gapStart,
                end = endPoint,
                direction = dir
            });
        }
    }

    // --- ADIM 2: BİRLEŞTİRME ---
    private static void MergeSegmentsAndCreatePortals(RoomManager.RoomData room, List<PortalSegment> segments)
    {
        if (segments.Count == 0) return;

        // Yöne göre grupla
        var groupedByDir = segments.GroupBy(s => s.direction);

        foreach (var group in groupedByDir)
        {
            Vector3Int facingDir = group.Key;
            List<PortalSegment> dirSegments = group.ToList();

            // Segmentleri hizaya sokmamız lazım ki birleştirebilelim.
            // Neyle sıralayacağız? Tarama eksenindeki pozisyonlarına göre.
            // Kuzey/Güney bakıyorsa -> X eksenine göre.
            // Doğu/Batı bakıyorsa -> Z eksenine göre.

            // Basit çözüm: Tüm segmentleri tek tek gezip birleşebilenleri birleştirelim.
            // Iterative Merge
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < dirSegments.Count; i++)
                {
                    for (int j = i + 1; j < dirSegments.Count; j++)
                    {
                        PortalSegment s1 = dirSegments[i];
                        PortalSegment s2 = dirSegments[j];

                        // Birleşebilirler mi?
                        // (S1 sonu S2 başına yakın mı? Veya tam tersi?)
                        float dist1 = Vector3.Distance(s1.end, s2.start);
                        float dist2 = Vector3.Distance(s2.end, s1.start);

                        // Hata payı (Step size kadar veya biraz fazla)
                        float threshold = RoomManager.Instance.scanStepSize * 1.5f;

                        if (dist1 < threshold)
                        {
                            // S1 -> S2 birleşir
                            s1.end = s2.end; // S1'i uzat
                            dirSegments.RemoveAt(j); // S2'yi sil
                            changed = true;
                            break;
                        }
                        else if (dist2 < threshold)
                        {
                            // S2 -> S1 birleşir
                            s1.start = s2.start; // S1'i geriye uzat
                            dirSegments.RemoveAt(j); // S2'yi sil
                            changed = true;
                            break;
                        }
                    }
                    if (changed) break;
                }
            }

            // Kalan birleşmiş segmentlerden Portal oluştur
            foreach (var seg in dirSegments)
            {
                CreateFinalPortal(room, seg);
            }
        }
    }

    private static void CreateFinalPortal(RoomManager.RoomData room, PortalSegment seg)
    {
        // Genişlik
        float width = Vector3.Distance(seg.start, seg.end);

        // Çok küçük delikleri (örn raycast hatası) ele
        if (width < 0.3f) return;

        // Merkez
        Vector3 center = (seg.start + seg.end) / 2.0f;

        // Yükseklik Ayarı (Zemine oturtma)
        // Odanın merkezinden aşağı ray atıp zemini bulalım (Daha güvenli)
        float floorY = room.bounds.min.y;
        Vector3 rayOrigin = new Vector3(center.x, room.centerPoint.y + 1.0f, center.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            // Floor veya Wall layer'a çarpması lazım
            floorY = hit.point.y;
        }
        center.y = floorY + 1.0f; // Portalın görsel merkezi

        // Yönü Düzelt (Segmentin yönü Grid yönüydü, Rotation için Vector3 lazım)
        Vector3 dirVec = new Vector3(seg.direction.x, 0, seg.direction.z);

        // --- PORTAL DATA OLUŞTUR ---
        RoomManager.PortalData p = new RoomManager.PortalData();
        p.position = center;
        p.rotation = Quaternion.LookRotation(dirVec);
        p.size = new Vector3(width, 2.0f, 0.5f); // Yükseklik 2m standart

        // Hitbox
        if (RoomManager.Instance != null)
        {
            UpdatePortalHitbox(p, RoomManager.Instance.hitboxDepth, RoomManager.Instance.hitboxInnerPadding, RoomManager.Instance.hitboxHeight, RoomManager.Instance.hitboxWidthPadding);
        }
        else UpdatePortalHitbox(p, 4.0f, 0.5f, 4.0f, 0.2f);

        room.portals.Add(p);
    }

    public static void UpdatePortalHitbox(RoomManager.PortalData portal, float depth, float innerPadding, float height, float widthPadding)
    {
        // 1. Toplam Derinlik
        float totalDepth = innerPadding + depth;

        // 2. Merkez Kaydırma (Local Shift)
        // Artık "World Position" hesaplamıyoruz. Sadece "Ne kadar ileri gideyim?" hesabı yapıyoruz.
        // Portalın merkezi (0,0,0) kabul edilirse, ileriye (Z) ne kadar gideceğiz?
        float localZShift = (totalDepth / 2.0f) - innerPadding;

        // --- KRİTİK DEĞİŞİKLİK: LOCAL SPACE ---
        // Bounds merkezini (0, 0, localZShift) yapıyoruz.
        // Yani portalın tam göbeğinden, hesapladığımız kadar ileriye (Z ekseninde) koyuyoruz.
        Vector3 localCenter = new Vector3(0, 0, localZShift);

        // Boyutlar (Local Size)
        // Genişlik (X), Yükseklik (Y), Derinlik (Z)
        Vector3 localSize = new Vector3(portal.size.x + widthPadding, height, totalDepth);

        // Artık bu Bounds, dünya ekseninden bağımsız, sadece portalın "Vücut Ölçülerini" tutuyor.
        portal.triggerZone = new Bounds(localCenter, localSize);
    }
}