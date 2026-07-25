using UnityEngine;
using FMODUnity;

public class SpeakerController : AudioSourceBase
{
    [Header("Pan Settings")]
    public float minPanStrength = 0.5f;
    public float maxPanStrength = 1.0f;

    [Header("Portal Settings")]
    [Range(0.0f, 1.0f)] public float portalViewThreshold = 0.2f;


    [HideInInspector] public string volWinner = "None";
    [HideInInspector] public string freqWinner = "None";

    public RoomManager.RoomData myRoom;

    private float targetAuxLevel = 0f;

    // Debug
    private bool debugIsInHitbox = false;
    private Vector3 debugBestPortalPos;

    protected override void Start()
    {
        base.Start(); // Base Start çalışsın

        if (currentProfile != null && currentProfile.isStatic)
            StartCoroutine(InitRoom());
    }

    // --- BURASI ÇOK ÖNEMLİ: String ile Aux Güncelleme ---
    protected override void UpdateFMODParameters()
    {
        // 1. Önce Base çalışsın (Vol, Pan, EQ gitsin)
        base.UpdateFMODParameters();

        // 2. Sonra Aux Send'i İsimle Yolla
        if (emitter.EventInstance.isValid())
        {
            emitter.EventInstance.setParameterByName("Aux1Send", targetAuxLevel);
        }
    }

    System.Collections.IEnumerator InitRoom()
    {
        yield return new WaitForSeconds(0.5f);
        OnRoomEnter();
    }

    public override void OnRoomEnter()
    {
        FloodFill scanner = GetComponent<FloodFill>();
        if (scanner != null)
        {
            scanner.GetOrCalculateRoom((room) => myRoom = room);
        }
    }

    protected override void CalculatePhysics()
    {
        if (currentProfile == null) return;

        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;
        float directDist = Vector3.Distance(sourcePos, playerPos);

        debugIsInHitbox = false;

        // 1. PORTAL HITBOX KONTROLÜ
        bool isInHitbox = false;

        if (myRoom != null && myRoom.portals.Count > 0)
        {
            foreach (var portal in myRoom.portals)
            {
                // Local Space Kontrolü
                Vector3 relativePos = playerPos - portal.position;
                Vector3 localPlayerPos = Quaternion.Inverse(portal.rotation) * relativePos;

                if (portal.triggerZone.Contains(localPlayerPos))
                {
                    isInHitbox = true;
                    debugIsInHitbox = true;
                    debugBestPortalPos = portal.position;
                    break;
                }
            }
        }

        // 2. AUX HESABI
        int assignedAux = 0;
        if (ReverbManager.RevInstance != null)
            assignedAux = ReverbManager.RevInstance.GetAuxIndexForRoom(myRoom);

        // KURAL: Hitbox içindeysek Aux 1 kesinlikle 1.0 olsun.
        if (isInHitbox)
        {
            targetAuxLevel = 1.0f;
        }
        else if (assignedAux > 0)
        {
            targetAuxLevel = 1.0f;
        }
        else
        {
            targetAuxLevel = 0f;
        }

        // 3. FİZİK HESABI (Pan, Vol, Freq Targets)
        if (isDirectConnection)
        {
            frequencyWinner = "DIRECT";
            Debug.DrawLine(sourcePos, playerPos, Color.green);
            ApplyAudioTargets(sourcePos, playerPos, directDist, isWall: false, forceOpenFreq: false);
        }
        else
        {
            frequencyWinner = "WALL";
            Debug.DrawLine(sourcePos, playerPos, Color.red);
            // Hitbox içindeysek duvar arkasında olsak bile net duyulsun (forceOpenFreq = true)
            ApplyAudioTargets(sourcePos, playerPos, directDist, isWall: true, forceOpenFreq: isInHitbox);
        }
    }

