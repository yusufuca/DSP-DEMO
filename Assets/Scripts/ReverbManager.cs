using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

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
    
    public EventReference rifleSfx;
    public EventReference pistolSfx;


    public void UpdateReverb(float avRoomSize, float earlyDelay, float lateDelay,int onOF, float diffusion, float density, float hfDecayRatio, float hfReference, float highCut,float delayMix)
    {
        RuntimeManager.StudioSystem.setParameterByName("reverbTime", avRoomSize);
        RuntimeManager.StudioSystem.setParameterByName("earlyDelay", earlyDelay);
        RuntimeManager.StudioSystem.setParameterByName("lateDelay", lateDelay);
        RuntimeManager.StudioSystem.setParameterByName("onOF", onOF);
        RuntimeManager.StudioSystem.setParameterByName("diffusion", diffusion);
        RuntimeManager.StudioSystem.setParameterByName("density", density);
        RuntimeManager.StudioSystem.setParameterByName("hfDecayRatio", hfDecayRatio);
        RuntimeManager.StudioSystem.setParameterByName("hfReference", hfReference);
        RuntimeManager.StudioSystem.setParameterByName("highCut", highCut);
        RuntimeManager.StudioSystem.setParameterByName("delayMix", delayMix);
    }
}
