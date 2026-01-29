using FMOD;
using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ReverbManager : MonoBehaviour
{
    public static ReverbManager RevInstance { get; private set; }

    [Header("References")]
    public Transform playerTransform;
    private DetectingWall detect;
    private RoomManager.RoomData currentRoomData;
    private FloodFill playerScanner;

    private FMOD.Studio.PARAMETER_ID reverbTimeID, earlyDelayID, lateDelayID, onOFID, diffusionID, densityID, hfDecayID, hfRefID,
       highCutID, delayMixID, dLateID, dHighCutID, outdoorReverbTimeID, dFeedbackID, lowGainID, lowFreqID, dReverbMixID;


    public struct ReverbState
    {
        public float reverbTime;
        public float earlyDelay;
        public float lateDelay;
        public float diffusion;
        public float density;
        public float highCut;
        public float hfDecay; 
        public float lowFreq; 
        public float lowGain;
        public float onOF;

        public float dLateDelay;
        public float outdoorReverbTime;
        public float dHighCut;
        public float dFeedback;
        public float delayMixDB;
        public float dReverbMixDB;
    }

    public ReverbState mainReverbState;
    public ReverbState aux1State;
    public ReverbState aux2State;

    [Header("UI & Debug")]
    public TextMeshProUGUI reverbTimeText;
    public TextMeshProUGUI roomDataText;
    public TextMeshProUGUI roomModeText;

    private float dLateDelayCalc = 0f;
    private float dFeedbackCalc = 0f;
    private float dDiffusionCalc = 0f;
    private float dHighCutCalc = 20000f;
    private float outMixCalc = 0f;
    private float outdoorReverbTimeCalc = 0f;
    private float dReverbMixCalc = 0f;

    private float scanTimer = 0f;

    private float scanInterval = 5f;
    private bool isChecking = false;
   

    [Header("Outdoor / Probe Settings")]
    public float maxProbeDistance = 50f;

    private void Awake()
    {
        if (RevInstance != null && RevInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            RevInstance = this;
        }

       
    }

   
    public EventReference rifleSfx;
    public EventReference pistolSfx;
    public EventReference clapSFX;
    public EventReference walkSFX;
    public EventReference runSFX;
    public EventReference musicLoops;
    private EventInstance musicInstance;
 
    private void Start()
    {
        detect = DetectingWall.DetectInstance;

       



        InitializeFMODParams();
    }

    private void Update()
    {

        CalculateMainReverb();
        // CalculateAuxReverbs();


        ApplyMainReverbToFMOD();
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("[ReverbManager] 'Player' tag'ine sahip obje bulunamadı!");
                return;
            }
        }
        if (playerScanner == null) playerScanner = playerTransform.gameObject.AddComponent<FloodFill>();
       

       
        scanTimer += Time.deltaTime;
        if (detect.distances[4] > 2 && !isChecking && scanTimer >= scanInterval)
        {
            CheckCurrentRoom();
            scanTimer = 0f;
        }

        
    }
  
    void CheckCurrentRoom()
    {
        if (RoomManager.Instance == null) return;

        Vector3 checkPos = playerTransform.position;

        // ADIM 1: Zaten bildiğimiz bir odada mıyız? (Cache Kontrolü)
        if (RoomManager.Instance.TryGetRoomAt(checkPos, out RoomManager.RoomData room))
        {
            Debug.Log($"[ReverbManager] Kayıtlı odaya girildi: {room.roomID}");
            // Evet, kayıtlı bir odadayız.
           
                currentRoomData = room;
               
           
            // Kayıtlı odadaysak başka işlem yapmaya gerek yok.
        }
        else
        {
            // ADIM 2: Kayıtlı değil. Peki tepemizde tavan var mı? (Yeni Oda Taraması)
            float ceilingDist = detect.distances[4];
            bool hasCeiling = ceilingDist > 0 && ceilingDist < 20f;

            if (hasCeiling)
            {
                // Tavan var ama RoomManager bu odayı bilmiyor. -> TARA!
                if (!playerScanner.IsScanning)
                {
                    isChecking = true;
                    Debug.Log($"[ReverbManager] YENİ ODA TESPİT EDİLDİ (Tavan: {ceilingDist:F1}m). Tarama Başlatılıyor...");

                    // FloodFill'i başlat
                    playerScanner.GetOrCalculateRoom((newRoom) =>
                    {
                        currentRoomData = newRoom;
                        Debug.Log($"[ReverbManager] Tarama bitti, oda eklendi: {newRoom.roomID}");
                        isChecking = false;
                    });
                }
            }
            else
            {
                // Tavan da yok, kayıt da yok -> Dışarısı
                currentRoomData = null;
                isChecking = false;
            }
        }
    }

    void CalculateMainReverb()
    {
        bool isCeilingDetected = detect.distances[4] > 0 && detect.distances[4] < 20f;
        bool hasRoomData = currentRoomData != null;

        if (isCeilingDetected && hasRoomData)
        {

            float vol = currentRoomData.volume;
            float hard = currentRoomData.hardness;
            float jag = currentRoomData.jagness;

            
            float rawReverbTime = vol * hard * detect.reverbSizePar * 1000f;
            mainReverbState.reverbTime = Mathf.Clamp(rawReverbTime, 100f, 20000f);

            float minWallDist = detect.maxDistance;
            for (int i = 0; i < 4; i++) { if (detect.distances[i] > 0 && detect.distances[i] < minWallDist) minWallDist = detect.distances[i]; }

            float rawEarlyDelay = (minWallDist / 343f) * 1000f;
            mainReverbState.earlyDelay = Mathf.Clamp(rawEarlyDelay, 0, 300);

            mainReverbState.lateDelay = Mathf.Clamp((mainReverbState.reverbTime * 0.02f), 0, 100);

            if (detect.distances[4] + detect.distances[5] > 10)
            {
                float heightBonus = (detect.distances[4] + detect.distances[5]) - 10f;
                mainReverbState.lateDelay += heightBonus * 1.5f;
            }

            if (mainReverbState.reverbTime > 2000f && minWallDist < 2.0f)
            {
                mainReverbState.lateDelay += 30f;
            }
            mainReverbState.lateDelay = Mathf.Clamp(mainReverbState.lateDelay, 0, 100);


            float difVolumeBonus = (mainReverbState.reverbTime / 20000) * 20;
            mainReverbState.diffusion = Mathf.Clamp((jag * 100) + difVolumeBonus, 0, 100);


            if (detect.distances[4] + detect.distances[5] > 10)
            {
                float heightBonus = (detect.distances[4] + detect.distances[5]) - 10f;
                mainReverbState.diffusion += heightBonus * 1.0f;
            }
            mainReverbState.diffusion = Mathf.Clamp(mainReverbState.diffusion, 0, 100);

            float sizePenalty = (mainReverbState.reverbTime / 20000f) * 50f;
            float jagnessBonus = jag * 40f;
            mainReverbState.density = Mathf.Clamp(100f - sizePenalty + jagnessBonus, 0f, 100f);

            mainReverbState.hfDecay = Mathf.Clamp(10f + (hard * 90f), 10f, 100f);

            float materialHighCut = 2000f + (hard * 18000f);
            float airAbsorption = 1f - (mainReverbState.reverbTime / 20000f * 0.8f);
            mainReverbState.highCut = Mathf.Clamp(materialHighCut * airAbsorption, 20f, 20000f);

            mainReverbState.lowFreq = Mathf.Clamp(250f - (mainReverbState.reverbTime / 20000f * 100f), 20f, 1000f);

            float baseLowGain = Mathf.Lerp(-36f, 12f, hard);
            float boundaryBonus = 0f;
            if (minWallDist < 1.5f) boundaryBonus = Mathf.Lerp(16f, 0f, minWallDist);
            mainReverbState.lowGain = baseLowGain + boundaryBonus;

            mainReverbState.onOF = 1f;
            mainReverbState.delayMixDB = -80f;
            mainReverbState.dReverbMixDB = -80f;

            if (roomDataText != null)
                roomDataText.text = $"INDOOR | Vol:{vol:F0} | Hard:{hard:F2}";
        }
        else
        {
           
            CalculateOutdoorEcho(); 

            mainReverbState.reverbTime = outdoorReverbTimeCalc;
            mainReverbState.outdoorReverbTime = outdoorReverbTimeCalc;

           
            mainReverbState.earlyDelay = dLateDelayCalc;
            mainReverbState.lateDelay = 0f;

            mainReverbState.diffusion = dDiffusionCalc;
            mainReverbState.density = 100f;
            mainReverbState.highCut = dHighCutCalc;
            mainReverbState.dHighCut = dHighCutCalc;

            mainReverbState.dFeedback = dFeedbackCalc;
            mainReverbState.dLateDelay = dLateDelayCalc;

            mainReverbState.onOF = 0f;

        
            mainReverbState.delayMixDB = Mathf.Lerp(-80f, 10f, outMixCalc);
            mainReverbState.dReverbMixDB = Mathf.Lerp(-80f, 0f, dReverbMixCalc);

            if (roomDataText != null) roomDataText.text = $"OUTDOOR | Echo:{dLateDelayCalc:F0}ms";
        }
    }

    void ApplyMainReverbToFMOD()
    {
        RuntimeManager.StudioSystem.setParameterByID(reverbTimeID, mainReverbState.reverbTime);
        RuntimeManager.StudioSystem.setParameterByID(earlyDelayID, mainReverbState.earlyDelay);
        RuntimeManager.StudioSystem.setParameterByID(lateDelayID, mainReverbState.lateDelay);
        RuntimeManager.StudioSystem.setParameterByID(onOFID, mainReverbState.onOF);

        RuntimeManager.StudioSystem.setParameterByID(diffusionID, mainReverbState.diffusion);
        RuntimeManager.StudioSystem.setParameterByID(densityID, mainReverbState.density);
        RuntimeManager.StudioSystem.setParameterByID(hfDecayID, mainReverbState.hfDecay);
        RuntimeManager.StudioSystem.setParameterByID(highCutID, mainReverbState.highCut);

        RuntimeManager.StudioSystem.setParameterByID(lowFreqID, mainReverbState.lowFreq);
        RuntimeManager.StudioSystem.setParameterByID(lowGainID, mainReverbState.lowGain);

        // Outdoor / Echo Params
        RuntimeManager.StudioSystem.setParameterByID(dLateID, mainReverbState.dLateDelay);
        RuntimeManager.StudioSystem.setParameterByID(outdoorReverbTimeID, mainReverbState.outdoorReverbTime);
        RuntimeManager.StudioSystem.setParameterByID(dHighCutID, mainReverbState.dHighCut);
        RuntimeManager.StudioSystem.setParameterByID(dFeedbackID, mainReverbState.dFeedback);
        RuntimeManager.StudioSystem.setParameterByID(dReverbMixID, mainReverbState.dReverbMixDB);
        RuntimeManager.StudioSystem.setParameterByID(delayMixID, mainReverbState.delayMixDB);
    }

    void CalculateOutdoorEcho()
    {
        float closestDist = maxProbeDistance;
        int closestIndex = -1;


        for (int i = 0; i < 4; i++)
        {
            if (detect.distances[i] > 0 && detect.distances[i] < closestDist)
            {
                closestDist = detect.distances[i];
                closestIndex = i;
            }
        }


        if (closestIndex == -1)
        {
            dLateDelayCalc = 0; dFeedbackCalc = 0; dDiffusionCalc = 0;
            dHighCutCalc = 20000; outMixCalc = 0; outdoorReverbTimeCalc = 0;
            dReverbMixCalc = 0;
            return;
        }


        Vector3 hitPoint = detect.wallOrigins[closestIndex];
        Vector3 wallNormal = detect.wallNormals[closestIndex];
        Vector3 probeOrigin = hitPoint + (wallNormal * 2.0f);




        Vector3 rainOrigin = probeOrigin + (Vector3.up * 15f);


        UnityEngine.Debug.DrawLine(hitPoint, probeOrigin, Color.yellow);
        UnityEngine.Debug.DrawLine(rainOrigin, probeOrigin, Color.cyan);

        bool isStructure = false;
        float structureVolume = 0f;
        RaycastHit roofHit;


        if (Physics.Raycast(rainOrigin, Vector3.down, out roofHit, 15f, detect.WallLayer))
        {


            if (roofHit.point.y > probeOrigin.y + 0.5f)
            {
                isStructure = true;


                float sizeX = GetRayDist(probeOrigin, Vector3.right) + GetRayDist(probeOrigin, Vector3.left);
                float sizeZ = GetRayDist(probeOrigin, Vector3.forward) + GetRayDist(probeOrigin, Vector3.back);


                float floorDist = GetRayDist(probeOrigin, Vector3.down);
                float totalHeight = (roofHit.point.y - probeOrigin.y) + floorDist;

                structureVolume = sizeX * sizeZ * totalHeight;
            }
        }

        dLateDelayCalc = (closestDist / 343f) * 1000f;

        if (isStructure)
        {

            dReverbMixCalc = 1.0f;
            outdoorReverbTimeCalc = Mathf.Clamp(0.4f + (structureVolume * 0.005f), 0.4f, 1.2f);
            dHighCutCalc = Mathf.Clamp(20000f - (structureVolume * 15f), 1000f, 20000f);
            dDiffusionCalc = Mathf.Clamp(60f + (structureVolume * 0.5f), 60f, 100f);
            dFeedbackCalc = Mathf.Clamp(30f + (structureVolume * 0.1f), 30f, 80f);
            outMixCalc = 0.85f;
        }
        else
        {

            dReverbMixCalc = 0.0f;
            outdoorReverbTimeCalc = 0f;
            dHighCutCalc = 20000f;
            dDiffusionCalc = 0f;
            dFeedbackCalc = 15f;
            outMixCalc = 0.6f;
        }
    }

    float GetRayDist(Vector3 origin, Vector3 dir)
    {
        if (Physics.Raycast(origin, dir, out RaycastHit hit, 20f, detect.WallLayer)) return hit.distance;
        return 20f;
    }


    void InitializeFMODParams()
    {
        GetParamID("reverbTime", out reverbTimeID);
        GetParamID("earlyDelay", out earlyDelayID);
        GetParamID("lateDelay", out lateDelayID);
        GetParamID("onOF", out onOFID);
        GetParamID("diffusion", out diffusionID);
        GetParamID("density", out densityID);
        GetParamID("hfDecayRatio", out hfDecayID);
        GetParamID("hfReference", out hfRefID);
        GetParamID("highCut", out highCutID);
        GetParamID("delayMix", out delayMixID);
        GetParamID("dLateDelay", out dLateID);
        GetParamID("lowGain", out lowGainID);
        GetParamID("lowFreq", out lowFreqID);
        GetParamID("outdoorReverbTime", out outdoorReverbTimeID);
        GetParamID("dHighCut", out dHighCutID);
        GetParamID("dFeedback", out dFeedbackID);
        GetParamID("dReverbMix", out dReverbMixID);
    }


    void GetParamID(string name, out FMOD.Studio.PARAMETER_ID id)
    {
        FMOD.Studio.PARAMETER_DESCRIPTION desc;
        RuntimeManager.StudioSystem.getParameterDescriptionByName(name, out desc);
        id = desc.id;
    }
        
  

  
}