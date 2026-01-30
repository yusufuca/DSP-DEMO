using UnityEngine;
using FMODUnity;

public class SpeakerController : AudioSourceBase
{
    [Header("Pan Settings")]
    public float minPanStrength = 0.5f;
    public float maxPanStrength = 1.0f;

    [Header("Portal Settings")]
    [Range(0.0f, 1.0f)] public float portalViewThreshold = 0.2f;

    public RoomManager.RoomData myRoom;

    private FMOD.Studio.PARAMETER_ID aux1SendID;

    protected override void Start()
    {
        base.Start();
        if (currentProfile != null && currentProfile.isStatic)
            StartCoroutine(InitRoom());

        if (emitter.EventDescription.isValid())
        {
            FMOD.Studio.PARAMETER_DESCRIPTION desc;
            FMOD.RESULT res1 = emitter.EventDescription.getParameterDescriptionByName("Aux1Send", out desc);
            if (res1 == FMOD.RESULT.OK) aux1SendID = desc.id;
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
        float directDist = Vector3.Distance(sourcePos, playerPos);

        // 1. Direkt Hat (Temiz)
        if (isDirectConnection)
        {
            frequencyWinner = "DIRECT";
            ApplyAudio(sourcePos, playerPos, directDist, false, false, 0);
            return;
        }

        // 2. Portal Kontrolü
        if (myRoom != null && myRoom.portals.Count > 0)
        {
            RoomManager.PortalData bestPortal = null;
            float bestScore = float.MaxValue;
            bool isInHitbox = false;

            foreach (var portal in myRoom.portals)
            {
                Vector3 portalForward = portal.rotation * Vector3.forward;
                Vector3 dirToPlayer = (playerPos - portal.position).normalized;
                float dot = Vector3.Dot(portalForward, dirToPlayer);

                if (dot < portalViewThreshold) continue;

                float distToPlayer = Vector3.Distance(portal.position, playerPos);
                float distToSource = Vector3.Distance(sourcePos, portal.position);
                float totalDist = distToPlayer + distToSource;

                if (totalDist < bestScore)
                {
                    bestScore = totalDist;
                    bestPortal = portal;
                }
                if (portal.triggerZone.Contains(playerPos)) isInHitbox = true;
            }

            if (bestPortal != null)
            {
                isPortalConnection = true;
                portalFound = true;
                portalPosition = bestPortal.position;

                Debug.DrawLine(sourcePos, bestPortal.position, Color.cyan);
                Debug.DrawLine(bestPortal.position, playerPos, Color.cyan);

                frequencyWinner = "PORTAL";

                int assignedAux = 0;
                if (ReverbManager.RevInstance != null)
                    assignedAux = ReverbManager.RevInstance.GetAuxIndexForRoom(myRoom);

                // Portal aktifse 1.0 (Full Send)
                float spillAmount = isInHitbox ? 1.0f : 0.0f;

                if (assignedAux > 0) spillAmount = 1.0f;

                ApplyAudio(bestPortal.position, playerPos, bestScore, true, false, assignedAux, spillAmount);
                return;
            }
        }

        // 3. Duvar Arkası
        isPortalConnection = false;
        portalFound = false;
        frequencyWinner = "WALL";
        Debug.DrawLine(sourcePos, playerPos, Color.red);
        ApplyAudio(sourcePos, playerPos, directDist, false, true, 0);
    }

    void ApplyAudio(Vector3 origin, Vector3 target, float distance, bool isPortal, bool isWall, int auxIndex, float sendLevel = 0f)
    {
        float distFactor = Mathf.Clamp01(1f - (distance / currentProfile.maxHearingDistance));
        distFactor = Mathf.Max(distFactor, 0.0f);

        float wallPenalty = isWall ? 0.3f : 1.0f;
        float finalTrans = distFactor * wallPenalty;

        targetVol = Mathf.Lerp(currentProfile.closedVol, currentProfile.openVol, finalTrans);

        // --- FREKANS DÜZELTMESİ (HIGH CUT FIX) ---
        // Portal veya Direkt olsa bile mesafeye göre boğulma eklenir.
        if (isPortal)
        {
            targetFreq = Mathf.Lerp(currentProfile.closedFreq, currentProfile.openFreq, distFactor);
        }
        else if (isWall)
        {
            targetFreq = currentProfile.closedFreq;
        }
        else
        {
            targetFreq = Mathf.Lerp(currentProfile.closedFreq, currentProfile.openFreq, distFactor);
        }

        Vector3 dir = (origin - playerTransform.position).normalized;
        float rawPan = Vector3.Dot(playerTransform.right, dir);
        targetPan = rawPan * Mathf.Lerp(minPanStrength, maxPanStrength, distFactor);

        if (emitter.EventInstance.isValid())
        {
            // Sadece Aux1'i yönetiyoruz
            emitter.EventInstance.setParameterByName("Aux1Send", 0f);

            if (auxIndex == 1)
                emitter.EventInstance.setParameterByName("Aux1Send", sendLevel);
        }
    }

    void OnDrawGizmos()
    {
        if (myRoom != null && myRoom.portals != null)
        {
            foreach (var portal in myRoom.portals)
            {
                // Hitbox'ı çiz
                Gizmos.color = Color.blue; 
                Gizmos.DrawCube(portal.triggerZone.center, portal.triggerZone.size);

                // Çerçevesi
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(portal.triggerZone.center, portal.triggerZone.size);
            }
        }
    }
}