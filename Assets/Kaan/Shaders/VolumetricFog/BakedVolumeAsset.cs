using UnityEngine;

[CreateAssetMenu(menuName = "Volumetrics/Baked Volume Asset")]
public class BakedVolumeAsset : ScriptableObject
{
    [Header("Baked data (from the baking project)")]
    public Texture3D volumeTex;           // RGBA (RGB = lighting color, A = density)

    [Header("Baked volume bounds (world space)")]
    public Vector3 volumeCenterWS = Vector3.zero;
    public Vector3 volumeSizeWS   = Vector3.one;

    [Header("Runtime controls")]
    [Range(0.0f, 4.0f)] public float densityScale = 1.0f; // scales baked A
    [Range(0.0f, 1.0f)] public float bakedBlend   = 1.0f; // how much baked RGB contributes
}