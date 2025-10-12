using UnityEngine;

[ExecuteAlways]
public class BakedLightVolumeBinder : MonoBehaviour
{
    [Header("Bindings")]
    public Material fogMaterial;
    public Texture3D bakedVolume;

    [Header("World-space Bounds (editable)")]
    public Vector3 origin = new Vector3(-5, 0, -5); // world min corner
    public Vector3 size   = new Vector3(10, 5, 10); // world size (x,y,z)

    [Header("Options")]
    public bool useBakedVolume = true;
    public bool forceAxisAligned = true;
    public float minSize = 1e-4f;

    // --- change tracking for two-way sync ---
    Vector3 _prevOrigin, _prevSize;
    Vector3 _prevPos, _prevLossyScale;
    bool _pendingInspectorEdit;  // set when OnValidate fired

    void OnEnable()  { Snapshot(); Apply(); }
    void OnValidate(){ _pendingInspectorEdit = true; } // user typed in Inspector

    void Update()
    {
        var t = transform;

        if (forceAxisAligned && t.rotation != Quaternion.identity)
            t.rotation = Quaternion.identity;

        // Detect inspector edits vs transform edits
        bool fieldsChanged =
            _pendingInspectorEdit ||
            origin != _prevOrigin || size != _prevSize;

        bool transformChanged =
            t.position != _prevPos || t.lossyScale != _prevLossyScale;

        if (fieldsChanged && !transformChanged)
        {
            // ---- Inspector drives Transform ----
            size.x = Mathf.Max(minSize, Mathf.Abs(size.x));
            size.y = Mathf.Max(minSize, Mathf.Abs(size.y));
            size.z = Mathf.Max(minSize, Mathf.Abs(size.z));

            Vector3 center = origin + 0.5f * size;
            t.position   = center;
            t.localScale = size; // assume parent scale ~ (1,1,1) for exact match
        }
        else if (transformChanged)
        {
            // ---- Transform gizmo drives fields ----
            Vector3 s = t.lossyScale;
            s.x = Mathf.Max(minSize, Mathf.Abs(s.x));
            s.y = Mathf.Max(minSize, Mathf.Abs(s.y));
            s.z = Mathf.Max(minSize, Mathf.Abs(s.z));

            size   = s;
            origin = t.position - 0.5f * size;
        }

        _pendingInspectorEdit = false;
        Snapshot();
        Apply();
    }

    void Snapshot()
    {
        _prevOrigin = origin;
        _prevSize = size;
        _prevPos = transform.position;
        _prevLossyScale = transform.lossyScale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.6f);
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
