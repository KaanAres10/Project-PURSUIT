using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public class BakedLightVolumeBaker : EditorWindow
{
    [MenuItem("Tools/Volumetric Fog/Bake 3D Light Volume")]
    public static void Open() => GetWindow<BakedLightVolumeBaker>("Bake 3D Light Volume");

    // Bounds of the volume in world space
    public Vector3 origin = new Vector3(-5, 0, -5);
    public Vector3 size   = new Vector3(10, 5, 10);

    // Resolution of the 3D texture
    public int resX = 64, resY = 32, resZ = 64;

    public enum ProbeMode { DCOnly, EvaluateUp, EvaluateDown, EvaluateForward, EvaluateCustom }
    public ProbeMode probeMode = ProbeMode.DCOnly;
    public Vector3 customDirection = new Vector3(0, 1, 0);

    // Exposure-ish multiplier
    public float intensity = 1.0f;

    // Asset path
    public string savePath = "Assets/BakedLightVolume.asset";

    void OnGUI()
    {
        EditorGUILayout.LabelField("World Bounds", EditorStyles.boldLabel);
        origin = EditorGUILayout.Vector3Field("Origin (world min)", origin);
        size   = EditorGUILayout.Vector3Field("Size (world)", size);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
        resX = EditorGUILayout.IntSlider("X", resX, 4, 256);
        resY = EditorGUILayout.IntSlider("Y", resY, 4, 256);
        resZ = EditorGUILayout.IntSlider("Z", resZ, 4, 256);

        EditorGUILayout.Space();
        probeMode = (ProbeMode)EditorGUILayout.EnumPopup("Probe Mode", probeMode);
        if (probeMode == ProbeMode.EvaluateCustom)
            customDirection = EditorGUILayout.Vector3Field("Custom Direction", customDirection.normalized);
        intensity = EditorGUILayout.Slider("Intensity", intensity, 0f, 8f);

        EditorGUILayout.Space();
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake 3D Light Volume"))
            Bake();
    }

    static Vector3 DirFor(ProbeMode m, Vector3 custom)
    {
        switch (m)
        {
            case ProbeMode.EvaluateUp:      return Vector3.up;
            case ProbeMode.EvaluateDown:    return Vector3.down;
            case ProbeMode.EvaluateForward: return Vector3.forward;
            case ProbeMode.EvaluateCustom:  return custom.normalized;
            default:                        return Vector3.zero; // DC only
        }
    }

    // === SH evaluation that works on any Unity version (9-coeff L2) ===
    // Unity coefficient layout: coeff index 0..8, color band 0(R)/1(G)/2(B)
    // Basis (same convention Unity uses):
    // 0:  0.282095
    // 1: -0.488603 * y
    // 2:  0.488603 * z
    // 3: -0.488603 * x
    // 4:  1.092548 * x*y
    // 5: -1.092548 * y*z
    // 6:  0.315392 * (3z^2 - 1)
    // 7: -1.092548 * x*z
    // 8:  0.546274 * (x^2 - y^2)
    static Color EvaluateSHDirection(SphericalHarmonicsL2 sh, Vector3 dir)
    {
        dir = dir.normalized;
        float x = dir.x, y = dir.y, z = dir.z;

        float b0 = 0.282095f;
        float b1 = -0.488603f * y;
        float b2 =  0.488603f * z;
        float b3 = -0.488603f * x;
        float b4 =  1.092548f * x * y;
        float b5 = -1.092548f * y * z;
        float b6 =  0.315392f * (3.0f * z * z - 1.0f);
        float b7 = -1.092548f * x * z;
        float b8 =  0.546274f * (x * x - y * y);

        float r =
            sh[0,0] * b0 + sh[0,1] * b1 + sh[0,2] * b2 + sh[0,3] * b3 +
            sh[0,4] * b4 + sh[0,5] * b5 + sh[0,6] * b6 + sh[0,7] * b7 + sh[0,8] * b8;

        float g =
            sh[1,0] * b0 + sh[1,1] * b1 + sh[1,2] * b2 + sh[1,3] * b3 +
            sh[1,4] * b4 + sh[1,5] * b5 + sh[1,6] * b6 + sh[1,7] * b7 + sh[1,8] * b8;

        float b =
            sh[2,0] * b0 + sh[2,1] * b1 + sh[2,2] * b2 + sh[2,3] * b3 +
            sh[2,4] * b4 + sh[2,5] * b5 + sh[2,6] * b6 + sh[2,7] * b7 + sh[2,8] * b8;

        return new Color(r, g, b, 1f);
    }

    void Bake()
    {
        if (resX <= 0 || resY <= 0 || resZ <= 0) { Debug.LogError("Invalid resolution"); return; }
        if (size.x <= 0 || size.y <= 0 || size.z <= 0) { Debug.LogError("Invalid size"); return; }

        // Prepare the texture (recreate to ensure correct dimensions & format)
        var tex = new Texture3D(resX, resY, resZ, TextureFormat.RGBAHalf, /*mipChain*/ true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 0,
            name = Path.GetFileNameWithoutExtension(savePath)
        };

        var cols = new Color[resX * resY * resZ];
        var dir = DirFor(probeMode, customDirection);
        bool useDir = dir != Vector3.zero;

        int idx = 0;
        for (int z = 0; z < resZ; z++)
        {
            float wz = origin.z + size.z * ((z + 0.5f) / resZ);
            for (int y = 0; y < resY; y++)
            {
                float wy = origin.y + size.y * ((y + 0.5f) / resY);
                for (int x = 0; x < resX; x++)
                {
                    float wx = origin.x + size.x * ((x + 0.5f) / resX);
                    var pos = new Vector3(wx, wy, wz);

                    // Interpolate baked Light Probe (GI) at this position
                    var sh = new SphericalHarmonicsL2();
                    LightProbes.GetInterpolatedProbe(pos, null, out sh);

                    Color rgb;
                    if (useDir)
                    {
                        // Directional evaluation via SH basis
                        rgb = EvaluateSHDirection(sh, dir);
                    }
                    else
                    {
                        // DC-only (ambient). DC basis constant:
                        const float c0 = 0.282095f;
                        rgb = new Color(
                            sh[0, 0] * c0,   // R DC
                            sh[1, 0] * c0,   // G DC
                            sh[2, 0] * c0);  // B DC
                    }

                    rgb *= intensity;
                    rgb.r = Mathf.Max(0, rgb.r);
                    rgb.g = Mathf.Max(0, rgb.g);
                    rgb.b = Mathf.Max(0, rgb.b);

                    cols[idx++] = new Color(rgb.r, rgb.g, rgb.b, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);

        // Create/replace asset (Texture3D cannot be resized; recreate instead)
        var dirPath = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(savePath);
        if (existing != null) AssetDatabase.DeleteAsset(savePath);

        AssetDatabase.CreateAsset(tex, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked 3D Light Volume saved to {savePath}  (res: {resX}×{resY}×{resZ})");
    }
}
