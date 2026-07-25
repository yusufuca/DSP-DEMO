using UnityEngine;
using TMPro;
using FMODUnity;

[RequireComponent(typeof(AudioSourceBase))]
public class SpeakerDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public Vector3 textOffset = new Vector3(0, 2.0f, 0);
    public float fontSize = 3.0f;
    public Color textColor = Color.yellow;

    private AudioSourceBase audioSource;
    private SpeakerController speakerController;
    private StudioEventEmitter emitter;

    private TextMeshPro debugText;
    private GameObject textObj;
    private Transform mainCamera;

    void Start()
    {
        audioSource = GetComponent<AudioSourceBase>();
        speakerController = GetComponent<SpeakerController>();
        emitter = GetComponent<StudioEventEmitter>();

        textObj = new GameObject("DEBUG_TEXT");
        textObj.transform.SetParent(this.transform);
        textObj.transform.localPosition = textOffset;

        debugText = textObj.AddComponent<TextMeshPro>();
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.fontSize = fontSize;
        debugText.color = textColor;
        debugText.isTextObjectScaleStatic = false;

        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (!showDebug || debugText == null || audioSource == null)
        {
            if (textObj != null && textObj.activeSelf) textObj.SetActive(false);
            return;
        }

        textObj.SetActive(true);

        // Veriler
        float currentFreq = audioSource.GetCurrentFreq();
        float targetFreq = audioSource.GetTargetFreq(); // Base'e eklediğimiz getter
        float currentVol = audioSource.GetCurrentVol();
        float currentPan = audioSource.GetCurrentPan();
        float dist = audioSource.currentDistance;

        // Winner Bilgileri (SpeakerController'dan)
        string volWin = speakerController != null ? speakerController.volWinner : "-";
        string freqWin = speakerController != null ? speakerController.freqWinner : "-";

        // Hardness
        string roomInfo = "NO ROOM";
        if (speakerController != null && speakerController.myRoom != null)
        {
            roomInfo = $"H:{speakerController.myRoom.hardness:0.0}";
        }

        // Aux
        string auxInfo = "AUX: OFF";
        if (emitter != null && emitter.EventInstance.isValid())
        {
            float aux1;
            emitter.EventInstance.getParameterByName("Aux1Send", out aux1);
            string a1Color = aux1 > 0.9f ? "<color=green>" : "<color=grey>";
            auxInfo = $"AUX1: {a1Color}{aux1:0.00}</color>";
        }

        debugText.text = string.Format(
             "<size=120%>{0}</size>\n" +
             "ROOM: <color=#DDDDDD>{1}</color>\n" +
             "DIST: {2:0.0}m\n" +
             "-----------------\n" +
             "FREQ: {3:0}Hz <color=orange>[{4}]</color>\n" +
             "VOL : {5:0.0}dB <color=orange>[{6}]</color>\n" +
             "PAN : {7:0.00}\n" +
             "{8}",

             audioSource.matchedTag.ToUpper(),
             roomInfo,
             dist,
             currentFreq, freqWin,  // Freq + Winner (REAR/WALL/AIR)
             currentVol, volWin,    // Vol + Winner (DIST/WALL)
             currentPan,
             auxInfo
        );

        if (mainCamera != null)
        {
            Vector3 direction = textObj.transform.position - mainCamera.position;
            direction.y = 0;
            if (direction != Vector3.zero)
                textObj.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}