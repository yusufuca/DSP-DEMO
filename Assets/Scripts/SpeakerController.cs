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
    protected override void CalculatePhysics()
    {
       
        if (currentProfile == null) return;

        
        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;
        Vector3 toSource = sourcePos - playerPos; 

        float dist = toSource.magnitude;
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
    public override void OnRoomEnter()
    {
        FloodFill myScanner = GetComponent<FloodFill>();

        if (myScanner != null)
        {
            Debug.Log($"[Speaker] {name} tarama başlatıyor...");
            // Odayı tara ve Manager'a kaydettir
            myScanner.GetOrCalculateRoom((room) =>
            {

               
                Debug.Log($"[Speaker] Radyo odayı tanımladı: {room.roomID}");
           
            });
        }
        else
        {
            Debug.LogError("Speaker static ama üzerinde FloodFill scripti yok!");
        }
    }
}