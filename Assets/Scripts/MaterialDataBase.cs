using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAudioData", menuName = "MyTools/Audio Material Data")]
public class MaterialDatabase : ScriptableObject
{
    [System.Serializable]
    public struct MaterialData
    {
        public string tag;
        public float hardness;
        public float jagness;
    }

    public List<MaterialData> allMaterials;
}
