using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ConeLightProbePlacer : EditorWindow
{
    // --- Source (optional) ---
    public Light referenceSpot;

    // --- Cone params ---
    public Vector3 apexPosition = Vector3.zero;
    public Vector3 direction    = Vector3.down;
    [Range(0.1f, 89f)] public float halfAngleDeg = 20f;
    [Min(0.05f)]       public float length       = 10f;

    // --- Density ---
    [Min(0.02f)] public float axialSpacing  = 0.25f;
    [Min(0.02f)] public float circumSpacing = 0.25f;
    [Range(1,64)] public int  minPerRing    = 6;

    // --- Anti-degenerate tricks ---
    [Header("Anti-Degenerate Settings")]
    [Tooltip("Rotate each ring by this many degrees (prevents vertical alignment).")]
    public float ringRotatePerRingDeg = 7.5f;

    [Tooltip("Add a small vertical offset zig-zag between neighbour rings.")]
    [Range(0f, 0.25f)] public float verticalStagger = 0.05f;

    [Tooltip("Tiny random jitter (meters) to break perfect symmetry.")]
    [Range(0f, 0.05f)] public float jitterRadius = 0.01f;

    [Tooltip("Remove points closer than this distance (meters).")]
    [Range(0.0f, 0.2f)] public float minSeparation = 0.02f;

    [Header("Scaffold (convex hull cage)")]
    public bool  addScaffoldBox = true;
    [Tooltip("Extra margin around the cone for the scaffold box (meters).")]
    public float scaffoldMargin = 1.0f;
    [Tooltip("Spacing of scaffold lattice points on the box (meters).")]
    public float scaffoldStep   = 2.0f;

    [Header("Apex/Rings")]
    public bool  addApexPoint    = true;
    [Range(0, 6)] public int extraApexRings = 1;
    public bool  jitterInRingPlane = true;

    // --- Placement ---
    public LightProbeGroup targetGroup;
    public bool overwriteGroup = false;
    public int  maxProbes = 100000;

    [MenuItem("Tools/Volumetric Fog/Create Cone Light Probes (Safe)")]
    public static void Open() => GetWindow<ConeLightProbePlacer>("Cone Light Probes (Safe)");

    void OnGUI()
    {
        GUILayout.Label("Source (Optional)", EditorStyles.boldLabel);
        referenceSpot = (Light)EditorGUILayout.ObjectField("Reference Spot Light", referenceSpot, typeof(Light), true);
        if (referenceSpot && referenceSpot.type == LightType.Spot)
        {
            if (GUILayout.Button("Read From Spot Light"))
            {
                apexPosition = referenceSpot.transform.position;
                direction    = referenceSpot.transform.forward;
                halfAngleDeg = referenceSpot.spotAngle * 0.5f;
                length       = Mathf.Max(0.05f, referenceSpot.range);
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("Cone Parameters", EditorStyles.boldLabel);
        apexPosition = EditorGUILayout.Vector3Field("Apex Position", apexPosition);
        direction    = EditorGUILayout.Vector3Field("Direction", direction);
        halfAngleDeg = EditorGUILayout.Slider("Half Angle (deg)", halfAngleDeg, 0.1f, 89f);
        length       = EditorGUILayout.FloatField("Length (m)", length);

        EditorGUILayout.Space();
        GUILayout.Label("Density", EditorStyles.boldLabel);
        axialSpacing  = EditorGUILayout.FloatField("Axial Spacing (m)", axialSpacing);
        circumSpacing = EditorGUILayout.FloatField("Circumferential Spacing (m)", circumSpacing);
        minPerRing    = EditorGUILayout.IntSlider("Min Per Ring", minPerRing, 1, 64);

        EditorGUILayout.Space();
        GUILayout.Label("Anti-Degenerate", EditorStyles.boldLabel);
        ringRotatePerRingDeg = EditorGUILayout.FloatField("Ring Rotate / Ring (deg)", ringRotatePerRingDeg);
        verticalStagger      = EditorGUILayout.Slider("Vertical Stagger (m)", verticalStagger, 0f, 0.25f);
        jitterRadius         = EditorGUILayout.Slider("Random Jitter (m)", jitterRadius, 0f, 0.05f);
        minSeparation        = EditorGUILayout.Slider("Min Separation (m)", minSeparation, 0f, 0.2f);

        EditorGUILayout.Space();
        GUILayout.Label("Scaffold (Hull Cage)", EditorStyles.boldLabel);
        addScaffoldBox = EditorGUILayout.Toggle("Add Scaffold Box", addScaffoldBox);
        using (new EditorGUI.DisabledScope(!addScaffoldBox))
        {
            scaffoldMargin = EditorGUILayout.FloatField("Margin (m)", scaffoldMargin);
            scaffoldStep   = EditorGUILayout.FloatField("Step (m)", scaffoldStep);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Apex / Rings", EditorStyles.boldLabel);
        addApexPoint    = EditorGUILayout.Toggle("Add Apex Point", addApexPoint);
        extraApexRings  = EditorGUILayout.IntSlider("Extra Apex Rings", extraApexRings, 0, 6);
        jitterInRingPlane = EditorGUILayout.Toggle("Ring-plane Jitter", jitterInRingPlane);

        EditorGUILayout.Space();
        GUILayout.Label("Placement", EditorStyles.boldLabel);
        targetGroup     = (LightProbeGroup)EditorGUILayout.ObjectField("Target LightProbeGroup", targetGroup, typeof(LightProbeGroup), true);
        overwriteGroup  = EditorGUILayout.Toggle("Overwrite Group", overwriteGroup);
        maxProbes       = EditorGUILayout.IntField("Max Probes", maxProbes);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create / Pick Group")) EnsureOrPickGroup();
            using (new EditorGUI.DisabledScope(targetGroup == null))
            {
                if (GUILayout.Button("Generate Cone"))
                    Generate();
                if (GUILayout.Button("Clear Group"))
                    ClearGroup();
            }
        }
    }

    void EnsureOrPickGroup()
    {
        if (targetGroup) return;
        var existing = Object.FindObjectOfType<LightProbeGroup>();
        if (existing) { targetGroup = existing; return; }
        var go = new GameObject("LightProbeGroup (Safe Cone)");
        Undo.RegisterCreatedObjectUndo(go, "Create LightProbeGroup");
        targetGroup = go.AddComponent<LightProbeGroup>();
        Selection.activeGameObject = go;
    }

    void ClearGroup()
    {
        if (!targetGroup) return;
        Undo.RecordObject(targetGroup, "Clear Light Probes");
        targetGroup.probePositions = new Vector3[0];
        EditorUtility.SetDirty(targetGroup);
    }

    void Generate()
    {
        if (!targetGroup)
        {
            EditorUtility.DisplayDialog("No LightProbeGroup", "Assign or create a LightProbeGroup.", "OK");
            return;
        }
        if (halfAngleDeg <= 0f || length <= 0f || axialSpacing <= 0f || circumSpacing <= 0f)
        {
            EditorUtility.DisplayDialog("Invalid Settings", "Check angle, length, and spacing.", "OK");
            return;
        }

        var dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        OrthonormalBasis(dir, out var right, out var up);

        var pointsW = new List<Vector3>(2048);

        // 1) Apex & extra tight rings
        if (addApexPoint) pointsW.Add(apexPosition);

        int extra = Mathf.Max(0, extraApexRings);
        float apexStep = Mathf.Min(axialSpacing * 0.5f, 0.1f);
        for (int e = 1; e <= extra; e++)
        {
            float d  = e * apexStep;
            float r  = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad) * d;
            int   n  = Mathf.Max(minPerRing, Mathf.CeilToInt((2f * Mathf.PI * r) / Mathf.Max(1e-4f, circumSpacing)));
            float rot = e * ringRotatePerRingDeg;
            Vector3 c = apexPosition + dir * (d + ((e & 1) == 1 ? verticalStagger : -verticalStagger) * 0.5f);
            PlaceRing(pointsW, c, right, up, r, n, rot, jitterInRingPlane ? jitterRadius : 0f);
        }

        // 2) Main rings with rotation + vertical stagger
        int ringCount = Mathf.Max(1, Mathf.CeilToInt(length / axialSpacing));
        for (int i = 1; i <= ringCount; i++)
        {
            float d  = Mathf.Min(i * axialSpacing, length);
            float r  = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad) * d;
            int   n  = Mathf.Max(minPerRing, Mathf.CeilToInt((2f * Mathf.PI * r) / Mathf.Max(1e-4f, circumSpacing)));

            float rot = i * ringRotatePerRingDeg;
            float stagger = ((i & 1) == 1 ? verticalStagger : -verticalStagger);
            Vector3 c = apexPosition + dir * d + up * stagger;

            PlaceRing(pointsW, c, right, up, r, n, rot, jitterInRingPlane ? jitterRadius : 0f);
        }

        // 3) Add a scaffold cage (loose box around the cone)
        if (addScaffoldBox)
            AddScaffoldBox(pointsW, apexPosition, dir, halfAngleDeg, length, scaffoldMargin, scaffoldStep, right, up);

        // 4) Deduplicate / min separation
        if (minSeparation > 0f)
            PointsMinSeparation(pointsW, minSeparation);

        // 5) Safety guard
        if (pointsW.Count > maxProbes)
        {
            if (!EditorUtility.DisplayDialog(
                "Too Many Probes",
                $"You are about to place {pointsW.Count} probes (> {maxProbes}). Proceed?",
                "Place All", "Cancel")) return;
        }

        // 6) Write to group (world -> local)
        var t = targetGroup.transform;
        Vector3[] local = overwriteGroup ? new Vector3[pointsW.Count]
                                         : MergeWithExisting(targetGroup, pointsW, t);

        if (overwriteGroup)
            for (int i = 0; i < pointsW.Count; i++)
                local[i] = t.InverseTransformPoint(pointsW[i]);

        Undo.RecordObject(targetGroup, "Place Safe Cone Light Probes");
        targetGroup.probePositions = local;
        EditorUtility.SetDirty(targetGroup);

        Debug.Log($"Placed {pointsW.Count} safe cone probes into '{targetGroup.name}'.");
    }

    static Vector3[] MergeWithExisting(LightProbeGroup group, List<Vector3> newWorld, Transform t)
    {
        var existing = group.probePositions ?? new Vector3[0];
        var merged = new List<Vector3>(existing.Length + newWorld.Count);
        merged.AddRange(existing);
        foreach (var pw in newWorld) merged.Add(t.InverseTransformPoint(pw));
        return merged.ToArray();
    }

    static void PlaceRing(List<Vector3> outList, Vector3 center, Vector3 right, Vector3 up, float radius, int count, float rotateDeg, float jitter)
    {
        if (radius <= 0f || count <= 0) return;

        float rot = rotateDeg * Mathf.Deg2Rad;
        float twoPi = Mathf.PI * 2f;

        for (int k = 0; k < count; k++)
        {
            float a = rot + (k / (float)count) * twoPi;
            Vector3 onCircle = Mathf.Cos(a) * right + Mathf.Sin(a) * up;
            Vector3 p = center + onCircle * radius;

            if (jitter > 0f)
            {
                // radial jitter in ring plane
                Vector2 j = Random.insideUnitCircle * jitter;
                p += right * j.x + up * j.y;
            }
            outList.Add(p);
        }
    }

    static void OrthonormalBasis(Vector3 n, out Vector3 r, out Vector3 u)
    {
        if (Mathf.Abs(n.y) < 0.999f)
            r = Vector3.Normalize(Vector3.Cross(n, Vector3.up));
        else
            r = Vector3.Normalize(Vector3.Cross(n, Vector3.right));
        u = Vector3.Normalize(Vector3.Cross(r, n));
    }

    static void AddScaffoldBox(List<Vector3> outList, Vector3 apex, Vector3 dir, float halfAngleDeg, float length, float margin, float step, Vector3 right, Vector3 up)
    {
        // Compute a world-aligned box that contains the whole cone + margin.
        float maxR = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad) * length;
        // Make a box centered roughly at mid-depth of the cone
        Vector3 center = apex + dir * (length * 0.5f);
        // Build a basis to cover the cone cross section (right, up, dir)
        Vector3 extentRight = right * (maxR + margin);
        Vector3 extentUp    = up    * (maxR + margin);
        Vector3 extentFwd   = dir   * (length * 0.5f + margin);

        // Now fill the 6 faces of the oriented box with lattice points
        // We only place points on faces (not interior) to give the solver a convex hull “cage”.
        Vector3[] faceNormals = { right, -right, up, -up, dir, -dir };
        Vector2Int stepsRU = new Vector2Int(Mathf.Max(2, Mathf.CeilToInt((2f*(maxR+margin))/Mathf.Max(0.05f, step))),
                                            Mathf.Max(2, Mathf.CeilToInt((2f*(maxR+margin))/Mathf.Max(0.05f, step))));
        int stepsF = Mathf.Max(2, Mathf.CeilToInt(((length+2f*margin)/Mathf.Max(0.05f, step))));

        // Helper to emit a face grid
        void EmitFace(Vector3 centerFace, Vector3 axisA, Vector3 axisB, int countA, int countB)
        {
            for (int ia = 0; ia < countA; ia++)
            for (int ib = 0; ib < countB; ib++)
            {
                float ta = (countA == 1) ? 0.5f : ia / (float)(countA - 1);
                float tb = (countB == 1) ? 0.5f : ib / (float)(countB - 1);
                Vector3 p = centerFace + (ta - 0.5f) * 2f * axisA + (tb - 0.5f) * 2f * axisB;
                outList.Add(p);
            }
        }

        // +X / -X faces (right / left)
        EmitFace(center + extentRight, up, extentFwd, stepsRU.y, stepsF);
        EmitFace(center - extentRight, up, extentFwd, stepsRU.y, stepsF);
        // +Y / -Y faces (up / down)
        EmitFace(center + extentUp, right, extentFwd, stepsRU.x, stepsF);
        EmitFace(center - extentUp, right, extentFwd, stepsRU.x, stepsF);
        // +Z / -Z faces (forward / back)
        EmitFace(center + extentFwd, right, up, stepsRU.x, stepsRU.y);
        EmitFace(center - extentFwd, right, up, stepsRU.x, stepsRU.y);
    }

    static void PointsMinSeparation(List<Vector3> pts, float minDist)
    {
        if (pts.Count <= 1 || minDist <= 0f) return;
        float minSqr = minDist * minDist;
        var keep = new List<Vector3>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 p = pts[i];
            bool tooClose = false;
            for (int k = 0; k < keep.Count; k++)
            {
                if ((keep[k] - p).sqrMagnitude < minSqr) { tooClose = true; break; }
            }
            if (!tooClose) keep.Add(p);
        }
        pts.Clear();
        pts.AddRange(keep);
    }
}
