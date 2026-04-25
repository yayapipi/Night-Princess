using UnityEngine;

namespace NightPrincess
{
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ArcDetector : MonoBehaviour
{
    [Header("Arc Shape")]
    public float radius = 3f;
    [Range(10f, 350f)] public float angleDegrees = 90f;
    [Range(3, 64)] public int segments = 24;

    [Header("Visual")]
    public Color arcColor = new Color(1f, 0.25f, 0.25f, 0.4f);
    public string sortingLayerName = "Default";
    public int sortingOrder = 32767;

    [Header("Detection")]
    public EnemyController owner;
    public string playerTag = "Player";
    public LayerMask detectionMask = ~0;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    private Mesh arcMesh;
    private float lastRadius;
    private float lastAngle;
    private int lastSegments;
    private Color lastColor;

    void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (owner == null) owner = GetComponentInParent<EnemyController>();
        EnsureMaterial();
        Rebuild();
        ApplySorting();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        EnsureMaterial();
        Rebuild();
        ApplySorting();
    }

    void Update()
    {
        if (radius != lastRadius || angleDegrees != lastAngle || segments != lastSegments || arcColor != lastColor)
            Rebuild();

        if (owner != null)
        {
            Vector2 facing = owner.GetFacing();
            if (facing.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        if (Application.isPlaying) DetectPlayer();
    }

    void EnsureMaterial()
    {
        if (meshRenderer == null) return;
        if (runtimeMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            runtimeMaterial = new Material(sh);
            runtimeMaterial.hideFlags = HideFlags.DontSave;
        }
        runtimeMaterial.color = arcColor;
        meshRenderer.sharedMaterial = runtimeMaterial;
    }

    void ApplySorting()
    {
        if (meshRenderer == null) return;
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    void Rebuild()
    {
        if (meshFilter == null) return;
        if (arcMesh == null)
        {
            arcMesh = new Mesh { name = "ArcMesh" };
            arcMesh.hideFlags = HideFlags.DontSave;
        }
        arcMesh.Clear();

        int segs = Mathf.Max(3, segments);
        Vector3[] verts = new Vector3[segs + 2];
        int[] tris = new int[segs * 3];
        Color[] colors = new Color[verts.Length];

        verts[0] = Vector3.zero;
        colors[0] = arcColor;

        float half = angleDegrees * 0.5f;
        for (int i = 0; i <= segs; i++)
        {
            float t = (float)i / segs;
            float a = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            colors[i + 1] = arcColor;
        }
        for (int i = 0; i < segs; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        arcMesh.vertices = verts;
        arcMesh.triangles = tris;
        arcMesh.colors = colors;
        arcMesh.RecalculateBounds();
        meshFilter.sharedMesh = arcMesh;

        if (runtimeMaterial != null) runtimeMaterial.color = arcColor;

        lastRadius = radius;
        lastAngle = angleDegrees;
        lastSegments = segments;
        lastColor = arcColor;
    }

    void DetectPlayer()
    {
        if (owner == null || owner.IsDead) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, detectionMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag(playerTag)) continue;
            Vector2 toTarget = (Vector2)hits[i].transform.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude < 0.0001f) continue;
            float a = Vector2.Angle((Vector2)transform.right, toTarget);
            if (a <= angleDegrees * 0.5f)
            {
                owner.OnPlayerInArc(hits[i].transform);
                return;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = arcColor;
        Vector3 origin = transform.position;
        int segs = Mathf.Max(3, segments);
        float half = angleDegrees * 0.5f;
        Vector3 prev = origin;
        for (int i = 0; i <= segs; i++)
        {
            float t = (float)i / segs;
            float a = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            Vector3 world = transform.TransformPoint(dir);
            if (i == 0) prev = world;
            Gizmos.DrawLine(origin, world);
            if (i > 0) Gizmos.DrawLine(prev, world);
            prev = world;
        }
    }
}
}
