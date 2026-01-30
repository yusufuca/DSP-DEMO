using UnityEngine;

public class SpeakerController : AudioSourceBase
{
    [Header("Pan Settings")]
    public float minPanStrength = 0.5f;
    public float maxPanStrength = 1.0f;

    [Header("Portal Settings")]
    [Tooltip("Portalın oyuncuyu görme açısı. 0.0 = Tam 90 derece yanlar dahil, 1.0 = Sadece tam karşıdan.")]
    [Range(0.0f, 1.0f)] public float portalViewThreshold = 0.2f; // Eşik Değeri

    // Odamız (FloodFill tarafından bulunur)
    public RoomManager.RoomData myRoom;

    // FMOD Parametre ID'si (Reverb Gönderimi için)
    private FMOD.Studio.PARAMETER_ID roomSendID;

    protected override void Start()
    {
        base.Start();
        if (currentProfile != null && currentProfile.isStatic)
            StartCoroutine(InitRoom());

        if (emitter.EventDescription.isValid())
        {
            FMOD.Studio.PARAMETER_DESCRIPTION desc;
            emitter.EventDescription.getParameterDescriptionByName("RoomSend", out desc);
            roomSendID = desc.id;
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
            scanner.GetOrCalculateRoom((room) =>
            {
                myRoom = room;
            });
        }
    }

    protected override void CalculatePhysics()
    {
        if (currentProfile == null) return;
        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;
        float directDist = Vector3.Distance(sourcePos, playerPos); // Direkt mesafe

        // -----------------------------------------------------------
        // ADIM 1: DİREKT SİNYAL (Her zaman hesaplanır - Referans Ray)
        // -----------------------------------------------------------
        // Bu, duvarın arkasındaki "Dry" sesi temsil eder.
        // Debug için her zaman Kırmızı/Yeşil çizelim ki "Asıl Kaynak" yerini unutmayalım.
        Color directColor = isDirectConnection ? Color.green : new Color(1, 0, 0, 0.3f); // Soluk Kırmızı
        Debug.DrawLine(sourcePos, playerPos, directColor);

        // SENARYO 1: Engel Yok (Aynı Odayız ve Görüş Var)
        if (isDirectConnection)
        {
            frequencyWinner = "DIRECT";
            // Portal olsa bile direkt görüş varsa direkt ses baskındır.
            ApplyAudio(sourcePos, playerPos, directDist, false, false, 0.0f);
            return;
        }

        // -----------------------------------------------------------
        // ADIM 2: PORTAL SİNYALİ (Engel Var - Kapı Arıyoruz)
        // -----------------------------------------------------------
        if (myRoom != null && myRoom.portals.Count > 0)
        {
            RoomManager.PortalData bestPortal = null;
            float bestScore = float.MaxValue;
            bool isInTriggerZone = false;

            foreach (var portal in myRoom.portals)
            {
                // 1. AÇI KONTROLÜ (Threshold)
                // Portalın dışarı bakan yüzü ile oyuncu arasındaki açı
                Vector3 portalForward = portal.rotation * Vector3.forward; // Portalın baktığı yön
                Vector3 dirToPlayer = (playerPos - portal.position).normalized;

                // Dot Product: 1 (Tam Karşı), 0 (Tam Yan), -1 (Arkası)
                float dot = Vector3.Dot(portalForward, dirToPlayer);

                // Eğer oyuncu portalın arkasında veya çok yanındaysa bu portalı kullanma!
                // threshold ne kadar yüksekse, oyuncunun o kadar portalın karşısında olması gerekir.
                if (dot < portalViewThreshold) continue;

                // 2. MESAFE KONTROLÜ
                float distToPlayer = Vector3.Distance(portal.position, playerPos);
                float distToSource = Vector3.Distance(sourcePos, portal.position);
                float totalDist = distToPlayer + distToSource;

                if (totalDist < bestScore)
                {
                    bestScore = totalDist;
                    bestPortal = portal;
                }

                if (portal.triggerZone.Contains(playerPos)) isInTriggerZone = true;
            }

            if (bestPortal != null)
            {
                // --- PORTAL BULUNDU VE GÖRÜŞ AÇISINDAYIZ ---
                isPortalConnection = true;
                portalFound = true;
                portalPosition = bestPortal.position;

                // Portal Debug (Cyan)
                Debug.DrawLine(sourcePos, bestPortal.position, Color.cyan);
                Debug.DrawLine(bestPortal.position, playerPos, Color.cyan);

                frequencyWinner = "PORTAL";

                // Reverb Spill: TriggerZone'daysak Full, uzaktaysak az.
                float reverbSpillAmount = isInTriggerZone ? 1.0f : 0.5f;

                // Sesi Portal Pozisyonundan Ver
                ApplyAudio(bestPortal.position, playerPos, bestScore, true, false, reverbSpillAmount);
                return;
            }
        }

        // -----------------------------------------------------------
        // SENARYO 3: DUVAR ARKASI (Portal Yok veya Açı Kötü)
        // -----------------------------------------------------------
        // Portal görüş açısından çıktık, artık ses direkt duvardan (boğuk) gelmeli.
        isPortalConnection = false;
        portalFound = false;
        frequencyWinner = "WALL";

        // Debug: Tam Kırmızı (Duvar Arkası)
        Debug.DrawLine(sourcePos, playerPos, Color.red);

        ApplyAudio(sourcePos, playerPos, directDist, false, true, 0.0f);
    }

    void ApplyAudio(Vector3 origin, Vector3 target, float distance, bool isPortal, bool isWall, float reverbSend)
    {
        float distFactor = Mathf.Clamp01(1f - (distance / currentProfile.maxHearingDistance));
        distFactor = Mathf.Max(distFactor, 0.0f);

        float wallPenalty = isWall ? 0.3f : 1.0f;
        float finalTrans = distFactor * wallPenalty;

        targetVol = Mathf.Lerp(currentProfile.closedVol, currentProfile.openVol, finalTrans);

        if (isPortal) targetFreq = currentProfile.openFreq;
        else if (isWall) targetFreq = currentProfile.closedFreq;
        else targetFreq = Mathf.Lerp(currentProfile.closedFreq, currentProfile.openFreq, distFactor);

        Vector3 dir = (origin - playerTransform.position).normalized;
        float rawPan = Vector3.Dot(playerTransform.right, dir);
        targetPan = rawPan * Mathf.Lerp(minPanStrength, maxPanStrength, distFactor);

        if (emitter.EventInstance.isValid())
        {
            emitter.EventInstance.setParameterByID(roomSendID, reverbSend);
        }
    }
}