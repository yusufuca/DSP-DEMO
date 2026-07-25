using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public abstract class AudioSourceBase : MonoBehaviour
{
    [Header("Config")]
    public AudioSourceLibrary audioLibrary;
    public LayerMask obstructionLayer;

    [Header("Runtime Data")]
    public bool isDirectConnection;
    public float currentDistance;
    public bool isObstructed;
    public string frequencyWinner = "None";
    public string matchedTag = "None";
    public bool portalFound = false; // SpeakerController kullanıyor
    public Vector3 portalPosition;

    protected StudioEventEmitter emitter;
    protected Transform playerTransform;
    protected AudioSourceLibrary.AudioProfile currentProfile;

    // Hedef Değerler
    protected float currentFreq = 22000f, targetFreq = 22000f;
    protected float currentVol = 0f, targetVol = 0f;
    protected float currentPan = 0f, targetPan = 0f;

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
    }

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
        CalculatePhysics();     // Hedefleri hesapla
        UpdateFMODParameters(); // FMOD'a gönder
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

    // --- ID YOK, SADECE İSİM VAR ---
    protected virtual void UpdateFMODParameters()
    {
        if (currentProfile == null) return;

        // Lerp (Yumuşak Geçiş)
        float speed = Time.deltaTime * currentProfile.occlusionLerpSpeed;
        currentFreq = Mathf.Lerp(currentFreq, targetFreq, speed);
        currentVol = Mathf.Lerp(currentVol, targetVol, speed);
        currentPan = Mathf.Lerp(currentPan, targetPan, speed);

        if (emitter.EventInstance.isValid())
        {
            // HATA VERSE BİLE DİĞERİNE GEÇ (Try-Catch mantığı gibi tek tek yolla)

            // 1. HighCut
            emitter.EventInstance.setParameterByName("OccEQ", currentFreq);

            // 2. Volume
            emitter.EventInstance.setParameterByName("OccVol", currentVol);

            // 3. Pan
            emitter.EventInstance.setParameterByName("rawPan", currentPan);
        }
    }

    protected abstract void CalculatePhysics();
    public virtual void OnRoomEnter() { }

    // Debugger için Getterlar
    public float GetTargetFreq() => targetFreq;

    public float GetCurrentFreq() => currentFreq;
    public float GetCurrentVol() => currentVol;
    public float GetCurrentPan() => currentPan;
}