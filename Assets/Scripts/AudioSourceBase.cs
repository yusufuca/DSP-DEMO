using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

[RequireComponent(typeof(StudioEventEmitter))]
public abstract class AudioSourceBase : MonoBehaviour
{
    [Header("Config")]
    public AudioSourceLibrary audioLibrary;

    [Header("Room Awareness")]
    public RoomManager.RoomData SourceRoom;

    [Header("Portal Scanning")]
    public bool usePortalScanning = true;
    [Range(45f, 180f)]
    public float scanArcAngle = 120f;
    [Range(10, 60)]
    public int scanResolution = 40;

    // --- HATALI SİLİNEN DEĞİŞKENLER GERİ EKLENDİ (Public) ---
    [Header("Runtime Debug Data")]
    public string matchedTag = "None";
    public bool isIndoors = false;
    public float currentDistance;
    public bool isObstructed;
    public string frequencyWinner;

    public bool portalFound = false;
    public Vector3 portalPosition;
    public RoomManager.RoomData roomThroughPortal;

    // DEBUG DEĞİŞKENLERİ (Gizmos İçin)
    private Vector3 _debugCandidatePos;
    private bool _debugLinecastHit;
    private bool _debugWasGapFound;

    protected AudioSourceLibrary.AudioProfile currentProfile;
    protected StudioEventEmitter emitter;
    protected Transform playerTransform;
    protected DetectingWall detect;

    protected PARAMETER_ID occVolID, occEQID, panID;

    protected float currentFreq = 22000f;
    protected float currentVol = 0f;
    protected float currentPan = 0f;
    protected float targetFreq = 22000f;
    protected float targetVol = 0f;
    protected float targetPan = 0f;

    private float ceilingCheckTimer = 0f;
    public bool isScannerLocked = false;

    protected virtual void Awake()
    {
        if (emitter == null) emitter = GetComponent<StudioEventEmitter>();
        detect = DetectingWall.DetectInstance;
        if (Camera.main != null) playerTransform = Camera.main.transform;

        if (audioLibrary != null)
        {
            string myTag = gameObject.tag;
            foreach (var profile in audioLibrary.allSources)
            {
                if (profile.tagID == myTag)
                {
                    currentProfile = profile;
                    matchedTag = myTag;
                    break;
                }
            }
        }
        GetFMODParameters();
    }

    protected virtual void Start()
    {
        if (currentProfile == null) return;
        CheckCeiling();
        if (currentProfile.isStatic) OnRoomEnter();
    }

    protected virtual void Update()
    {
        if (currentProfile == null || emitter == null || !emitter.IsPlaying()) return;
        if (playerTransform == null)
        {
            if (Camera.main != null) playerTransform = Camera.main.transform;
            else return;
        }

        ceilingCheckTimer += Time.deltaTime;
        if (ceilingCheckTimer >= 0.5f)
        {
            CheckCeiling();
            ceilingCheckTimer = 0f;
        }

        CalculatePhysics();
        UpdateFMODParameters();
    }

