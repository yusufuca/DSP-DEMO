using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public abstract class AudioSourceBase : MonoBehaviour
{
    [Header("Config")]
    public AudioSourceLibrary audioLibrary; 

   

    [Header("Portal Scanning")]
    public bool usePortalScanning = true;
    public float scanArcAngle = 90f;
    public int scanResolution = 10;  

    [Header("Runtime Debug Data")]
    public string matchedTag = "None";
    public bool isIndoors = false;
    public float currentDistance;
    public bool isObstructed;
    public string frequencyWinner;

    public bool portalFound = false;
    public Vector3 portalPosition;
    public RoomManager.RoomData roomThroughPortal;

    protected AudioSourceLibrary.AudioProfile currentProfile;
    protected StudioEventEmitter emitter;
    protected Transform playerTransform;
    protected DetectingWall detect;

    protected PARAMETER_ID occVolID, occEQID, panID;

    protected float currentFreq = 22000f;
    protected float currentVol = 0f;
    protected float currentPan = 0f;
    protected float targetFreq = 22000f;
    protected float targetVol = 0f;
    protected float targetPan = 0f;

    private float ceilingCheckTimer = 0f;

    private bool isScannerLocked = false;

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
            if (currentProfile == null) Debug.LogError($"'{name}' kütüphanede bulunamadı! Tag: {myTag}");
        }

        GetFMODParameters();


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

        if (isObstructed && usePortalScanning)
        {
            ScanForPortal();
        }
        else
        {
            portalFound = false;
            isScannerLocked = false;
            roomThroughPortal = null;
        }

        UpdateFMODParameters();

    }
    protected void ScanForPortal()
    {
        Vector3 sourcePos = transform.position + currentProfile.rayOffset;
        Vector3 playerPos = playerTransform.position;
        Vector3 dirToPlayer = (playerPos - sourcePos).normalized;
        float distToPlayer = Vector3.Distance(sourcePos, playerPos);

     
        if (isScannerLocked)
        {
            Vector3 dirToPortal = (portalPosition - sourcePos).normalized;
           
            if (Vector3.Angle(dirToPlayer, dirToPortal) > scanArcAngle / 1.5f)
            {
                isScannerLocked = false;
            }

            else if (Physics.Linecast(sourcePos, portalPosition, currentProfile.obstructionLayer))
            {
                isScannerLocked = false;
            }
            else
            { 
                Debug.DrawLine(sourcePos, portalPosition, Color.green);
                IdentifyRoomThroughPortal();
                return;
            }
        }

   
        float stepAngle = scanArcAngle / scanResolution;
        float startAngle = -scanArcAngle / 2f;

        bool gapFound = false;
        Vector3 bestPoint = Vector3.zero;

        for (int i = 0; i <= scanResolution; i++)
        {
            float currentAngle = startAngle + (stepAngle * i);
        
            Vector3 scanDir = Quaternion.Euler(0, currentAngle, 0) * dirToPlayer;

          
            Debug.DrawRay(sourcePos, scanDir * currentProfile.maxHearingDistance, new Color(0, 0, 1, 0.1f));

     
            if (!Physics.Raycast(sourcePos, scanDir, distToPlayer, currentProfile.obstructionLayer))
            {
                
                bestPoint = sourcePos + (scanDir * (distToPlayer * 0.9f));
                gapFound = true;
                break; 
            }
        }

        if (gapFound)
        {
            portalFound = true;
            portalPosition = bestPoint;
            isScannerLocked = true;

           
            Debug.DrawLine(sourcePos, portalPosition, Color.green);

            IdentifyRoomThroughPortal();
        }
        else
        {
            portalFound = false;
            roomThroughPortal = null;
        }
    }
    void IdentifyRoomThroughPortal()
    {
        if (RoomManager.Instance == null) return;

        if (RoomManager.Instance.TryGetRoomAt(portalPosition, out RoomManager.RoomData room))
        {
            roomThroughPortal = room;
           
            Debug.DrawLine(portalPosition, room.centerPoint, Color.magenta);
        }
        else
        {
            roomThroughPortal = null; 
        }
    }

    void CheckCeiling()
    {
        Vector3 origin = transform.position + currentProfile.rayOffset;
        isIndoors = Physics.Raycast(origin, Vector3.up, 15f, currentProfile.obstructionLayer);
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
    public virtual void OnRoomEnter() 
    {

    }

    public float GetTargetFreq() => targetFreq;
    public float GetCurrentFreq() => currentFreq;
    public float GetCurrentVol() => currentVol;
    public float GetCurrentPan() => currentPan;
}