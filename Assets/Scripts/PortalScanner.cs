using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class PortalScanner
{
    public class PortalSegment
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3Int direction;
    }

    public static void ScanRoomPortals(RoomManager.RoomData room, LayerMask wallLayer)
    {
        room.portals.Clear();

        // AYARLAR (RoomManager 00.13 Referanslı)
        float nodeSize = RoomManager.Instance.nodeSize;
        float stepSize = RoomManager.Instance.scanStepSize;
        float rayOffset = RoomManager.Instance.portalRayOffset;
        float rayLength = RoomManager.Instance.portalRayLength;

        float rayHeight = room.bounds.center.y;

        List<PortalSegment> rawSegments = new List<PortalSegment>();
        Vector3Int[] directions = { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };

        foreach (var cell in room.occupiedCells)
        {
            Vector3 cellCenter = new Vector3(cell.x * nodeSize, rayHeight, cell.z * nodeSize);

            foreach (var dir in directions)
            {
                if (!room.occupiedCells.Contains(cell + dir))
                {
                    ScanEdgeLinear(cellCenter, dir, nodeSize, rayHeight, stepSize, rayOffset, rayLength, wallLayer, rawSegments);
                }
            }
        }

        MergeSegmentsAndCreatePortals(room, rawSegments);
    }

    private static void ScanEdgeLinear(Vector3 cellCenter, Vector3Int dir, float nodeSize, float yPos, float step, float offset, float length, LayerMask layer, List<PortalSegment> segments)
    {
        Vector3 dirVec = new Vector3(dir.x, 0, dir.z);
        Vector3 rightVec = Vector3.Cross(Vector3.up, dirVec);

        Vector3 edgeCenter = cellCenter + (dirVec * (nodeSize * 0.5f));
        Vector3 scanLineCenter = edgeCenter + (dirVec * offset);

        Vector3 startPoint = scanLineCenter - (rightVec * (nodeSize * 0.5f));
        Vector3 endPoint = scanLineCenter + (rightVec * (nodeSize * 0.5f));

        bool inGap = false;
        Vector3 gapStart = Vector3.zero;

        int steps = Mathf.CeilToInt(nodeSize / step);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, t);

            bool hitWall = Physics.Raycast(currentPos, dirVec, length, layer);

            if (!hitWall)
            {
                if (!inGap) { inGap = true; gapStart = currentPos; }
            }
            else
            {
                if (inGap)
                {
                    inGap = false;
                    segments.Add(new PortalSegment { start = gapStart, end = currentPos, direction = dir });
                }
            }
        }

        if (inGap)
        {
            segments.Add(new PortalSegment { start = gapStart, end = endPoint, direction = dir });
        }
    }

    private static void MergeSegmentsAndCreatePortals(RoomManager.RoomData room, List<PortalSegment> segments)
    {
        if (segments.Count == 0) return;

        var groupedByDir = segments.GroupBy(s => s.direction);

        foreach (var group in groupedByDir)
        {
            Vector3Int facingDir = group.Key;
            List<PortalSegment> dirSegments = group.ToList();

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

                        float dist1 = Vector3.Distance(s1.end, s2.start);
                        float dist2 = Vector3.Distance(s2.end, s1.start);
                        float threshold = RoomManager.Instance.scanStepSize * 1.5f;

                        if (dist1 < threshold)
                        {
                            s1.end = s2.end; dirSegments.RemoveAt(j); changed = true; break;
                        }
                        else if (dist2 < threshold)
                        {
                            s1.start = s2.start; dirSegments.RemoveAt(j); changed = true; break;
                        }
                    }
                    if (changed) break;
                }
            }

            foreach (var seg in dirSegments)
            {
                CreateFinalPortal(room, seg);
            }
        }
    }

    private static void CreateFinalPortal(RoomManager.RoomData room, PortalSegment seg)
    {
        float width = Vector3.Distance(seg.start, seg.end);
        if (width < 0.3f) return;

        // 1. Ham Merkez (Scan Line Üzerinde)
        Vector3 rawCenter = (seg.start + seg.end) / 2.0f;

        Vector3 dirVec = new Vector3(seg.direction.x, 0, seg.direction.z);

        // 2. KONUM DÜZELTMESİ (OFFSET FIX)
        // Tarama yaparken 'portalRayOffset' kadar içeri girmiştik (örn: -0.1f).
        // Şimdi o offset'i geri alarak portalı duvar yüzeyine yapıştırıyoruz.
        // Formül: RawCenter - (Yön * Offset)
        float usedOffset = RoomManager.Instance.portalRayOffset;
        Vector3 correctedCenter = rawCenter - (dirVec * usedOffset);

        // Yükseklik Ayarı
        float floorY = room.bounds.min.y;
        Vector3 rayOrigin = new Vector3(correctedCenter.x, room.centerPoint.y + 1.0f, correctedCenter.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f)) floorY = hit.point.y;

        correctedCenter.y = floorY + 1.0f;

        RoomManager.PortalData p = new RoomManager.PortalData();
        p.position = correctedCenter; // Düzeltilmiş konum
        p.rotation = Quaternion.LookRotation(dirVec);
        p.size = new Vector3(width, 2.0f, 0.5f);

        if (RoomManager.Instance != null)
        {
            UpdatePortalHitbox(p, RoomManager.Instance.hitboxDepth, RoomManager.Instance.hitboxInnerPadding, RoomManager.Instance.hitboxHeight, RoomManager.Instance.hitboxWidthPadding);
        }
        else UpdatePortalHitbox(p, 4.0f, 0.5f, 4.0f, 0.2f);

        room.portals.Add(p);
    }

    public static void UpdatePortalHitbox(RoomManager.PortalData portal, float depth, float innerPadding, float height, float widthPadding)
    {
        float totalDepth = innerPadding + depth;
        float localZShift = (totalDepth / 2.0f) - innerPadding;

        Vector3 localCenter = new Vector3(0, 0, localZShift);
        Vector3 localSize = new Vector3(portal.size.x + widthPadding, height, totalDepth);

        portal.triggerZone = new Bounds(localCenter, localSize);
    }
}