    // --- GÜNCELLENMİŞ OPTİMİZASYON ---
    void ApplyAudioTargets(Vector3 origin, Vector3 target, float distance, bool isWall, bool forceOpenFreq)
    {
        // 1. MESAFE FAKTÖRÜ (0.0 = Uzak, 1.0 = Yakın)
        // MaxHearingDistance üzerinden normalize ediyoruz.
        float distFactor = Mathf.Clamp01(1f - (distance / currentProfile.maxHearingDistance));
        distFactor = Mathf.Max(distFactor, 0.0f);

        // ---------------------------------------------------------------------
        // 2. FREKANS HESABI (HIGH CUT + REAR CONE)
        // ---------------------------------------------------------------------

        // A) Hava Emilimi (Air Absorption - Mesafe)
        float airFreq = Mathf.Lerp(currentProfile.openFreq * 0.4f, currentProfile.openFreq, distFactor);

        // B) Rear Cone (Arkadan gelen ses boğulur)
        // Oyuncunun baktığı yön (Forward) ile ses kaynağına olan yön arasındaki açı.
        // Dot: 1.0 (Ön), -1.0 (Arka)
        Vector3 dirToSource = (origin - playerTransform.position).normalized;
        float dotLook = Vector3.Dot(playerTransform.forward, dirToSource);

        // Arkadaysa (dot < 0), frekansı düşür.
        // Dot 1 ise (Ön) çarpan 1.0, Dot -1 ise (Arka) çarpan 0.6 (10k Hz civarı)
        float rearConeFactor = Mathf.Lerp(0.5f, 1.0f, (dotLook + 1.0f) * 0.5f);
        float coneFreq = currentProfile.openFreq * rearConeFactor;

        // C) Duvar (Wall)
        float wallFreq = currentProfile.openFreq;
        if (isWall && !forceOpenFreq)
        {
            float hardness = (myRoom != null) ? myRoom.hardness : 0.5f;
            // Duvar geçirgenliği: Sert duvar sesi boğar. 0.3 (Sert) - 0.95 (Yumuşak)
            float wallTransmission = Mathf.Lerp(0.3f, 0.95f, hardness);
            wallFreq = Mathf.Lerp(currentProfile.closedFreq, airFreq, wallTransmission);
        }
        else
        {
            wallFreq = airFreq;
        }

        // KAZANANI BELİRLE (En düşük frekans her zaman kazanır - Low Pass Mantığı)
        // Hava mı daha çok boğuyor, Arka dönmek mi, Duvar mı?
        float finalFreq = Mathf.Min(wallFreq, coneFreq);

        // Debug için kimin kazandığını yaz
        if (finalFreq == coneFreq && coneFreq < wallFreq) freqWinner = $"REAR ({finalFreq:0})";
        else if (isWall && !forceOpenFreq) freqWinner = $"WALL ({finalFreq:0})";
        else freqWinner = $"AIR ({finalFreq:0})";

        targetFreq = finalFreq;


        // ---------------------------------------------------------------------
        // 3. VOLUME HESABI (Mesafe + Duvar - AZ DÜŞÜŞ)
        // ---------------------------------------------------------------------

        // Duvar Cezası: "Çok düşmesin" dediğin için aralığı yükselttim.
        // En kötü durumda bile (Beton duvar) sesin %70'i geçecek (0.7f).
        // İnce duvarda %95'i geçecek (0.95f).
        float wallVolPenalty = 1.0f;
        if (isWall && !forceOpenFreq)
        {
            float hardness = (myRoom != null) ? myRoom.hardness : 0.5f;
            wallVolPenalty = Mathf.Lerp(0.7f, 0.95f, hardness);
        }

        // Mesafe düşüşü + Duvar düşüşü
        float finalTrans = distFactor * wallVolPenalty;
        targetVol = Mathf.Lerp(currentProfile.closedVol, currentProfile.openVol, finalTrans);

        // Debug için kimin sesi kıstığını yaz
        if (distFactor < wallVolPenalty) volWinner = "DIST";
        else volWinner = "WALL";


        // ---------------------------------------------------------------------
        // 4. PAN HESABI
        // ---------------------------------------------------------------------
        Vector3 dir = (origin - playerTransform.position).normalized;
        float rawPan = Vector3.Dot(playerTransform.right, dir);
        targetPan = rawPan * Mathf.Lerp(minPanStrength, maxPanStrength, distFactor);
    }

    void OnDrawGizmos()
    {
        if (myRoom != null && myRoom.portals != null)
        {
            foreach (var portal in myRoom.portals)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(portal.position, portal.rotation, Vector3.one);

                // Mavi: Aktif, Gri: Pasif
                bool isThisActive = debugIsInHitbox && Vector3.Distance(portal.position, debugBestPortalPos) < 0.1f;

                Gizmos.color = isThisActive ? new Color(0, 1, 1, 0.8f) : new Color(0, 0, 1, 0.2f);
                Gizmos.DrawCube(portal.triggerZone.center, portal.triggerZone.size);

                Gizmos.color = isThisActive ? Color.white : Color.red;
                Gizmos.DrawWireCube(portal.triggerZone.center, portal.triggerZone.size);

                Gizmos.matrix = oldMatrix;
            }
        }
    }
}