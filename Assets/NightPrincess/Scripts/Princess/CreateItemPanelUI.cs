using NightPrincess.Akarion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess.Princess
{
    public class CreateItemPanelUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DrawableCanvas drawable;
        [SerializeField] private TMP_InputField itemNameInput;
        [SerializeField] private Button buildBtn;
        [SerializeField] private Button giveBtn;
        [SerializeField] private Button closeBtn;
        [SerializeField] private GameObject loadingObj;

        [Header("Dependencies")]
        [SerializeField] private AkarionImageClient imageClient;
        [SerializeField] private AkarionConfig config;

        [Header("Background (disabled — keep white bg as-is)")]
        [SerializeField] private bool removeWhiteBackground = false;
        [SerializeField, Range(0.5f, 1f)] private float whiteThreshold = 0.92f;

        private DialogPanelUI dialogOwner;
        private bool hasForgedOnce;
        private string lastGeneratedUrl;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private bool initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            if (panelRoot == null) panelRoot = gameObject;
            if (buildBtn != null) buildBtn.onClick.AddListener(OnBuild);
            if (giveBtn != null) giveBtn.onClick.AddListener(OnGive);
            if (closeBtn != null) closeBtn.onClick.AddListener(Close);
            if (loadingObj != null) loadingObj.SetActive(false);
            if (giveBtn != null) giveBtn.interactable = false;
            if (imageClient == null) imageClient = Object.FindFirstObjectByType<AkarionImageClient>(FindObjectsInactive.Include);
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            initialized = true;
        }

        public void Open(DialogPanelUI dialog)
        {
            EnsureInitialized();
            dialogOwner = dialog;
            panelRoot.SetActive(true);
            if (drawable != null) drawable.Clear();
            hasForgedOnce = false;
            if (giveBtn != null) giveBtn.interactable = false;
            if (itemNameInput != null) itemNameInput.text = string.Empty;
        }

        public void Close()
        {
            panelRoot.SetActive(false);
            if (dialogOwner != null) dialogOwner.Open();
        }

        private void OnBuild()
        {
            if (drawable == null || imageClient == null || config == null) return;
            string itemName = itemNameInput != null && !string.IsNullOrWhiteSpace(itemNameInput.text)
                ? itemNameInput.text.Trim()
                : "神秘寶物";

            var png = drawable.EncodeToPng();
            if (png == null) return;

            // Log & backup player drawing (run on imageClient so coroutine isn't paused when panel hides)
            imageClient.StartCoroutine(imageClient.UploadMedia(png, $"player_draw_{System.DateTime.UtcNow.Ticks}.png", "/player_drawings",
                url => AkarionEventLogger.Instance?.LogText(AkarionEvents.PlayerDrawing, "url", url),
                err => Debug.LogWarning("[CreateItem] Upload draft failed: " + err)));

            if (loadingObj != null) loadingObj.SetActive(true);
            if (buildBtn != null) buildBtn.interactable = false;

            string prompt = (config.itemGenerationPrompt ?? string.Empty).Replace("{itemName}", itemName);

            imageClient.EditImage(prompt, png,
                (tex, url) =>
                {
                    if (loadingObj != null) loadingObj.SetActive(false);
                    if (buildBtn != null) buildBtn.interactable = true;

                    Texture2D processed = removeWhiteBackground ? RemoveWhiteBackground(tex, whiteThreshold) : tex;
                    drawable.ApplyTexture(processed);

                    // Backup AI-generated result as well
                    var resultPng = processed.EncodeToPNG();
                    imageClient.StartCoroutine(imageClient.UploadMedia(resultPng, $"ai_item_{System.DateTime.UtcNow.Ticks}.png", "/ai_items",
                        u => AkarionEventLogger.Instance?.LogText(AkarionEvents.ItemForged, "url", u + "|" + itemName),
                        err => Debug.LogWarning("[CreateItem] Upload result failed: " + err)));

                    lastGeneratedUrl = url;
                    hasForgedOnce = true;
                    if (giveBtn != null) giveBtn.interactable = true;
                },
                err =>
                {
                    if (loadingObj != null) loadingObj.SetActive(false);
                    if (buildBtn != null) buildBtn.interactable = true;
                    Debug.LogWarning("[CreateItem] Forge failed: " + err);
                });
        }

        private void OnGive()
        {
            if (!hasForgedOnce || dialogOwner == null) return;
            string itemName = itemNameInput != null && !string.IsNullOrWhiteSpace(itemNameInput.text)
                ? itemNameInput.text.Trim()
                : "神秘寶物";

            panelRoot.SetActive(false);
            dialogOwner.OnGiveBack(itemName, lastGeneratedUrl);
        }

        private static Texture2D RemoveWhiteBackground(Texture2D src, float whiteThreshold)
        {
            if (src == null) return null;
            var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            var px = src.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.r >= whiteThreshold && c.g >= whiteThreshold && c.b >= whiteThreshold)
                    px[i] = new Color(c.r, c.g, c.b, 0f);
            }
            result.SetPixels(px);
            result.Apply();
            return result;
        }
    }
}
