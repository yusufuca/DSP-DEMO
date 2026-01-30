using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public abstract class AudioSourceBase : MonoBehaviour
{
    [Header("Config")]
    public AudioSourceLibrary audioLibrary;
    public LayerMask obstructionLayer;

    // --- DEBUGGER İÇİN PUBLIC DEĞİŞKENLER ---
    [Header("Runtime Data")]
    public bool isDirectConnection;
    public bool isPortalConnection;
    public Vector3 activePortalPos;
    public float currentDistance;
    public bool isObstructed; // Debugger için eklendi
    public string frequencyWinner = "None"; // Debugger için eklendi
    public string matchedTag = "None"; // Debugger için eklendi
    public bool portalFound = false;
    public Vector3 portalPosition;
    public RoomManager.RoomData roomThroughPortal; // Uyumluluk için

    // Debug Görselleri için (Hata veriyordu)
    public float portalWidth;
    public float portalHeight;

    protected StudioEventEmitter emitter;
    protected Transform playerTransform;
    protected AudioSourceLibrary.AudioProfile currentProfile;

    protected float currentFreq = 22000f, targetFreq = 22000f;
    protected float currentVol = 0f, targetVol = 0f;
    protected float currentPan = 0f, targetPan = 0f;
    protected PARAMETER_ID occVolID, occEQID, panID;

    protected virtual void Awake()
    {
        if (emitter == null) emitter = GetComponent<StudioEventEmitter>();
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
        GetFMODParameters();
    }

    // --- START GERİ EKLENDİ ---
    protected virtual void Start()
    {
        if (currentProfile == null) return;
        CheckDirectObstruction();
        if (currentProfile.isStatic) OnRoomEnter();
    }

    protected virtual void Update()
    {
        if (playerTransform == null || currentProfile == null) return;

        CheckDirectObstruction();
        CalculatePhysics();
        UpdateFMODParameters();
    }

    void CheckDirectObstruction()
    {
        Vector3 offset = (currentProfile != null) ? currentProfile.rayOffset : Vector3.zero;
        Vector3 sourcePos = transform.position + offset;

        Vector3 dirToPlayer = (playerTransform.position - sourcePos).normalized;
        float dist = Vector3.Distance(sourcePos, playerTransform.position);
        currentDistance = dist;

        if (Physics.Raycast(sourcePos, dirToPlayer, dist, obstructionLayer))
        {
            isDirectConnection = false;
            isObstructed = true;
        }
        else
        {
            isDirectConnection = true;
            isObstructed = false;
        }
    }

    void GetFMODParameters()
    {
        if (emitter.EventDescription.isValid())
        {
            emitter.EventDescription.getParameterDescriptionByName("OccEQ", out var d1); occEQID = d1.id;
            emitter.EventDescription.getParameterDescriptionByName("OccVol", out var d2); occVolID = d2.id;
            emitter.EventDescription.getParameterDescriptionByName("StereoPan", out var d3); panID = d3.id;
        }
    }

    protected virtual void UpdateFMODParameters()
    {
        if (currentProfile == null) return;

        float speed = Time.deltaTime * currentProfile.occlusionLerpSpeed;
        currentFreq = Mathf.Lerp(currentFreq, targetFreq, speed);
        currentVol = Mathf.Lerp(currentVol, targetVol, speed);
        currentPan = Mathf.Lerp(currentPan, targetPan, speed);

        if (emitter.EventInstance.isValid())
        {
            emitter.EventInstance.setParameterByID(occEQID, currentFreq);
            emitter.EventInstance.setParameterByID(occVolID, currentVol);
            emitter.EventInstance.setParameterByID(panID, currentPan);
        }
    }

    protected abstract void CalculatePhysics();
    public virtual void OnRoomEnter() { }

    // --- DEBUGGER İÇİN GETTERLAR (EKSİK OLANLAR) ---
    public float GetCurrentFreq() => currentFreq;
    public float GetTargetFreq() => targetFreq;
    public float GetCurrentVol() => currentVol;
    public float GetCurrentPan() => currentPan;
}