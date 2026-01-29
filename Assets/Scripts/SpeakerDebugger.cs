using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSourceBase))]
public class SpeakerDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public Vector3 textOffset = new Vector3(0, 2.0f, 0);
    public float fontSize = 3.5f;
    public Color textColor = Color.yellow;

    private AudioSourceBase audioSource;
    private TextMeshPro debugText;
    private GameObject textObj;
    private Transform mainCamera;

    void Start()
    {
        audioSource = GetComponent<AudioSourceBase>();

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

        float currentFreq = audioSource.GetCurrentFreq();
        float targetFreq = audioSource.GetTargetFreq();
        float vol = audioSource.GetCurrentVol();
        float pan = audioSource.GetCurrentPan();
        float dist = audioSource.currentDistance;
        bool blocked = audioSource.isObstructed; 
        string winner = audioSource.frequencyWinner; 
        string tagInfo = audioSource.matchedTag;


        string panStr = "C";
        if (pan < -0.1f) panStr = "L";
        else if (pan > 0.1f) panStr = "R";

        string blockStr = blocked ? "<color=red>BLOCKED</color>" : "<color=green>CLEAR</color>";
        string winnerColor = "<color=white>"; 

        if (winner == "WALL") winnerColor = "<color=#FFAA00>"; 
        else if (winner == "ANGLE") winnerColor = "<color=#00FFFF>"; 
        else if (winner == "DIST") winnerColor = "<color=#AAAAAA>"; 

        string portalInfo = "NO PORTAL";
        if (audioSource.portalFound)
        {
            string roomName = audioSource.roomThroughPortal != null ? audioSource.roomThroughPortal.roomID : "OUTDOOR";
            portalInfo = $"<color=green>PORTAL -> {roomName}</color>";
        }

        debugText.text = string.Format(
                    "<size=120%>{0}</size>\n" +                 
                    "DIST: <color=white>{1:0.0}m</color> | {2}\n" + 
                    "FREQ: {3}<b>{4}</b></color> <size=60%>(now:{5:0})</size>\n" + 
                    "VOL : <color=white>{6:0.0} dB</color>\n" +
                    "{7}\n" +
                    "PAN : <color=green>{7:0.00}</color> ({8})",

                    tagInfo.ToUpper(),
                    dist, blockStr,
                    winnerColor, winner, currentFreq, 
                    vol,
                    portalInfo,
                    pan, panStr
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