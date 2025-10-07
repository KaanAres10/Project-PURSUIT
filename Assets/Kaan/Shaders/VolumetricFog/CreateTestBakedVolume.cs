using UnityEngine;
using UnityEditor;

public static class CreateTestBakedVolume
{
    [MenuItem("Volumetrics/Create Test Baked Volume 3D Texture")]
    static void Create()
    {
        const int N = 64; // resolution
        var tex = new Texture3D(N, N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;

        var cols = new Color[N * N * N];
        int i = 0;
        for (int z = 0; z < N; z++)
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++, i++)
        {
            // map [0,1] coords → [-1,1]
            float fx = (x + 0.5f) / N * 2f - 1f;
            float fy = (y + 0.5f) / N * 2f - 1f;
            float fz = (z + 0.5f) / N * 2f - 1f;
            float r = Mathf.Sqrt(fx * fx + fy * fy + fz * fz);

            // soft sphere density falloff
            float dens = Mathf.SmoothStep(1f, 0f, r);
            // some color gradient (like warm light)
            Color c = new Color(1.2f - r, 0.5f + 0.3f * fy, 0.2f + 0.5f * r, dens);
            cols[i] = c;
        }

        tex.SetPixels(cols);
        tex.Apply();

        var path = EditorUtility.SaveFilePanelInProject("Save 3D Texture", "TestBakedVolume", "asset", "Save the baked volume texture");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            Debug.Log("Created test baked volume: " + path);
        }
    }
}