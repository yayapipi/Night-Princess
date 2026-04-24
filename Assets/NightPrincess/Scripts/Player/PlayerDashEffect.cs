using UnityEngine;

namespace NightPrincess.Player
{
    [RequireComponent(typeof(LineRenderer))]
    public class PlayerDashEffect : MonoBehaviour
    {
        [SerializeField] private float maxTrailLength = 3.5f;
        [SerializeField] private float trailFadeSpeed = 8f;
        [SerializeField] private float minPointDistance = 0.05f;
        [SerializeField] private Gradient trailGradient;
        [SerializeField] private float startWidth = 0.45f;
        [SerializeField] private float endWidth = 0.0f;

        private LineRenderer line;
        private bool active;
        private float accumulated;
        private Vector3 lastPoint;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            if (trailGradient != null && trailGradient.colorKeys != null && trailGradient.colorKeys.Length > 0)
                line.colorGradient = trailGradient;
            line.positionCount = 0;
            line.enabled = false;

            if (line.sharedMaterial == null)
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void BeginTrail()
        {
            active = true;
            line.enabled = true;
            line.positionCount = 1;
            line.SetPosition(0, transform.position);
            lastPoint = transform.position;
            accumulated = 0f;
        }

        public void EndTrail()
        {
            active = false;
        }

        private void LateUpdate()
        {
            if (active)
            {
                Vector3 p = transform.position;
                if (Vector3.Distance(p, lastPoint) > minPointDistance)
                {
                    accumulated += Vector3.Distance(p, lastPoint);
                    int n = line.positionCount;
                    line.positionCount = n + 1;
                    line.SetPosition(n, p);
                    lastPoint = p;

                    while (accumulated > maxTrailLength && line.positionCount > 2)
                        TrimOldest();
                }
            }
            else if (line.positionCount > 0)
            {
                // Fade out by trimming the oldest points
                float trim = trailFadeSpeed * Time.deltaTime;
                while (trim > 0f && line.positionCount > 1)
                {
                    TrimOldest();
                    trim -= 0.1f;
                }
                if (line.positionCount <= 1)
                {
                    line.positionCount = 0;
                    line.enabled = false;
                }
            }
        }

        private void TrimOldest()
        {
            int count = line.positionCount;
            if (count <= 1) return;
            var pts = new Vector3[count];
            line.GetPositions(pts);
            var trimmed = new Vector3[count - 1];
            System.Array.Copy(pts, 1, trimmed, 0, count - 1);
            line.positionCount = count - 1;
            line.SetPositions(trimmed);
        }
    }
}
