using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public abstract class AudioSourceBase : MonoBehaviour
{
    [Header("Config")]
    public AudioSourceLibrary audioLibrary; 

    [Header("Debug Info (Read Only)")]
    public string matchedTag = "None";
    public bool isIndoors = false;

    [Header("Runtime Debug Data")]
    public float currentDistance;      
    public bool isObstructed;         
    public string frequencyWinner;

    protected AudioSourceLibrary.AudioProfile currentProfile;

   
    protected StudioEventEmitter emitter;
    protected Transform playerTransform;
    protected DetectingWall detect; 

   


    protected float currentFreq = 22000f;
    protected float currentVol = 0f;
    protected float currentPan = 0f;

    protected float targetFreq = 22000f;
    protected float targetVol = 0f;
    protected float targetPan = 0f;

    private float ceilingCheckTimer = 0f;

    protected virtual void Awake()
    {
       
        if (emitter == null) emitter = GetComponent<StudioEventEmitter>();

        detect = DetectingWall.DetectInstance;
        if (Camera.main != null) playerTransform = Camera.main.transform;

       
        if (audioLibrary != null)
        {
            string myTag = gameObject.tag;
            foreach (var profile in audioLibrary.allSources)
            {
                if (profile.tagID == myTag)
                {
                    currentProfile = profile;
                    matchedTag = myTag;
                    break;
                }
            }
           
        }

  
    }

    protected virtual void Start()
    {
        if (currentProfile == null) return;
        CheckCeiling();
        if (currentProfile.isStatic) OnRoomEnter();
    }

    protected virtual void Update()
    {
        if (currentProfile == null || emitter == null || !emitter.IsPlaying()) return;
        if (playerTransform == null) return;

       
        ceilingCheckTimer += Time.deltaTime;
        if (ceilingCheckTimer >= 0.5f)
        {
            CheckCeiling();
            ceilingCheckTimer = 0f;
        }

        CalculatePhysics();
        UpdateFMODParameters(); 
        
    }

    void CheckCeiling()
    {
        Vector3 origin = transform.position + currentProfile.rayOffset;
        isIndoors = Physics.Raycast(origin, Vector3.up, 15f, currentProfile.obstructionLayer);
    }



    protected virtual void UpdateFMODParameters()
    {
        float speed = Time.deltaTime * currentProfile.occlusionLerpSpeed;

        currentFreq = Mathf.Lerp(currentFreq, targetFreq, speed);
        currentVol = Mathf.Lerp(currentVol, targetVol, speed);
        currentPan = Mathf.Lerp(currentPan, targetPan, speed);

        if (emitter.EventInstance.isValid())
        {
            emitter.EventInstance.setParameterByName("occEQ", currentFreq);
            emitter.EventInstance.setParameterByName("occVol", currentVol);
            emitter.EventInstance.setParameterByName("rawPan", currentPan);
        }
    }


    protected abstract void CalculatePhysics();
    public virtual void OnRoomEnter() { }

    public float GetTargetFreq() => targetFreq;
    public float GetCurrentFreq() => currentFreq;
    public float GetCurrentVol() => currentVol;
    public float GetCurrentPan() => currentPan;
}