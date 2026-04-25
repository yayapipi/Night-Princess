using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess
{
    public class CreateItemPanelController : MonoBehaviour
    {
        [Header("API")]
        public AkarionAPI api;

        [Header("Models")]
        public string imageModel = "google/gemini-3.1-flash-image-preview";

        [Header("UI Refs")]
        public GameObject panelRoot;
        public RawImage drawTargetImg;
        public TMP_InputField itemNameInputField;
        public Button buildBtn;
        public Button giveBtn;
        public Button closeBtn;
        public GameObject loadingObj;

        [Header("Linked Panels")]
        public DialogPanelController dialogPanel;

        [Header("Drawing")]
        public int textureWidth = 512;
        public int textureHeight = 512;
        public Color backgroundColor = Color.white;
        public Color brushColor = Color.black;
        public int brushRadius = 4;

        [Header("AI Prompt")]
        [TextArea(3, 8)]
        public string editPromptTemplate =
            "Refine and extend this hand-drawn sketch named '{itemName}' into a clean simple-line-art icon (簡筆畫風 / sketch style). Keep the user's original strokes and intent — do NOT redraw from scratch, just enhance and add subtle detail. Pure white background, centered composition, no shadows, no text, no watermark, output PNG.";

        private Texture2D drawTexture;
        private RectTransform drawRect;
        private bool drawing;
        private Vector2 lastTexPoint;
        private bool hasLastPoint;

        private bool isLoading;
        private bool craftedAtLeastOnce;
        private Texture2D currentItemTexture;
        private string lastDrawingMediaUrl;
        private string lastAIMediaUrl;
        private string pendingItemName;

        public bool IsOpen { get { return panelRoot != null && panelRoot.activeSelf; } }

        void OnEnable() { UILock.Push(); }
        void OnDisable() { UILock.Pop(); }

        void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (api == null) api = AkarionAPI.Instance;
            if (drawTargetImg != null) drawRect = drawTargetImg.rectTransform;
        }

        void Start()
        {
            if (buildBtn != null) buildBtn.onClick.AddListener(OnBuildClicked);
            if (giveBtn != null) giveBtn.onClick.AddListener(OnGiveClicked);
            if (closeBtn != null) closeBtn.onClick.AddListener(ClosePanel);

            InitDrawTexture();
            SetGiveInteractable(false);
            SetLoading(false);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void InitDrawTexture()
        {
            drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawTexture.wrapMode = TextureWrapMode.Clamp;
            drawTexture.filterMode = FilterMode.Bilinear;
            ClearDrawTexture();
            if (drawTargetImg != null) drawTargetImg.texture = drawTexture;
        }

        void ClearDrawTexture()
        {
            if (drawTexture == null) return;
            Color[] cols = new Color[textureWidth * textureHeight];
            for (int i = 0; i < cols.Length; i++) cols[i] = backgroundColor;
            drawTexture.SetPixels(cols);
            drawTexture.Apply();
        }

        public void OpenPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ClearDrawTexture();
            craftedAtLeastOnce = false;
            currentItemTexture = null;
            SetGiveInteractable(false);
            if (itemNameInputField != null) itemNameInputField.text = "";
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (isLoading) return;
            if (drawRect == null || drawTexture == null) return;

            if (Input.GetMouseButtonDown(0)) TryStartDraw();
            if (Input.GetMouseButton(0) && drawing) ContinueDraw();
            if (Input.GetMouseButtonUp(0)) EndDraw();
        }

        void TryStartDraw()
        {
            Vector2 tex;
            if (!ScreenToTexture(Input.mousePosition, out tex)) return;
            drawing = true;
            hasLastPoint = false;
            PaintAt(tex);
            lastTexPoint = tex;
            hasLastPoint = true;
            drawTexture.Apply();
        }

        void ContinueDraw()
        {
            Vector2 tex;
            if (!ScreenToTexture(Input.mousePosition, out tex))
            {
                hasLastPoint = false;
                return;
            }
            if (hasLastPoint) PaintLine(lastTexPoint, tex);
            else PaintAt(tex);
            lastTexPoint = tex;
            hasLastPoint = true;
            drawTexture.Apply();
        }

        void EndDraw()
        {
            drawing = false;
            hasLastPoint = false;
        }

        bool ScreenToTexture(Vector2 screen, out Vector2 tex)
        {
            tex = Vector2.zero;
            if (drawRect == null) return false;
            Vector2 local;
            Camera cam = null;
            Canvas canvas = drawRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(drawRect, screen, cam, out local))
                return false;
            Rect r = drawRect.rect;
            if (local.x < r.xMin || local.x > r.xMax || local.y < r.yMin || local.y > r.yMax)
                return false;
            float u = (local.x - r.xMin) / r.width;
            float v = (local.y - r.yMin) / r.height;
            tex = new Vector2(u * textureWidth, v * textureHeight);
            return true;
        }

        void PaintAt(Vector2 tex)
        {
            int cx = Mathf.RoundToInt(tex.x);
            int cy = Mathf.RoundToInt(tex.y);
            int r = Mathf.Max(1, brushRadius);
            int r2 = r * r;
            int xMin = Mathf.Max(0, cx - r);
            int xMax = Mathf.Min(textureWidth - 1, cx + r);
            int yMin = Mathf.Max(0, cy - r);
            int yMax = Mathf.Min(textureHeight - 1, cy + r);
            for (int y = yMin; y <= yMax; y++)
            {
                int dy = y - cy;
                for (int x = xMin; x <= xMax; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2) drawTexture.SetPixel(x, y, brushColor);
                }
            }
        }

        void PaintLine(Vector2 a, Vector2 b)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / Mathf.Max(1, brushRadius * 0.5f)));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 p = Vector2.Lerp(a, b, t);
                PaintAt(p);
            }
        }

        public void OnBuildClicked()
        {
            if (isLoading) return;
            if (api == null) api = AkarionAPI.Instance;
            if (api == null) return;

            string itemName = itemNameInputField != null ? itemNameInputField.text : "";
            if (string.IsNullOrWhiteSpace(itemName)) itemName = "Mystery Item";
            pendingItemName = itemName;
            lastDrawingMediaUrl = null;
            lastAIMediaUrl = null;

            string prompt = editPromptTemplate.Replace("{itemName}", itemName);

            if (drawTexture != null) drawTexture.Apply();

            byte[] drawingPng = drawTexture != null ? drawTexture.EncodeToPNG() : null;
            if (drawingPng != null)
            {
                string fname = SanitizeFileName(itemName) + "_draw_" + System.DateTime.UtcNow.Ticks + ".png";
                api.UploadMedia(drawingPng, fname, "image/png", "/nightprincess/drawings", OnDrawingUploaded);
            }

            SetLoading(true);
            api.GenerateOrEditImage(imageModel, prompt, drawTexture, OnImageResult);
        }

        void OnDrawingUploaded(bool ok, string body)
        {
            if (ok) lastDrawingMediaUrl = AkarionAPI.ExtractFirstStringField(body, "url");
        }

        void OnImageResult(bool ok, Texture2D tex, string info)
        {
            SetLoading(false);
            if (!ok || tex == null)
            {
                Debug.LogWarning("[CreateItemPanel] Image generation failed: " + info);
                return;
            }
            currentItemTexture = tex;
            if (drawTargetImg != null) drawTargetImg.texture = tex;
            craftedAtLeastOnce = true;
            SetGiveInteractable(true);

            byte[] aiPng = tex.EncodeToPNG();
            if (aiPng != null && api != null)
            {
                string fname = SanitizeFileName(pendingItemName) + "_ai_" + System.DateTime.UtcNow.Ticks + ".png";
                api.UploadMedia(aiPng, fname, "image/png", "/nightprincess/ai_items", OnAIUploaded);
            }
            else
            {
                ReportDrawingEvent();
            }
        }

        void OnAIUploaded(bool ok, string body)
        {
            if (ok) lastAIMediaUrl = AkarionAPI.ExtractFirstStringField(body, "url");
            ReportDrawingEvent();
        }

        void ReportDrawingEvent()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.RecordDrawingSubmitted(pendingItemName, lastDrawingMediaUrl, lastAIMediaUrl);
        }

        string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "item";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            string r = sb.ToString();
            return string.IsNullOrEmpty(r) ? "item" : r;
        }

        public void OnGiveClicked()
        {
            if (!craftedAtLeastOnce) return;
            if (dialogPanel == null) return;
            string itemName = itemNameInputField != null ? itemNameInputField.text : "";
            Texture2D toSend = currentItemTexture;
            ClosePanel();
            dialogPanel.SendItemToPrincess(toSend, itemName);
        }

        void SetGiveInteractable(bool v)
        {
            if (giveBtn != null) giveBtn.interactable = v;
        }

        void SetLoading(bool v)
        {
            isLoading = v;
            if (loadingObj != null) loadingObj.SetActive(v);
            if (buildBtn != null) buildBtn.interactable = !v;
            if (closeBtn != null) closeBtn.interactable = !v;
            if (giveBtn != null) giveBtn.interactable = !v && craftedAtLeastOnce;
        }
    }
}
