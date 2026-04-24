using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightPrincess.Princess
{
    [RequireComponent(typeof(RawImage))]
    public class DrawableCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Canvas")]
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 512;
        [SerializeField] private Color backgroundColor = Color.white;

        [Header("Brush")]
        [SerializeField] private Color brushColor = Color.black;
        [SerializeField, Range(1, 32)] private int brushRadius = 4;

        private RawImage rawImage;
        private RectTransform rect;
        private Texture2D texture;
        private Vector2? lastPoint;

        public Texture2D Texture => texture;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            rect = (RectTransform)transform;
            BuildTexture();
        }

        private void BuildTexture()
        {
            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Clear();
            rawImage.texture = texture;
        }

        public void Clear()
        {
            if (texture == null) return;
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
            texture.SetPixels(pixels);
            texture.Apply();
        }

        public void SetBrushColor(Color c) => brushColor = c;
        public void SetBrushRadius(int r) => brushRadius = Mathf.Clamp(r, 1, 64);

        public void ApplyTexture(Texture2D newTex)
        {
            if (newTex == null) return;
            // Scale new texture into our canvas size while preserving aspect
            var scaled = ScaleTexture(newTex, textureWidth, textureHeight);
            texture.SetPixels(scaled.GetPixels());
            texture.Apply();
            Destroy(scaled);
        }

        public byte[] EncodeToPng() => texture != null ? texture.EncodeToPNG() : null;

        public void OnPointerDown(PointerEventData eventData)
        {
            lastPoint = null;
            DrawAt(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            DrawAt(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            lastPoint = null;
        }

        private void DrawAt(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, eventData.position, eventData.pressEventCamera, out var local)) return;

            Vector2 size = rect.rect.size;
            Vector2 pivot = rect.pivot;
            Vector2 normalized = new Vector2(
                (local.x + size.x * pivot.x) / size.x,
                (local.y + size.y * pivot.y) / size.y);

            Vector2 pixel = new Vector2(normalized.x * textureWidth, normalized.y * textureHeight);
            if (lastPoint.HasValue) DrawLine(lastPoint.Value, pixel);
            else DrawCircle(pixel);
            lastPoint = pixel;
            texture.Apply();
        }

        private void DrawCircle(Vector2 center)
        {
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            int r = brushRadius;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y > r * r) continue;
                    int px = cx + x;
                    int py = cy + y;
                    if (px < 0 || py < 0 || px >= textureWidth || py >= textureHeight) continue;
                    texture.SetPixel(px, py, brushColor);
                }
            }
        }

        private void DrawLine(Vector2 a, Vector2 b)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawCircle(Vector2.Lerp(a, b, t));
            }
        }

        private static Texture2D ScaleTexture(Texture2D source, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            result.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}
