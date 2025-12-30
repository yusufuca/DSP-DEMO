using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralHouseGenerator : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Zeminlerin boyutu (Örn: 4 metre)")]
    public float tileSize = 4f;

    [Tooltip("Kaç adet oda eklensin?")]
    public int roomCount = 5;

    [Header("Assets")]
    public GameObject floorPrefab; // Buraya prefabini sürükle

    [Header("Boyut Ayarı")]
    [Tooltip("Eğer prefabin 1x1 ise bunu işaretle. Eğer zaten 4x4 modellediysen işareti kaldır.")]
    public bool autoScale = true;

    // Oluşturulan objeleri tutan liste
    private List<GameObject> spawnedFloors = new List<GameObject>();

    // Grid haritamız
    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();

    private void Start()
    {
        GenerateHouse();
    }

    private void Update()
    {
        // Oyun çalışırken TileSize veya AutoScale değişirse anlık güncelle
        if (spawnedFloors.Count > 0)
        {
            // Sadece kontrol amaçlı basit bir tetikleme
            // (Performans için normalde her frame yapılmaz ama editörde görmek için bırakıyorum)
            RegenerateVisuals();
        }
    }

    [ContextMenu("Yeniden Oluştur")]
    public void GenerateHouse()
    {
        ClearHouse();

        // --- 1. ADIM: HARİTA OLUŞTURMA (MANTIK) ---
        Vector2Int currentPos = Vector2Int.zero;
        occupiedTiles.Add(currentPos);

        for (int i = 0; i < roomCount; i++)
        {
            Vector2Int direction = GetRandomDirection();
            currentPos += direction;
            occupiedTiles.Add(currentPos);

            // Odaları biraz daha dolgun göstermek için yanına da ekle (İsteğe bağlı)
            // occupiedTiles.Add(currentPos + Vector2Int.right);
        }

        // --- 2. ADIM: SAHNEYE KOYMA (GÖRSEL) ---
        foreach (Vector2Int tilePos in occupiedTiles)
        {
            CreateFloorTile(tilePos);
        }
    }

    void CreateFloorTile(Vector2Int gridPos)
    {
        if (floorPrefab == null)
        {
            Debug.LogError("Floor Prefab atanmamış!");
            return;
        }

        // Prefab'den oluştur
        GameObject obj = Instantiate(floorPrefab, transform);
        obj.name = $"Floor_{gridPos.x}_{gridPos.y}";

        // Pozisyonu ayarla
        Vector3 worldPos = new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
        obj.transform.localPosition = worldPos;

        // Boyutu ayarla (Seçeneğe göre)
        if (autoScale)
        {
            obj.transform.localScale = new Vector3(tileSize, 0.2f, tileSize);
        }

        spawnedFloors.Add(obj);
    }

    void RegenerateVisuals()
    {
        int index = 0;
        foreach (Vector2Int gridPos in occupiedTiles)
        {
            if (index < spawnedFloors.Count)
            {
                GameObject obj = spawnedFloors[index];

                // Pozisyon güncelle
                Vector3 worldPos = new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
                obj.transform.localPosition = worldPos;

                // Boyut güncelle (Seçeneğe göre)
                if (autoScale)
                {
                    // Eğer kod yönetsin dersen zorla tileSize yap
                    obj.transform.localScale = new Vector3(tileSize, 0.2f, tileSize);
                }
                // AutoScale kapalıysa prefabin kendi boyutuna dokunmuyoruz.
            }
            index++;
        }
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
        foreach (GameObject obj in spawnedFloors)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedFloors.Clear();
        occupiedTiles.Clear();
    }
}