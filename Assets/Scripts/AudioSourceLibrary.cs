using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlobalAudioLibrary", menuName = "MyTools/Audio Source Library")]
public class AudioSourceLibrary : ScriptableObject
{
    [System.Serializable]
    public class AudioProfile
    {
        [Header("Identity")]
        public string tagID; 
        public bool isStatic;

        [Header("Raycast Settings")]
        public Vector3 rayOffset = new Vector3(0, 1.5f, 0); 
        public LayerMask obstructionLayer; 

        [Header("Occlusion Settings")]
        public float maxHearingDistance = 50f;
        public float occlusionLerpSpeed = 10f;

        [Header("Frequencies")]
        public float openFreq = 22000f;   
        public float closedFreq = 600f;   
        public float openVol = 0f;
        public float closedVol = -30f;

        [Header("Frequencies (Directional)")]
        public float frontFreq = 22000f; 
        public float backFreq = 10000f;  
    
    }

    public List<AudioProfile> allSources = new List<AudioProfile>();
}