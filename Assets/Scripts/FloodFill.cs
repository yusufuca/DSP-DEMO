using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodFill : MonoBehaviour
{
    [Header("Settings")]
    public bool debugDraw = false;
    public bool IsScanning { get; private set; } = false;

    private LayerMask floorLayer;
    private DetectingWall detect;
    private Collider[] resultsBuffer = new Collider[10];

    private void Start()
    {
        detect = DetectingWall.DetectInstance;
        // ... (Layer bulma kodları aynı, dokunma) ...
        if (detect != null)
        {
            if (detect.FloorLayer.value != 0) floorLayer = detect.FloorLayer;
            else
            {
                int layerIndex = LayerMask.NameToLayer("Floor");
                if (layerIndex != -1) floorLayer = 1 << layerIndex;
                else floorLayer = 1;
            }
        }
    }

    public void GetOrCalculateRoom(System.Action<RoomManager.RoomData> onComplete)
    {
        if (IsScanning) return;
        // Grid Kontrolü
        if (RoomManager.Instance != null && RoomManager.Instance.TryGetRoomAt(transform.position, out RoomManager.RoomData existingRoom))
        {
            if (debugDraw) Debug.Log($"[FloodFill] Cache'den geldi (Grid): {existingRoom.roomID}");
            onComplete?.Invoke(existingRoom);
            return;
        }
        ScanRoutine(transform.position, onComplete);
    }

    public void ScanRoutine(Vector3 startPos, System.Action<RoomManager.RoomData> onComplete)
    {
        IsScanning = true;
        if (detect == null) detect = DetectingWall.DetectInstance;

        float nodeSize = detect.nodeSize;

        // --- ADIM 1: ZEMİN KONTROLÜ ---
        float scanY = startPos.y;
        Vector3 rayOrigin = startPos + Vector3.up * 1.0f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit startHit, 10f, floorLayer))
        {
            scanY = startHit.point.y + (nodeSize * 0.5f);
        }
        else
        {
            // Zemin yoksa iptal etme, belki havadayızdır ama log ver
            // Debug.LogWarning("Zemin bulunamadı...");
        }

        Vector3 adjustedStart = new Vector3(startPos.x, scanY, startPos.z);
        Vector3 rawStart = SnapToGrid(adjustedStart, nodeSize);

        Queue<Vector3> wallQueue = new Queue<Vector3>();
        HashSet<Vector3> visited = new HashSet<Vector3>();
        HashSet<Vector3Int> occupiedGridCells = new HashSet<Vector3Int>(); // Odanın ham hali

        float accumulatedHardness = 0f;
        float accumulatedJagness = 0f;
        int totalWallsTouched = 0;
        float calculatedTotalVolume = 0f;

        // Bounds hesabı için (Sadece Portal Scanner için lazım olacak)
        Vector3 minPoint = rawStart;
        Vector3 maxPoint = rawStart;

        wallQueue.Enqueue(rawStart);
        visited.Add(rawStart);

        int safetyCounter = 0;

        while (wallQueue.Count > 0 && safetyCounter < 10000)
        {
            Vector3 currentPos = wallQueue.Dequeue();
            safetyCounter++;

            // --- HACİM VE HÜCRE KAYDI ---
            // ... (Hacim hesaplama kodları aynı kalsın) ...
            float ceilingDist = 0f; float floorDist = 0f;
            if (Physics.Raycast(currentPos, Vector3.up, out RaycastHit hitUp, 20f, detect.WallLayer)) ceilingDist = hitUp.distance;
            if (Physics.Raycast(currentPos, Vector3.down, out RaycastHit hitDown, 20f, floorLayer)) floorDist = hitDown.distance;

            float totalHeight = ceilingDist + floorDist;
            if (totalHeight < 0.1f) totalHeight = nodeSize;
            calculatedTotalVolume += (nodeSize * nodeSize) * totalHeight;

            // Bounds genişletme
            Vector3 pointTop = currentPos + Vector3.up * ceilingDist;
            Vector3 pointBottom = currentPos + Vector3.down * floorDist;
            minPoint = Vector3.Min(minPoint, pointBottom);
            maxPoint = Vector3.Max(maxPoint, pointTop);

            // 1. HÜCREYİ EKLE (Grid)
            if (RoomManager.Instance != null)
            {
                Vector3Int gridPos = RoomManager.Instance.WorldToGrid(currentPos);
                occupiedGridCells.Add(gridPos);
            }

            if (debugDraw) DrawDebugBox(currentPos, Vector3.one * (nodeSize * 0.9f), Quaternion.identity, Color.yellow, 10f);

            // --- KOMŞU TARAMA ---
            Vector3[] neighbors = new Vector3[]
            {
                currentPos + Vector3.forward * nodeSize,
                currentPos + Vector3.back * nodeSize,
                currentPos + Vector3.right * nodeSize,
                currentPos + Vector3.left * nodeSize
            };

            foreach (var target in neighbors)
            {
                if (visited.Contains(target)) continue;

                // A) DUVAR MI? (Wall Layer)
                if (IsWall(target))
                {
                    if (!visited.Contains(target))
                    {
                        visited.Add(target);

                        // --- KRİTİK: DUVARI DA ODAYA DAHİL ET ---
                        // Duvarın kendisini de odanın bir parçası sayıyoruz.
                        if (RoomManager.Instance != null)
                        {
                            Vector3Int wallGridPos = RoomManager.Instance.WorldToGrid(target);
                            occupiedGridCells.Add(wallGridPos);
                        }
                        // ----------------------------------------

                        if (GetMaterialData(target, out float h, out float j, nodeSize, 10f))
                        {
                            accumulatedHardness += h;
                            accumulatedJagness += j;
                            totalWallsTouched++;
                        }

                        if (debugDraw) DrawDebugBox(SnapToGrid(target, nodeSize), Vector3.one * (nodeSize * 0.9f), Quaternion.identity, Color.red, 10f);
                    }
                    continue; // Duvarın içine girme, sadece kaydet
                }

                // B) ZEMİN VAR MI? (Floor Layer)
                bool hasFloor = Physics.Raycast(target + Vector3.up, Vector3.down, 5.0f, floorLayer);
                if (hasFloor)
                {
                    visited.Add(target);
                    wallQueue.Enqueue(target);
                }
                else
                {
                    visited.Add(target); // Boşluk
                }
            }
        }

        // --- ADIM 2: HÜCRELERİ GENİŞLET (DILATION) ---
        // Odanın şeklini bozmadan (L ise L kalarak) kenarlardan şişiriyoruz.
        int expansionSteps = RoomManager.Instance != null ? RoomManager.Instance.roomExpansionSteps : 1;

        if (expansionSteps > 0)
        {
            occupiedGridCells = ExpandRoomCells(occupiedGridCells, expansionSteps);
        }
        // ---------------------------------------------

        // --- ODA OLUŞTURMA ---
        RoomManager.RoomData newRoom = new RoomManager.RoomData();
        newRoom.bounds = new Bounds();
        newRoom.bounds.SetMinMax(minPoint, maxPoint);
        newRoom.centerPoint = newRoom.bounds.center;

        if (RoomManager.Instance != null)
        {
            Vector3Int g = RoomManager.Instance.WorldToGrid(rawStart);
            newRoom.roomID = $"Room_{g.x}_{g.y}_{g.z}";
        }

        newRoom.volume = calculatedTotalVolume;
        newRoom.occupiedCells = occupiedGridCells; // Genişletilmiş hücreleri kaydet

        if (totalWallsTouched > 0)
        {
            newRoom.hardness = accumulatedHardness / totalWallsTouched;
            newRoom.jagness = accumulatedJagness / totalWallsTouched;
        }

        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.RegisterRoom(newRoom);
        }

        onComplete?.Invoke(newRoom);
        IsScanning = false;
    }

    // --- YENİ FONKSİYON: HÜCRE GENİŞLETME ---
    HashSet<Vector3Int> ExpandRoomCells(HashSet<Vector3Int> originalCells, int steps)
    {
        HashSet<Vector3Int> expandedCells = new HashSet<Vector3Int>(originalCells);
        HashSet<Vector3Int> currentBoundary = new HashSet<Vector3Int>(originalCells);

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right
        };

        for (int i = 0; i < steps; i++)
        {
            HashSet<Vector3Int> nextBoundary = new HashSet<Vector3Int>();

            foreach (var cell in currentBoundary)
            {
                foreach (var dir in directions)
                {
                    Vector3Int neighbor = cell + dir;
                    if (!expandedCells.Contains(neighbor))
                    {
                        expandedCells.Add(neighbor);
                        nextBoundary.Add(neighbor);
                    }
                }
            }
            currentBoundary = nextBoundary;
        }
        return expandedCells;
    }

    // ... (SnapToGrid, IsWall, GetMaterialData, DrawDebugBox AYNI KALSIN) ...
    // Aşağıdakileri önceki kodundan kopyala/yapıştır yapabilirsin.

    Vector3 SnapToGrid(Vector3 pos, float size)
    {
        float x = Mathf.Round(pos.x / size) * size;
        float y = pos.y;
        float z = Mathf.Round(pos.z / size) * size;
        return new Vector3(x, y, z);
    }

    bool IsWall(Vector3 position)
    {
        float nodeSize = detect.nodeSize;
        float halfSizeRef = (nodeSize / 2f) * 0.85f;
        return Physics.CheckBox(position, new Vector3(halfSizeRef, halfSizeRef, halfSizeRef), Quaternion.identity, detect.WallLayer);
    }

    bool GetMaterialData(Vector3 wallPosition, out float hardness, out float jagness, float nodeSize, float debugDuration)
    {
        hardness = 0f; jagness = 0f;
        float halfSize = (detect.nodeSize / 2) * 0.9f;
        int count = Physics.OverlapBoxNonAlloc(wallPosition, new Vector3(halfSize, halfSize, halfSize), resultsBuffer, Quaternion.identity, detect.WallLayer);
        float totalH = 0f; float totalJ = 0f; int valid = 0;
        for (int i = 0; i < count; i++)
        {
            if (resultsBuffer[i] == null) continue;
            string tag = resultsBuffer[i].tag;
            if (detect.GetMaterialInfo(tag, out MaterialDatabase.MaterialData data))
            {
                totalH += data.hardness; totalJ += data.jagness; valid++;
            }
        }
        if (valid > 0) { hardness = totalH / valid; jagness = totalJ / valid; return true; }
        return false;
    }

    void DrawDebugBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration = 0.1f)
    {
        // (Eski kodundaki DrawDebugBox aynen buraya)
        Vector3 half = size * 0.5f;
        Vector3[] points = new Vector3[8];
        points[0] = center + rotation * new Vector3(-half.x, -half.y, -half.z);
        points[1] = center + rotation * new Vector3(half.x, -half.y, -half.z);
        points[2] = center + rotation * new Vector3(half.x, -half.y, half.z);
        points[3] = center + rotation * new Vector3(-half.x, -half.y, half.z);
        points[4] = center + rotation * new Vector3(-half.x, half.y, -half.z);
        points[5] = center + rotation * new Vector3(half.x, half.y, -half.z);
        points[6] = center + rotation * new Vector3(half.x, half.y, half.z);
        points[7] = center + rotation * new Vector3(-half.x, half.y, half.z);
        Debug.DrawLine(points[0], points[1], color, duration);
        Debug.DrawLine(points[1], points[2], color, duration);
        Debug.DrawLine(points[2], points[3], color, duration);
        Debug.DrawLine(points[3], points[0], color, duration);
        Debug.DrawLine(points[4], points[5], color, duration);
        Debug.DrawLine(points[5], points[6], color, duration);
        Debug.DrawLine(points[6], points[7], color, duration);
        Debug.DrawLine(points[7], points[4], color, duration);
        Debug.DrawLine(points[0], points[4], color, duration);
        Debug.DrawLine(points[1], points[5], color, duration);
        Debug.DrawLine(points[2], points[6], color, duration);
        Debug.DrawLine(points[3], points[7], color, duration);
    }
}