    protected void ScanForPortal()
    {
        if (playerTransform == null || currentProfile == null) return;

        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;

        // 1. DÜZLEŞTİRME (Flatten)
        Vector3 dirToPlayerFlat = (new Vector3(playerPos.x, sourcePos.y, playerPos.z) - sourcePos).normalized;
        float distToPlayer = Vector3.Distance(sourcePos, playerPos);

        // Menzili oyuncu mesafesinin biraz ötesine kadar tara
        float scanReach = Mathf.Min(distToPlayer * 2.5f, currentProfile.maxHearingDistance);

        // Kilit mekanizmasını iptal ettik (Sürekli güncelleme için)

        float stepAngle = scanArcAngle / scanResolution;
        float startAngle = -scanArcAngle / 2f;

        List<Vector3> gapDirections = new List<Vector3>();
        // Duvar verilerini tutuyoruz: <Yön, Mesafe>
        List<KeyValuePair<Vector3, float>> wallHits = new List<KeyValuePair<Vector3, float>>();

        for (int i = 0; i <= scanResolution; i++)
        {
            float currentAngle = startAngle + (stepAngle * i);
            Vector3 scanDir = Quaternion.Euler(0, currentAngle, 0) * dirToPlayerFlat;

            RaycastHit hit;
            // Raycast başlangıcını çok az ileri alıyoruz (Self-collision önlemi)
            bool hitSomething = Physics.Raycast(sourcePos + (scanDir * 0.1f), scanDir, out hit, scanReach, currentProfile.obstructionLayer);

            // Boşluk Kriteri: Çarpmadıysa VEYA Çarptığı yer oyuncudan daha uzaksa
            bool isGap = !hitSomething || (hitSomething && hit.distance > distToPlayer);

            if (isGap)
            {
                gapDirections.Add(scanDir);
            }
            else
            {
                // Duvarı kaydet
                wallHits.Add(new KeyValuePair<Vector3, float>(scanDir, hit.distance));
            }
        }

        // --- SONUÇ HESAPLAMA ---
        if (gapDirections.Count > 0)
        {
            _debugWasGapFound = true;

            // 1. Ortalama Boşluk Yönü (Kapının Göbeği)
            Vector3 averageDir = Vector3.zero;
            foreach (var dir in gapDirections) averageDir += dir;
            averageDir = averageDir.normalized;

            // 2. PERVAZ TESPİTİ (Dot Product Yöntemi)
            // Sorun şuydu: En yakın duvarı alıyorduk, o da hoparlörün yanındaki duvardı.
            // Çözüm: "Ortalama Boşluk Yönüne" en paralel olan (en yakın açılı) duvarı bulacağız.

            float bestWallDist = distToPlayer; // Default
            float maxDot = -1f;
            bool foundFrame = false;

            foreach (var wall in wallHits)
            {
                // Duvar yönü ile Kapı yönü ne kadar benziyor?
                float dot = Vector3.Dot(wall.Key, averageDir);

                // En yüksek benzerliği bul (Kapının hemen yanındaki duvar)
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestWallDist = wall.Value;
                    foundFrame = true;
                }
            }

            float finalDist = distToPlayer;

            if (foundFrame)
            {
                // Pervazı bulduk! Mesafeye 0.5m ekle (İçeri it)
                finalDist = bestWallDist + 0.5f;
            }
            else
            {
                // Hiç duvar yoksa (açık alan), oyuncuya yakın koy
                finalDist = Mathf.Min(distToPlayer, 2.0f);
            }

            // Menzil sınırı
            finalDist = Mathf.Min(finalDist, currentProfile.maxHearingDistance);

            // 3. Aday Nokta
            Vector3 candidatePos = sourcePos + (averageDir * finalDist);
            candidatePos.y = (sourcePos.y + playerPos.y) / 2f; // Yükseklik ortalaması

            _debugCandidatePos = candidatePos;

            // 4. SON GÜVENLİK (Linecast)
            // Aday noktadan oyuncuya hat çek.
            if (!Physics.Linecast(candidatePos, playerPos, currentProfile.obstructionLayer))
            {
                // BAŞARILI!
                portalPosition = candidatePos;
                portalFound = true;
                isScannerLocked = true;
                _debugLinecastHit = false;

                IdentifyRoomThroughPortal();
            }
            else
            {
                // BAŞARISIZ (Duvar Arkası)
                portalFound = false;
                roomThroughPortal = null;
                _debugLinecastHit = true;
            }
        }
        else
        {
            _debugWasGapFound = false;
            portalFound = false;
            roomThroughPortal = null;
        }
    }

    // --- GIZMOS (FULL OPAK) ---
    void OnDrawGizmos()
    {
        Vector3 sourcePos = transform.position;
        if (currentProfile != null) sourcePos += currentProfile.rayOffset;

        // Sarı Çember
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sourcePos, (currentProfile != null) ? currentProfile.maxHearingDistance : 10f);

        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 dirToPlayerFlat = (new Vector3(playerPos.x, sourcePos.y, playerPos.z) - sourcePos).normalized;
        float distToPlayer = Vector3.Distance(sourcePos, playerPos);
        float range = (currentProfile != null) ? Mathf.Min(distToPlayer * 2.5f, currentProfile.maxHearingDistance) : distToPlayer;

        Vector3 leftDir = Quaternion.Euler(0, -scanArcAngle / 2f, 0) * dirToPlayerFlat;
        Vector3 rightDir = Quaternion.Euler(0, scanArcAngle / 2f, 0) * dirToPlayerFlat;

        // Mavi Tarama Çizgileri
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(sourcePos, sourcePos + leftDir * range);
        Gizmos.DrawLine(sourcePos, sourcePos + rightDir * range);

        // --- SONUÇ GÖRSELLEŞTİRME ---
        if (portalFound)
        {
            // BAŞARILI: MAVİ TOP (İçi Dolu)
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(portalPosition, 0.5f);
            Gizmos.DrawLine(sourcePos, portalPosition);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(portalPosition, playerPos);
        }
        else if (_debugWasGapFound && _debugLinecastHit)
        {
            // HATA: TURUNCU TOP (Hesaplandı ama Duvar Arkasında Kaldı)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Turuncu
            Gizmos.DrawSphere(_debugCandidatePos, 0.5f);
            Gizmos.DrawLine(sourcePos, _debugCandidatePos);

            // Sorunlu Hattı Kırmızı Çiz
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_debugCandidatePos, playerPos);
        }
    }

    void IdentifyRoomThroughPortal()
    {
        if (RoomManager.Instance == null) return;
        if (RoomManager.Instance.TryGetRoomAt(portalPosition, out RoomManager.RoomData room))
        {
            roomThroughPortal = room;
        }
        else
        {
            roomThroughPortal = null;
        }
    }

    void CheckCeiling()
    {
        // Null Check eklendi (Hata düzeltmesi)
        if (currentProfile == null) return;

        Vector3 origin = transform.position + currentProfile.rayOffset;
        isIndoors = Physics.Raycast(origin, Vector3.up, 15f, currentProfile.obstructionLayer);
    }

    void GetFMODParameters()
    {
        if (emitter.EventDescription.isValid())
        {
            emitter.EventDescription.getParameterDescriptionByName("OccEQ", out var d1); occEQID = d1.id;
            emitter.EventDescription.getParameterDescriptionByName("OccVol", out var d2); occVolID = d2.id;
            emitter.EventDescription.getParameterDescriptionByName("StereoPan", out var d3); panID = d3.id;
        }
    }

    protected virtual void UpdateFMODParameters()
    {
        if (currentProfile == null) return;
        float speed = Time.deltaTime * currentProfile.occlusionLerpSpeed;

        currentFreq = Mathf.Lerp(currentFreq, targetFreq, speed);
        currentVol = Mathf.Lerp(currentVol, targetVol, speed);
        currentPan = Mathf.Lerp(currentPan, targetPan, speed);

        if (emitter.EventInstance.isValid())
        {
            emitter.EventInstance.setParameterByID(occEQID, currentFreq);
            emitter.EventInstance.setParameterByID(occVolID, currentVol);
            emitter.EventInstance.setParameterByID(panID, currentPan);
        }
    }

    protected abstract void CalculatePhysics();
    public virtual void OnRoomEnter() { }

    public float GetTargetFreq() => targetFreq;
    public float GetCurrentFreq() => currentFreq;
    public float GetCurrentVol() => currentVol;
    public float GetCurrentPan() => currentPan;
}