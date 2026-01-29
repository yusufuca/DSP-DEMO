using UnityEngine;

public class SpeakerController : AudioSourceBase
{

    [Header("Pan Settings")]

    public float minPanStrength = 0.5f;
    public float maxPanStrength = 1.0f;

    protected override void Start()
    {

        base.Start();


        if (currentProfile != null && currentProfile.isStatic)
        {
            StartCoroutine(DelayedRoomScan());
        }
    }
    System.Collections.IEnumerator DelayedRoomScan()
    {
        // 0.5 saniye bekle, sistem otursun
        yield return new WaitForSeconds(0.5f);
        OnRoomEnter();
    }

    public override void OnRoomEnter()
    {
        FloodFill myScanner = GetComponent<FloodFill>();
        if (myScanner != null)
        {
            // Odayı tara ve SourceRoom'a kaydet
            myScanner.GetOrCalculateRoom((room) =>
            {
                SourceRoom = room; // <--- Kimlik Kartı İşlendi!
                Debug.Log($"[Speaker] {name} kendi odasını buldu: {room.roomID}");
            });
        }
        else
        {
            Debug.LogError("[Speaker] FloodFill bileşeni eksik!");
        }
    }
    protected override void CalculatePhysics()
    {

        bool isSameRoom = false;
        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;
        Vector3 toSource = sourcePos - playerPos;
        float dist = toSource.magnitude;
        currentDistance = dist;

      


        if (ReverbManager.RevInstance != null)
        {
            var playerRoom = ReverbManager.RevInstance.GetPlayerRoom();

            // İkimizin de odası belli mi ve aynı mı?
            if (SourceRoom != null && playerRoom != null)
            {
                isSameRoom = (SourceRoom == playerRoom); // Referans karşılaştırması
            }
            // Eğer ikimiz de "Dışarıdaysak" (null), aynı oda sayabiliriz (opsiyonel)
            else if (SourceRoom == null && playerRoom == null)
            {
                isSameRoom = true;
            }
        }

        if (isSameRoom)
        {
           
            HandleDirectPhysics(sourcePos, playerPos, dist);

           
        }
        else
        {
          

            HandleDifferentRoomPhysics(sourcePos, playerPos, dist);
        }
    }

    void HandleDirectPhysics(Vector3 sourcePos, Vector3 playerPos, float dist)
    {
        Vector3 toSource = sourcePos - playerPos;
        Vector3 dirNormalized = toSource.normalized;
        currentDistance = dist;

        float distFactor = Mathf.Clamp01(1f - (dist / currentProfile.maxHearingDistance));
        distFactor = Mathf.Max(distFactor, 0.0f);

        float wallFactor = 1f;
        RaycastHit hit;

        if (Physics.Linecast(sourcePos, playerPos, out hit, currentProfile.obstructionLayer))
        {
            isObstructed = true;
            frequencyWinner = "WALL";
            string tag = hit.collider.tag;
            float hardness = 0.5f;

            if (detect != null && detect.GetMaterialInfo(tag, out MaterialDatabase.MaterialData data))
                hardness = data.hardness;

            wallFactor = 1f - (hardness * 0.8f);
            Debug.DrawLine(sourcePos, hit.point, Color.red);
        }
        else
        {
            isObstructed = false;
            frequencyWinner = "DIST";
            Debug.DrawLine(sourcePos, playerPos, Color.green);
        }

        float finalTransmission = distFactor * wallFactor;
        float occlusionFreq = Mathf.Lerp(currentProfile.closedFreq, currentProfile.openFreq, finalTransmission);

        targetVol = Mathf.Lerp(currentProfile.closedVol, currentProfile.openVol, finalTransmission);

        float forwardDot = Vector3.Dot(playerTransform.forward, dirNormalized);
        float directionFreq = currentProfile.frontFreq;

        if (forwardDot < 0)
        {
            float fatness = Mathf.Abs(forwardDot);
            directionFreq = Mathf.Lerp(currentProfile.frontFreq, currentProfile.backFreq, fatness);
        }

        if (occlusionFreq < directionFreq)
        {
            targetFreq = occlusionFreq;
        }
        else
        {
            targetFreq = directionFreq;
            frequencyWinner = "ANGLE";
        }

        float rawPan = Vector3.Dot(playerTransform.right, dirNormalized);
        float currentDistFactor = Mathf.Clamp01(dist / currentProfile.maxHearingDistance);
        float dynamicStrength = Mathf.Lerp(minPanStrength, maxPanStrength, currentDistFactor);

        targetPan = rawPan * dynamicStrength;
    }

