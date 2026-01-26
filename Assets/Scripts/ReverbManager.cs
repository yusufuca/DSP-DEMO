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

    private FMOD.Studio.PARAMETER_ID reverbTimeID, earlyDelayID, lateDelayID, onOFID, diffusionID, densityID, hfDecayID, hfRefID, highCutID, delayMixID, dEarlyID, dLateID, dDiffID, lowGainID, lowFreqID;

    public EventReference rifleSfx;
    public EventReference pistolSfx;
    public EventReference clapSFX;
    public EventReference walkSFX;
    public EventReference runSFX;

    public TextMeshProUGUI reverbTimeText;
    public TextMeshProUGUI roomDataText;
    public TextMeshProUGUI roomModeText;

    private DetectingWall detect;
    private FloodFill fill;
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
        GetParamID("dEarlyDelay", out dEarlyID);
        GetParamID("dLateDelay", out dLateID);
        GetParamID("dDiffusion", out dDiffID);
        GetParamID("lowGain", out lowGainID);
        GetParamID("lowFreq", out lowFreqID);
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
        

        float totalDistances = 0f;

        for (int i = 0; i < 6; i++) totalDistances += detect.distances[i] / 6;

        float totalHardness = 0f;

        for (int i = 0; i < 6; i++) totalHardness += detect.hardnesses[i] / 6;

        float totalJagness = 0f;

        for (int i = 0; i < 6; i++) totalJagness += detect  .jagnesses[i] / 6;

        float mainRoomVolume = fill.totalRoomVolume * (detect.distances[4] + detect.distances[5]);

        float minWallDist = detect.maxDistance;
        /* REVERB TIME */



        float rawReverbTime = mainRoomVolume * fill.totalRoomHardness * detect.reverbSizePar * 1000;
        float minReverbFmodTime = 100f;
        float maxReverbFmodTime = 20000f;
        float reverbTime = Mathf.Clamp(rawReverbTime, minReverbFmodTime, maxReverbFmodTime);
     



        /* EARLY DELAY */

        for (int i = 0; i < 4; i++)
        {
            if (detect.distances[i] > 0 && detect.distances[i] < minWallDist)
            {
                minWallDist = detect.distances[i];

            }
        }


        float rawEarlyDelay = (minWallDist  / 343) * 1000;
        float earlyDelay = Mathf.Clamp(rawEarlyDelay, 0, 300);
        


        /* LATE DELAY */

        float rawLateDelay = ((mainRoomVolume * detect.reverbSizePar * 1000f) * 0.002f);

        float minLateFmodTime = 0;
        float maxLateFmodTime = 100;

        float lateDelay = Mathf.Clamp(rawLateDelay, minLateFmodTime, maxLateFmodTime);

      

        /* DIFFUSION */

        float rawDiffusion = fill.totalRoomJagness * 100;

        float difVolumeBonus = (rawReverbTime / 20000) * 20;

        float diffusion = rawDiffusion + difVolumeBonus;

        diffusion = Mathf.Clamp(diffusion, 0, 100);

         

        /* DENSITY */

        float sizePenalty = (rawReverbTime / 20000f) * 50f;
        float jagnessBonus = fill.totalRoomJagness * 40f;

        float rawDensity = 100f - sizePenalty + jagnessBonus;

        float density = Mathf.Clamp(rawDensity, 0f, 100f);




        /* HF DECAY */

        float rawHfDecayRatio = 10f + (fill.totalRoomHardness * 90f);

        float hfDecayRatio = Mathf.Clamp(rawHfDecayRatio, 10f, 100f);

        /* HF REFERENCE */


   

        float hfReference = fill.totalRoomHardness;

        /* HIGH CUT */

        float materialHighCut = 2000f + (fill.totalRoomHardness * 18000f);
        float airAbsorption = 1f - (rawReverbTime / 20000f * 0.8f);
        float rawHighCut = materialHighCut * airAbsorption;

        float minHighCutFmod = 20f;
        float maxHighCutFmod = 20000f;

        float highCut = Mathf.Clamp(rawHighCut, minHighCutFmod, maxHighCutFmod);


       


        /* LOW FREQ */

        float rawLowFreq = 250f - (rawReverbTime / 20000f * 100f);

        float minLowFreqFmod = 20f;
        float maxLowFreqFmod = 1000f;

        float lowFreq = Mathf.Clamp(rawLowFreq, minLowFreqFmod, maxLowFreqFmod);

       

        /* LOW GAIN */

        float rawLowGain = (fill.totalRoomHardness * 24f) - 24f;

        float minLowGainFmod = -80f;
        float maxLowGainFmod = 0f;

        float lowGain = (rawLowGain - minLowGainFmod) / (maxLowGainFmod - minLowGainFmod);
        lowGain = Mathf.Clamp(lowGain, 0f, 1f);

        /* EARLY LATE MIX */

        float delayMix = reverbTime * detect.reverbSizePar;

        /* REVERB ON OF */

        int onOF = 1;

        if (detect.distances[4] > 0)
        {
            onOF = 1;
            reverbTimeText.text = $"Reverb Time is: {reverbTime} {rawReverbTime} Early Delay is: {earlyDelay} {rawEarlyDelay} LateDelay is: {lateDelay} Diffusion: {diffusion}";
            roomModeText.text = $"HF Decay: {hfDecayRatio} HF Reference {hfReference} HighCut: {highCut}";
            roomDataText.text = $"TotalRoomVolume: {mainRoomVolume} Avarage Room Hardness: {fill.totalRoomHardness} Avarage Room jagness: {fill.totalRoomJagness} Raw High Cut: {rawHighCut} {highCut}";
        }
        else
        {
            onOF = 0;
            reverbTimeText.text = $"Early Delay: {detect.dEarlyDelay} Late Delay: {detect.dLateDelay} FeedBack is: {detect.dFeedBack}";
        }


        UpdateReverb(reverbTime, earlyDelay, lateDelay, onOF, diffusion, density, hfDecayRatio, hfReference, highCut, delayMix, detect.dEarlyDelay, detect.dLateDelay, detect.dFeedBack,lowFreq,lowGain);


    }


    public void UpdateReverb(float reverbTime, float earlyDelay, float lateDelay,int onOF, float diffusion, float density, float hfDecayRatio, float hfReference, float highCut,float delayMix, float dEarlyDelay, float dLateDelay, float dDiffusion, float lowFreq, float lowGain)
    {
        RuntimeManager.StudioSystem.setParameterByID(reverbTimeID, reverbTime);
        RuntimeManager.StudioSystem.setParameterByID(earlyDelayID, earlyDelay);
        RuntimeManager.StudioSystem.setParameterByID(lateDelayID, lateDelay);
        RuntimeManager.StudioSystem.setParameterByID(onOFID, onOF);

        RuntimeManager.StudioSystem.setParameterByID(diffusionID, diffusion);
        RuntimeManager.StudioSystem.setParameterByID(densityID, density);

        RuntimeManager.StudioSystem.setParameterByID(hfDecayID, hfDecayRatio);  
        RuntimeManager.StudioSystem.setParameterByID(hfRefID, hfReference);
        RuntimeManager.StudioSystem.setParameterByID(highCutID, highCut);
        RuntimeManager.StudioSystem.setParameterByID(lowFreqID, lowFreq);
        RuntimeManager.StudioSystem.setParameterByID(lowGainID, lowGain);

        RuntimeManager.StudioSystem.setParameterByID(dEarlyID, dEarlyDelay);
        RuntimeManager.StudioSystem.setParameterByID(dLateID, dLateDelay);
        RuntimeManager.StudioSystem.setParameterByID(dDiffID, dDiffusion);
    }
    
}
