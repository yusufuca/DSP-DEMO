using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using TMPro;
using FMOD;

public class ReverbManager : MonoBehaviour
{
    public static ReverbManager RevInstance { get; private set; }

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

    private FMOD.Studio.PARAMETER_ID reverbTimeID, earlyDelayID, lateDelayID, onOFID, diffusionID, densityID, hfDecayID, hfRefID,
        highCutID, delayMixID, dLateID, dHighCutID, outdoorReverbTimeID, dFeedbackID, lowGainID, lowFreqID, dReverbMixID;

    public EventReference rifleSfx;
    public EventReference pistolSfx;
    public EventReference clapSFX;
    public EventReference walkSFX;
    public EventReference runSFX;
    public EventReference musicLoops;

    private EventInstance musicInstance;

    public TextMeshProUGUI reverbTimeText;
    public TextMeshProUGUI roomDataText;
    public TextMeshProUGUI roomModeText;

    private DetectingWall detect;
    private FloodFill fill;

    [Header("Outdoor / Probe Settings")]
    public float maxProbeDistance = 50f;

 
    private float dLateDelayCalc = 0f;
    private float dFeedbackCalc = 0f;
    private float dDiffusionCalc = 0f;
    private float dHighCutCalc = 20000f;
    private float outMixCalc = 0f; 
    private float outdoorReverbTimeCalc = 0f;
    private float dReverbMixCalc = 0f; 
    private void Start()
    {
        detect = DetectingWall.DetectInstance;
        fill = FloodFill.FillInstance;

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

    private void Update()
    {
        ParameterUpdater();
    }

    void GetParamID(string name, out FMOD.Studio.PARAMETER_ID id)
    {
        FMOD.Studio.PARAMETER_DESCRIPTION desc;
        RuntimeManager.StudioSystem.getParameterDescriptionByName(name, out desc);
        id = desc.id;
    }

    void ParameterUpdater()
    {

        bool isIndoors = detect.distances[4] > 0 && detect.distances[4] < 20f;


        float mainRoomVolume = fill.totalRoomVolume * (detect.distances[4] + detect.distances[5]);
        float minWallDist = detect.maxDistance;
        for (int i = 0; i < 4; i++) { if (detect.distances[i] > 0 && detect.distances[i] < minWallDist) minWallDist = detect.distances[i]; }

        float rawReverbTime = mainRoomVolume * fill.totalRoomHardness * detect.reverbSizePar * 1000f;
        float finalReverbTime = Mathf.Clamp(rawReverbTime, 100f, 20000f);


        float rawEarlyDelay = (minWallDist / 343f) * 1000f;
        float finalEarlyDelay = Mathf.Clamp(rawEarlyDelay, 0, 300);
        float finalLateDelay = Mathf.Clamp((rawReverbTime * 0.02f), 0, 100);


        float difVolumeBonus = (finalReverbTime / 20000) * 20;
        float finalDiffusion = Mathf.Clamp((fill.totalRoomJagness * 100) + difVolumeBonus, 0, 100);

        if (detect.distances[4] + detect.distances[5] > 10)
        {

            float heightBonus = (detect.distances[4] + detect.distances[5]) - 10f;

      
            finalLateDelay += heightBonus * 1.5f;

            finalDiffusion += heightBonus * 1.0f;
        }

        if (finalReverbTime > 2000f && minWallDist < 2.0f)
        {
            finalLateDelay += 30f;
        }

        finalLateDelay = Mathf.Clamp(finalLateDelay, 0, 100);
        finalDiffusion = Mathf.Clamp(finalDiffusion, 0, 100);

        float sizePenalty = (finalReverbTime / 20000f) * 50f;
        float jagnessBonus = fill.totalRoomJagness * 40f;
        float finalDensity = Mathf.Clamp(100f - sizePenalty + jagnessBonus, 0f, 100f);

        float finalHfDecay = Mathf.Clamp(10f + (fill.totalRoomHardness * 90f), 10f, 100f);


        float materialHighCut = 2000f + (fill.totalRoomHardness * 18000f);
        float airAbsorption = 1f - (finalReverbTime / 20000f * 0.8f);
        float finalHighCut = Mathf.Clamp(materialHighCut * airAbsorption, 20f, 20000f);
        float finalLowFreq = Mathf.Clamp(250f - (finalReverbTime / 20000f * 100f), 20f, 1000f);

        float baseLowGain = Mathf.Lerp(-36f, 12f, fill.totalRoomHardness);

        float boundaryBonus = 0f;
        if (minWallDist < 1.5f)
        {
           
            boundaryBonus = Mathf.Lerp(16f, 0f, minWallDist);
        }

        float finalLowGain = baseLowGain + boundaryBonus;

        float finalDelayMixNormalized = 0f;
        float finalFeedback = detect.dFeedBack;
        int onOF = 1;

        float finalOutdoorReverbTime = 0f;
        float finalDHighCut = 20000f;
        float finalDReverbMixNormalized = 0f; 

  
        if (!isIndoors)
        {
      
            CalculateOutdoorEcho();

       
            finalReverbTime = outdoorReverbTimeCalc;
            finalOutdoorReverbTime = outdoorReverbTimeCalc;

            finalEarlyDelay = dLateDelayCalc; 
            finalLateDelay = 0f;

            finalDiffusion = dDiffusionCalc;
            finalDensity = 100f;

            finalHighCut = dHighCutCalc;
            finalDHighCut = dHighCutCalc;

            finalDelayMixNormalized = outMixCalc;
            finalFeedback = dFeedbackCalc;

            finalDReverbMixNormalized = dReverbMixCalc;

            
            onOF = 0;

           
            if (finalDelayMixNormalized <= 0.01f) onOF = 0; 

            if (reverbTimeText != null)
                reverbTimeText.text = $"OUTDOOR | Echo: {finalEarlyDelay:F0}ms | MixNorm: {outMixCalc:F2} | RevMix: {finalDReverbMixNormalized}";
        }
        else
        {
            if (reverbTimeText != null)
             
            reverbTimeText.text = $"Reverb Time is: {finalReverbTime}  Early Delay is: {finalEarlyDelay} LateDelay is: {finalLateDelay} Diffusion: {finalDiffusion}";
            roomModeText.text = $"HF Decay: {finalHfDecay} HF Reference {fill.totalRoomHardness} HighCut: {finalHighCut}";
            roomDataText.text = $"TotalRoomVolume: {mainRoomVolume} Avarage Room Hardness: {fill.totalRoomHardness} Avarage Room jagness: {fill.totalRoomJagness}";
        }


        float finalDelayMixDB = Mathf.Lerp(-80f, 10f, finalDelayMixNormalized);

        float finalDReverbMixDB = Mathf.Lerp(-80f, 0f, finalDReverbMixNormalized);

        UpdateReverb(
            finalReverbTime,
            finalEarlyDelay,
            finalLateDelay,
            onOF,
            finalDiffusion,
            finalDensity,
            finalHfDecay,
            finalHighCut,
            finalDelayMixDB,
            dLateDelayCalc,
            finalLowFreq,
            finalLowGain,
            finalOutdoorReverbTime,
            finalDHighCut,
            finalFeedback,
            finalDReverbMixDB
        );
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

    public void UpdateReverb(float reverbTime, float earlyDelay, float lateDelay, int onOF, float diffusion, float density, float hfDecayRatio,
         float highCut, float delayMix,float dLateDelay, float lowFreq, float lowGain, float outdoorReverbTime, float dHighCut, float dFeedback, float dReverbMix)
    {
        RuntimeManager.StudioSystem.setParameterByID(reverbTimeID, reverbTime);
        RuntimeManager.StudioSystem.setParameterByID(earlyDelayID, earlyDelay);
        RuntimeManager.StudioSystem.setParameterByID(lateDelayID, lateDelay);
        RuntimeManager.StudioSystem.setParameterByID(onOFID, onOF);

        RuntimeManager.StudioSystem.setParameterByID(diffusionID, diffusion);
        RuntimeManager.StudioSystem.setParameterByID(densityID, density);

        RuntimeManager.StudioSystem.setParameterByID(hfDecayID, hfDecayRatio);
      
        RuntimeManager.StudioSystem.setParameterByID(highCutID, highCut);
        RuntimeManager.StudioSystem.setParameterByID(lowFreqID, lowFreq);
        RuntimeManager.StudioSystem.setParameterByID(lowGainID, lowGain);

 
        RuntimeManager.StudioSystem.setParameterByID(dLateID, dLateDelay);
  

        RuntimeManager.StudioSystem.setParameterByID(outdoorReverbTimeID, outdoorReverbTime);
        RuntimeManager.StudioSystem.setParameterByID(dHighCutID, dHighCut);
        RuntimeManager.StudioSystem.setParameterByID(dFeedbackID, dFeedback);

        
        RuntimeManager.StudioSystem.setParameterByID(dReverbMixID, dReverbMix);

        RuntimeManager.StudioSystem.setParameterByID(delayMixID, delayMix);

     

    }
}