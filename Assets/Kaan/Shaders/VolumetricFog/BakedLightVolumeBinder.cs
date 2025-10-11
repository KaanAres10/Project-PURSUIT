using UnityEngine;

[ExecuteAlways]
public class BakedLightVolumeBinder : MonoBehaviour
{
    public Material fogMaterial;           // your VolumetricFog material
    public Texture3D bakedVolume;
    public Vector3 origin = new Vector3(-5, 0, -5);
    public Vector3 size   = new Vector3(10, 5, 10);
    public bool useBakedVolume = true;

    void OnEnable()  { Apply(); }
    void OnValidate(){ Apply(); }
    void Update()    { Apply(); } // cheap, but you can remove if static

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0,1,1,0.5f); // cyan
        Gizmos.DrawWireCube(origin + 0.5f * size, size);
    }
    void Apply()
    {
        if (!fogMaterial) return;
        fogMaterial.SetTexture("_BakedLightVolume", bakedVolume);
        fogMaterial.SetVector("_BLV_Origin", origin);
        fogMaterial.SetVector("_BLV_Size", size);
        fogMaterial.SetFloat("_UseBakedVolume", useBakedVolume ? 1f : 0f);
    }
}