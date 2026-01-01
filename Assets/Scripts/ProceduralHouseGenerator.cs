using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralHouseGenerator : MonoBehaviour
{
    [Header("Genel Ayarlar")]
    public float tileSize = 4f;
    public int roomCount = 10;

    [Header("Duvar ve Tavan Ayarları")]
    public float wallHeight = 4f;

    [Header("Assets (Sürükle Bırak)")]
    public GameObject floorPrefab;   // Zemin
    public GameObject ceilingPrefab; // Tavan

    [Header("Duvar Çeşitleri")]
    public GameObject wallSolid;     // Düz Duvar
    public GameObject wallWindow;    // Pencereli Duvar
    public GameObject wallDoor;      // Kapılı Duvar

    [Header("Boyut Ayarı")]
    [Tooltip("Eğer prefablerin 1x1 boyutundaysa (küçükse) bunu işaretle. Hazır modüllerin zaten büyükse işareti kaldır.")]
    public bool autoScale = true;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();

    private void Start()
    {
        GenerateHouse();
    }

    [ContextMenu("Yeniden Oluştur")]
    public void GenerateHouse()
    {
        ClearHouse();

        // --- 1. ADIM: HARİTA KOORDİNATLARINI BELİRLE ---
        Vector2Int currentPos = Vector2Int.zero;
        occupiedTiles.Add(currentPos);

        for (int i = 0; i < roomCount; i++)
        {
            Vector2Int direction = GetRandomDirection();
            currentPos += direction;
            occupiedTiles.Add(currentPos);
        }

        // --- 2. ADIM: PREFABLARI YERLEŞTİR ---
        foreach (Vector2Int tilePos in occupiedTiles)
        {
            // Zemin
            SpawnObject(floorPrefab, tilePos, 0, false);

            // Tavan
            SpawnObject(ceilingPrefab, tilePos, wallHeight, false);

            // Duvarlar (Sadece dışarıya bakan kenarlara)
            CheckAndSpawnWalls(tilePos);
        }
    }

    void CheckAndSpawnWalls(Vector2Int gridPos)
    {
        // KUZEY (Yukarı) - Eğer yukarıda oda YOKSA duvar koy
        if (!occupiedTiles.Contains(gridPos + Vector2Int.up))
            SpawnRandomWall(gridPos, 0, Vector3.forward);

        // GÜNEY (Aşağı)
        if (!occupiedTiles.Contains(gridPos + Vector2Int.down))
            SpawnRandomWall(gridPos, 180, Vector3.back);

        // DOĞU (Sağ)
        if (!occupiedTiles.Contains(gridPos + Vector2Int.right))
            SpawnRandomWall(gridPos, 90, Vector3.right);

        // BATI (Sol)
        if (!occupiedTiles.Contains(gridPos + Vector2Int.left))
            SpawnRandomWall(gridPos, 270, Vector3.left);
    }

    void SpawnRandomWall(Vector2Int gridPos, float yRotation, Vector3 directionOffset)
    {
        // Hangi duvarı koyalım? Rastgele seç.
        GameObject selectedWall = GetRandomWallType();

        if (selectedWall == null) return;

        GameObject wall = Instantiate(selectedWall, transform);

        // --- POZİSYON AYARI ---
        // Zeminin merkezi
        Vector3 centerPos = new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);

        // Duvarı merkeze değil, kenara itmek için offset (TileSize'ın yarısı kadar)
        float edgeOffset = tileSize / 2f;

        // Final pozisyon
        wall.transform.localPosition = centerPos + (directionOffset * edgeOffset);

        // Dönme açısı
        wall.transform.localRotation = Quaternion.Euler(0, yRotation, 0);

        // --- BOYUT AYARI ---
        if (autoScale)
        {
            // Duvarın genişliği TileSize kadar, yüksekliği WallHeight kadar olmalı
            // Not: Genelde duvar prefabları X ekseninde genişler.
            // Kalınlığı (Z) ince tutuyoruz (0.2f).
            wall.transform.localScale = new Vector3(tileSize, wallHeight, 0.2f);
        }

        spawnedObjects.Add(wall);
    }

    GameObject GetRandomWallType()
    {
        // Basit bir rastgelelik: %20 Kapı, %40 Pencere, %40 Düz Duvar
        // Bunu kafana göre değiştirebilirsin.
        int roll = Random.Range(0, 10);

        if (roll < 2) return wallDoor;      // 0, 1 gelirse Kapı
        if (roll < 6) return wallWindow;    // 2, 3, 4, 5 gelirse Pencere
        return wallSolid;                   // Geri kalanı Düz Duvar
    }

    void SpawnObject(GameObject prefab, Vector2Int gridPos, float height, bool isWall)
    {
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, transform);
        obj.transform.localPosition = new Vector3(gridPos.x * tileSize, height, gridPos.y * tileSize);

        if (autoScale)
        {
            obj.transform.localScale = new Vector3(tileSize, 0.2f, tileSize);
        }

        spawnedObjects.Add(obj);
    }

    Vector2Int GetRandomDirection()
    {
        int r = Random.Range(0, 4);
        if (r == 0) return Vector2Int.up;
        if (r == 1) return Vector2Int.down;
        if (r == 2) return Vector2Int.left;
        return Vector2Int.right;
    }

    public void ClearHouse()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
        occupiedTiles.Clear();
    }
}