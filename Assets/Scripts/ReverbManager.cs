using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

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



    public void UpdateReverb(float avRoomSize, float earlyDelay, float lateDelay)
    {
        RuntimeManager.StudioSystem.setParameterByName("reverbTime", avRoomSize);
        RuntimeManager.StudioSystem.setParameterByName("earlyDelay", earlyDelay);
        RuntimeManager.StudioSystem.setParameterByName("lateDelay", lateDelay);

    }
}
