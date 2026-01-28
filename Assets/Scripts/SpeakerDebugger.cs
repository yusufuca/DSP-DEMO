using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(SelfOcclusion))]
public class SpeakerDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public Vector3 textOffset = new Vector3(0, 2.5f, 0); 
    public float fontSize = 4f;
    public Color textColor = Color.yellow;

    private SelfOcclusion physicsScript;
    private TextMeshPro debugText;
    private GameObject textObj;
    private Transform mainCamera;

    void Start()
    {
        physicsScript = GetComponent<SelfOcclusion>();

       
        CreateDebugTextObject();

        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void CreateDebugTextObject()
    {
        
        textObj = new GameObject("DEBUG_TEXT");
        textObj.transform.SetParent(this.transform);
        textObj.transform.localPosition = textOffset;

        debugText = textObj.AddComponent<TextMeshPro>();

        // Yazı Ayarları
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.fontSize = fontSize;
        debugText.color = textColor;
        debugText.isTextObjectScaleStatic = false;
    }

    void Update()
    {
        if (!showDebug || debugText == null || physicsScript == null)
        {
            if (textObj != null) textObj.SetActive(false);
            return;
        }

        textObj.SetActive(true);

      
        float freq = physicsScript.GetCurrentFreq();
        float vol = physicsScript.GetCurrentVol();
        float pan = physicsScript.GetCurrentPan();
        float targetF = physicsScript.GetTargetFreq();

        string panStr = pan < -0.1f ? "Left" : (pan > 0.1f ? "Right" : "Center");

        debugText.text = string.Format(
            "FREQ: <color=white>{0:0} Hz</color> <size=60%>(T:{1:0})</size>\n" +
            "VOL : <color=white>{2:0.0} dB</color>\n" +
            "PAN : <color=cyan>{3:0.00}</color> ({4})",
            freq, targetF, vol, pan, panStr
        );

       
        if (mainCamera != null)
        {
          
            Vector3 direction = textObj.transform.position - mainCamera.position;

      
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                textObj.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}