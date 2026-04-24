using System.Collections.Generic;
using UnityEngine;

namespace NightPrincess.Enemy
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public class EnemyArcDetector : MonoBehaviour
    {
        [Header("Arc Shape")]
        [SerializeField] private float radius = 2.5f;
        [SerializeField, Range(5f, 180f)] private float angleDegrees = 70f;
        [SerializeField, Range(4, 64)] private int segments = 16;
        [SerializeField] private Vector2 localForward = Vector2.right;

        [Header("Runtime Visual")]
        [SerializeField] private bool drawInGame = true;
        [SerializeField] private Color arcColor = new Color(1f, 0.3f, 0.3f, 0.35f);
        [SerializeField] private Material lineMaterialOverride;

        [Header("Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        private PolygonCollider2D poly;
        private LineRenderer runtimeLine;
        private EnemyController owner;

        private void Awake()
        {
            poly = GetComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            owner = GetComponentInParent<EnemyController>();
            BuildShape();

            if (drawInGame) BuildRuntimeVisual();
        }

        private void OnValidate()
        {
            if (poly == null) poly = GetComponent<PolygonCollider2D>();
            if (poly != null) BuildShape();
        }

        private void BuildShape()
        {
            var pts = new List<Vector2>(segments + 2);
            pts.Add(Vector2.zero);
            float halfAngle = angleDegrees * 0.5f;
            float baseAngle = Mathf.Atan2(localForward.y, localForward.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float a = (baseAngle - halfAngle) + angleDegrees * t;
                float r = a * Mathf.Deg2Rad;
                pts.Add(new Vector2(Mathf.Cos(r) * radius, Mathf.Sin(r) * radius));
            }
            poly.pathCount = 1;
            poly.SetPath(0, pts.ToArray());
        }

        private void BuildRuntimeVisual()
        {
            var go = new GameObject("ArcVisual");
            go.transform.SetParent(transform, false);
            runtimeLine = go.AddComponent<LineRenderer>();
            runtimeLine.useWorldSpace = false;
            runtimeLine.loop = true;
            runtimeLine.widthMultiplier = 0.05f;
            runtimeLine.material = lineMaterialOverride != null
                ? lineMaterialOverride
                : new Material(Shader.Find("Sprites/Default"));
            runtimeLine.startColor = arcColor;
            runtimeLine.endColor = arcColor;

            var pts = new List<Vector3>();
            pts.Add(Vector3.zero);
            float halfAngle = angleDegrees * 0.5f;
            float baseAngle = Mathf.Atan2(localForward.y, localForward.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float a = (baseAngle - halfAngle) + angleDegrees * t;
                float r = a * Mathf.Deg2Rad;
                pts.Add(new Vector3(Mathf.Cos(r) * radius, Mathf.Sin(r) * radius, 0f));
            }
            runtimeLine.positionCount = pts.Count;
            runtimeLine.SetPositions(pts.ToArray());
        }

        private void Update()
        {
            if (owner == null) return;
            // Face the arc towards the enemy's facing direction
            var facing = owner.FacingDirection;
            if (facing.sqrMagnitude < 0.001f) return;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            float baseAngle = Mathf.Atan2(localForward.y, localForward.x) * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle - baseAngle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var player = other.GetComponentInParent<NightPrincess.Player.PlayerController>();
            if (player == null || player.IsDashing) return; // let dash resolve in collision instead
            if (owner != null && !owner.IsDead) owner.AttackPlayer(player);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 center = Vector3.zero;
            float halfAngle = angleDegrees * 0.5f;
            float baseAngle = Mathf.Atan2(localForward.y, localForward.x) * Mathf.Rad2Deg;
            Vector3 prevPoint = center;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float a = (baseAngle - halfAngle) + angleDegrees * t;
                float r = a * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(r) * radius, Mathf.Sin(r) * radius, 0f);
                if (i == 0) Gizmos.DrawLine(center, p);
                else Gizmos.DrawLine(prevPoint, p);
                prevPoint = p;
            }
            Gizmos.DrawLine(prevPoint, center);
            Gizmos.matrix = prev;
        }
    }
}