    void HandleDifferentRoomPhysics(Vector3 sourcePos, Vector3 playerPos, float dist)
    {
        // 1. PORTAL TARAMASI YAP
        if (usePortalScanning)
        {
            // AudioSourceBase'deki fonksiyonu tetikle (Portal var mı?)
            ScanForPortal();
        }

        if (portalFound)
        {
            // --- SENARYO A: KAPI/DELİK BULUNDU (HELMHOLTZ ETKİSİ) ---
            // Ses kapıdan sızıyor. Oyuncu sesi kapının olduğu yerden duymalı.

            // Görselleştirme: Camgöbeği (Cyan) yol
            Debug.DrawLine(sourcePos, portalPosition, Color.cyan);
            Debug.DrawLine(portalPosition, playerPos, Color.cyan);

            // 1. YENİ MESAFE HESABI (Yol Uzadı)
            // Sesin kat ettiği yol: Kaynaktan kapıya + Kapıdan kulağa
            float distSourceToPortal = Vector3.Distance(sourcePos, portalPosition);
            float distPortalToPlayer = Vector3.Distance(portalPosition, playerPos);
            float totalPathLength = distSourceToPortal + distPortalToPlayer;

            currentDistance = totalPathLength;

            // 2. YÖN VE PAN (Portal Origininden Duyarız)
            // Player'ın kafasına göre portal ne tarafta kalıyor?
            Vector3 dirToPortal = (portalPosition - playerPos).normalized;

            // İletim (Transmission): Kapı açık olduğu için ses havalı ortamdan (1.0f) gelir.
            // Ama mesafe arttığı için bir düşüş olur.
            float combinedDistFactor = Mathf.Clamp01(1f - (totalPathLength / currentProfile.maxHearingDistance));

            // Pan'ı Portal'a göre hesapla (Kaynak yönüne göre değil!)
            // '1.0f' gönderiyoruz çünkü kapı aralığında duvar engeli yok varsayıyoruz.
            CalculatePanAndFreq(totalPathLength, dirToPortal, 1.0f);

            // 3. SES AYARLARI (Full Wet Sızıntı)
            // Ses uzaktan geldiği için kısılsın ama boğulmasın (Frekans açık kalsın)
            targetVol = Mathf.Lerp(0f, currentProfile.openVol, combinedDistFactor);
            targetFreq = currentProfile.openFreq; // Kapıdan net ses gelir

            // FMOD PARAMETRELERİ (Aux Reverb)
            // Burada "Ben farklı odadayım, kapıdan duyuluyorum" diyip Aux Reverb'i fulleyebilirsin.
            // Örn: emitter.EventInstance.setParameterByName("AuxReverb", 1.0f);
        }
        else
        {
            // --- SENARYO B: KAPALI KUTU (SADECE DUVAR) ---
            // Portal bulunamadı. Ses mecburen duvardan boğuk şekilde geçecek.

            Debug.DrawLine(sourcePos, playerPos, Color.magenta);

            // Klasik duvar arkası hesaplamasını kullan
            HandleDirectPhysics(sourcePos, playerPos, dist);

            // EKSTRA CEZA: Farklı odadayız ve kapı yok.
            // Normal duvar arkasından çok daha boğuk olmalı.
            targetFreq = Mathf.Min(targetFreq, 400f); // Max 400Hz (Heavy Lowpass)
            targetVol *= 0.6f; // Ses iyice kısılır
            portalFound = false;
            isScannerLocked = false;
            roomThroughPortal = null;
            // Aux Reverb kapalı (veya çok az)
            // emitter.EventInstance.setParameterByName("AuxReverb", 0.0f);
        }
    }
        void CalculatePanAndFreq(float dist, Vector3 dirNormalized, float transmission)
    {
        // Frekans hesapları...
        // ... (Eski kodundaki Angle/Occlusion logic'i) ...

        // Pan hesapları...
        float rawPan = Vector3.Dot(playerTransform.right, dirNormalized);
        float currentDistFactor = Mathf.Clamp01(dist / currentProfile.maxHearingDistance);
        targetPan = rawPan * Mathf.Lerp(minPanStrength, maxPanStrength, currentDistFactor);
    }
}


