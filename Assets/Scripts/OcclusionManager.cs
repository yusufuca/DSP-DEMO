using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class GlobalOcclusionManager : MonoBehaviour
{
    [Header("Settings")]
    public Transform playerTransform;
    public Transform soundSource;
    public LayerMask obstructionLayer;
    public float occlusionSpeed = 10f;
    public float maxDistance = 50f;

    private FMOD.Studio.PARAMETER_ID occVolParamID, occEQParamID;


    private float targetFreq = 22000f;
    private float targetVol = 0f;      



    private DetectingWall detect;

    private void Start()
    {
        detect = DetectingWall.DetectInstance;

        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

      
        GetParamID("OccEQ", out occEQParamID);
        GetParamID("OccVol", out occVolParamID);
    }
    void GetParamID(string name, out FMOD.Studio.PARAMETER_ID id)
    {
        FMOD.Studio.PARAMETER_DESCRIPTION desc;
        RuntimeManager.StudioSystem.getParameterDescriptionByName(name, out desc);
        id = desc.id;
    }

    private void Update()
    {
        if (playerTransform == null || detect == null || soundSource == null) return;

        CalculateRawOcclusionData();
     
    }

    void CalculateRawOcclusionData()
    {
       
        Vector3 sourcePos = soundSource.position;
        Vector3 playerPos = playerTransform.position;

        float distance = Vector3.Distance(sourcePos, playerPos);
        float distanceFactor = Mathf.Clamp01(1f - (distance / maxDistance));
        distanceFactor = Mathf.Max(distanceFactor, 0.1f);
        float wallFactor = 1f;

        RaycastHit hit;

        if (Physics.Linecast(sourcePos, playerPos, out hit, obstructionLayer))
        {
            string tag = hit.collider.tag;
            float hardness = 0.5f;

            if (detect.GetMaterialInfo(tag, out MaterialDatabase.MaterialData data))
            {
                hardness = data.hardness;
            }
            wallFactor = 1f - (hardness * 0.8f);

            


            Debug.DrawLine(sourcePos, hit.point, Color.red);
        }
        else
        {
            targetFreq = 22000f;
            targetVol = 0f;

            Debug.DrawLine(sourcePos, playerPos, Color.green);
        }
        float finalTransmission = distanceFactor * wallFactor;
        targetFreq = Mathf.Lerp(600f, 22000f, finalTransmission);
        targetVol = Mathf.Lerp(-20f, 0f, finalTransmission);

        UpdateFMODGlobal(targetFreq,targetVol);
    }

    void UpdateFMODGlobal(float freq, float vol)
    {


     
        FMOD.RESULT resultEQ = RuntimeManager.StudioSystem.setParameterByID(occEQParamID, freq);
        FMOD.RESULT resultVol = RuntimeManager.StudioSystem.setParameterByID(occVolParamID, vol);

        if (resultEQ != FMOD.RESULT.OK)
        {
            Debug.LogError($"EQ Par {resultEQ}");
        }

        if (resultVol != FMOD.RESULT.OK)
        {
            Debug.LogError($"Vol Para {resultVol}");
        }
    }
}