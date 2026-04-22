using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Brush")]
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField] private int brushSize = 3;

    [Header("Canvas Size")]
    [SerializeField] private int texWidth = 256;
    [SerializeField] private int texHeight = 256;

    private RawImage rawImage;
    private Texture2D drawTexture;
    private RectTransform rectTransform;
    private bool isDrawing;
    private Vector2 lastDrawPos;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        // Ensure raycast target is enabled so pointer events work
        rawImage.raycastTarget = true;

        Debug.Log("[DrawingCanvas] Awake - initialized");
    }

    private void OnEnable()
    {
        ClearCanvas();
        Debug.Log("[DrawingCanvas] OnEnable - canvas cleared");
    }

    public void ClearCanvas()
    {
        drawTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        drawTexture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[texWidth * texHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        drawTexture.SetPixels(pixels);
        drawTexture.Apply();

        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        rawImage.texture = drawTexture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrawing = true;
        Debug.Log("[DrawingCanvas] Pointer Down");

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Vector2 texCoord = LocalToTextureCoord(localPoint);
            DrawCircle((int)texCoord.x, (int)texCoord.y);
            lastDrawPos = texCoord;
            drawTexture.Apply();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Vector2 texCoord = LocalToTextureCoord(localPoint);
            DrawLine(lastDrawPos, texCoord);
            lastDrawPos = texCoord;
            drawTexture.Apply();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
    }

    private Vector2 LocalToTextureCoord(Vector2 localPoint)
    {
        Rect rect = rectTransform.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        int x = Mathf.Clamp((int)(normalizedX * texWidth), 0, texWidth - 1);
        int y = Mathf.Clamp((int)(normalizedY * texHeight), 0, texHeight - 1);

        return new Vector2(x, y);
    }

    private void DrawLine(Vector2 from, Vector2 to)
    {
        float dist = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, (int)(dist / 1f));

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            DrawCircle((int)point.x, (int)point.y);
        }
    }

    private void DrawCircle(int cx, int cy)
    {
        int r = brushSize;
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < texWidth && py >= 0 && py < texHeight)
                    {
                        drawTexture.SetPixel(px, py, brushColor);
                    }
                }
            }
        }
    }

    public string GetBase64()
    {
        byte[] pngData = drawTexture.EncodeToPNG();
        return System.Convert.ToBase64String(pngData);
    }

    public void SetGeneratedImage(Texture2D texture)
    {
        if (texture == null) return;

        Texture2D resized = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        resized.filterMode = FilterMode.Point;

        RenderTexture rt = RenderTexture.GetTemporary(texWidth, texHeight);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        resized.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        drawTexture = resized;
        rawImage.texture = drawTexture;
    }
}
