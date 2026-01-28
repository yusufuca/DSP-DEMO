using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class SelfOcclusion : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerTransform;
    public LayerMask obstructionLayer;

    [Header("Raycast Settings")]
    public Vector3 rayOffset = new Vector3(0, 1.5f, 0);

    [Header("Audio Physics")]
    public float occlusionSpeed = 5f;
    public float maxDistance = 50f;

    [Header("Spatializer Settings")]
    public float frontFreq = 22000f; 
    public float backFreq = 10000f;  
    [Range(0f, 1f)] public float minPanStrength = 0.3f; 
    [Range(0f, 1f)] public float maxPanStrength = 1.0f; 



    
    private StudioEventEmitter emitter;
    private DetectingWall detect;

    private float currentFreq = 22000f;
    private float currentVol = 0f;
    private float currentPan = 0f;

    private float targetFreq = 22000f;
    private float targetVol = 0f;
    private float targetPan = 0f;

    private void Start()
    {
        detect = DetectingWall.DetectInstance;

        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

        emitter = GetComponent<StudioEventEmitter>();
    }

    private void Update()
    {
        if (emitter == null || !emitter.IsPlaying()) return;
        if (playerTransform == null) return;

        CalculatePhysics();
        UpdateLocalFMOD();
    }

    void CalculatePhysics()
    {
        Vector3 sourcePos = transform.position + rayOffset;
        Vector3 playerPos = playerTransform.position;
        Vector3 toSource = sourcePos - playerPos;
        float distance = toSource.magnitude;
        Vector3 dirNormalized = toSource.normalized;

       
        float distanceFactor = Mathf.Clamp01(1f - (distance / maxDistance));
        distanceFactor = Mathf.Max(distanceFactor, 0.1f);

        // WallFactor
        float wallFactor = 1f;
        RaycastHit hit;

        if (Physics.Linecast(sourcePos, playerPos, out hit, obstructionLayer))
        {
            string tag = hit.collider.tag;
            float hardness = 0.5f;

            if (detect != null && detect.GetMaterialInfo(tag, out MaterialDatabase.MaterialData data))
            {
                hardness = data.hardness;
            }
            wallFactor = 1f - (hardness * 0.8f);
            Debug.DrawLine(sourcePos, hit.point, Color.red);
        }
        else
        {
            Debug.DrawLine(sourcePos, playerPos, Color.green);
        }

        float finalTransmission = distanceFactor * wallFactor;

       
        float occlusionFreq = Mathf.Lerp(10f, 22000f, finalTransmission);

        // Vol Calc
        targetVol = Mathf.Lerp(-30f, 0f, finalTransmission);


      //Direction Freq

        float forwardDot = Vector3.Dot(playerTransform.forward, dirNormalized);

        float directionFreq = 22000f;
        if (forwardDot < 0)
        {
            
            float fatness = Mathf.Abs(forwardDot); // 0..1
            directionFreq = Mathf.Lerp(frontFreq, backFreq, fatness);
        }


        
        targetFreq = Mathf.Min(occlusionFreq, directionFreq);


      
        float rawPan = Vector3.Dot(playerTransform.right, dirNormalized);

        float currentDistFactor = Mathf.Clamp01(distance / maxDistance);
        float dynamicPanStrength = Mathf.Lerp(minPanStrength, maxPanStrength, currentDistFactor);

        targetPan = rawPan * dynamicPanStrength;
    }

    void UpdateLocalFMOD()
    {
       
        currentFreq = Mathf.Lerp(currentFreq, targetFreq, Time.deltaTime * occlusionSpeed);
        currentVol = Mathf.Lerp(currentVol, targetVol, Time.deltaTime * occlusionSpeed);
        currentPan = Mathf.Lerp(currentPan, targetPan, Time.deltaTime * occlusionSpeed);

        if (emitter.EventInstance.isValid())
        {
            
            emitter.EventInstance.setParameterByName("OccEQ", currentFreq);
            emitter.EventInstance.setParameterByName("OccVol", currentVol);

            
            emitter.EventInstance.setParameterByName("rawPan", currentPan);
        }
    }






    public float GetCurrentFreq() { return currentFreq; }
    public float GetCurrentVol() { return currentVol; }
    public float GetCurrentPan() { return currentPan; }
    public float GetTargetFreq() { return targetFreq; }

}