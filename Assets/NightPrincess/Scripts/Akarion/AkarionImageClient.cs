using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace NightPrincess.Akarion
{
    public class AkarionImageClient : MonoBehaviour
    {
        [SerializeField] private AkarionConfig config;

        public AkarionConfig Config => config;
        public void SetConfig(AkarionConfig cfg) => config = cfg;

        public void EditImage(string prompt, byte[] referencePng, Action<Texture2D, string> onDone, Action<string> onError = null)
        {
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            if (config == null) { onError?.Invoke("AkarionConfig missing (Resources/AkarionConfig.asset)"); return; }
            StartCoroutine(EditRoutine(prompt, referencePng, onDone, onError));
        }

        private IEnumerator EditRoutine(string prompt, byte[] referencePng, Action<Texture2D, string> onDone, Action<string> onError)
        {
            // Step 1: upload the reference drawing to Akarion media
            string referenceUrl = null;
            yield return UploadMedia(referencePng, "player_drawing.png", "/drawings",
                url => referenceUrl = url,
                err => onError?.Invoke("Upload drawing failed: " + err));

            if (string.IsNullOrEmpty(referenceUrl))
            {
                onError?.Invoke("No reference URL");
                yield break;
            }

            // Step 2: call the image-editing chat endpoint using the uploaded URL
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"project_id\":\"").Append(config.projectId).Append("\",");
            sb.Append("\"user_id\":\"").Append(config.playerId).Append("\",");
            sb.Append("\"model\":\"").Append(config.imageModel).Append("\",");
            sb.Append("\"modalities\":[\"image\",\"text\"],");
            sb.Append("\"messages\":[{");
            sb.Append("\"role\":\"user\",\"content\":[");
            sb.Append("{\"type\":\"text\",\"text\":\"").Append(AkarionAPI.EscapeJson(prompt)).Append("\"},");
            sb.Append("{\"type\":\"image_url\",\"image_url\":{\"url\":\"").Append(AkarionAPI.EscapeJson(referenceUrl)).Append("\"}}");
            sb.Append("]}]}");

            string url = config.baseUrl + "/ai/chat";
            string generatedImageUrl = null;
            yield return AkarionAPI.PostJson(url, sb.ToString(), config,
                resp =>
                {
                    // Try to locate the first URL-like string in the response
                    generatedImageUrl = ExtractFirstImageUrl(resp);
                },
                err => onError?.Invoke("Image edit failed: " + err));

            if (string.IsNullOrEmpty(generatedImageUrl))
            {
                onError?.Invoke("No generated image URL in response");
                yield break;
            }

            Texture2D resultTex = null;
            yield return AkarionAPI.DownloadTexture(generatedImageUrl,
                tex => resultTex = tex,
                err => onError?.Invoke("Download generated image failed: " + err));

            if (resultTex != null)
                onDone?.Invoke(resultTex, generatedImageUrl);
        }

        public IEnumerator UploadMedia(byte[] data, string fileName, string folderPath,
            Action<string> onUrl, Action<string> onError = null)
        {
            string url = config.baseUrl + "/media/upload";
            var form = new WWWForm();
            form.AddBinaryData("file", data, fileName, "image/png");
            if (!string.IsNullOrEmpty(folderPath)) form.AddField("folder_path", folderPath);

            yield return AkarionAPI.PostMultipart(url, form, config,
                resp =>
                {
                    string u = AkarionAPI.ExtractField(resp, "url");
                    if (string.IsNullOrEmpty(u)) u = AkarionAPI.ExtractField(resp, "public_url");
                    if (string.IsNullOrEmpty(u)) u = AkarionAPI.ExtractField(resp, "cdn_url");
                    if (string.IsNullOrEmpty(u)) { onError?.Invoke("No url in upload response: " + resp); return; }
                    onUrl?.Invoke(u);
                },
                onError);
        }

        private static string ExtractFirstImageUrl(string resp)
        {
            if (string.IsNullOrEmpty(resp)) return null;

            // Common shapes: images:[{url:...}] or image:{url:...} or content:[{type:"image_url", image_url:{url:...}}]
            string url = AkarionAPI.ExtractField(resp, "url");
            if (!string.IsNullOrEmpty(url) && LooksLikeImageUrl(url)) return url;

            // Fallback: scan for any https url ending in .png/.jpg/.webp
            int idx = 0;
            while (idx < resp.Length)
            {
                int hit = resp.IndexOf("https://", idx, StringComparison.Ordinal);
                if (hit < 0) break;
                int end = hit;
                while (end < resp.Length && resp[end] != '"' && resp[end] != ' ' && resp[end] != '\\') end++;
                string candidate = resp.Substring(hit, end - hit);
                if (LooksLikeImageUrl(candidate)) return candidate;
                idx = end + 1;
            }
            return null;
        }

        private static bool LooksLikeImageUrl(string u)
        {
            if (string.IsNullOrEmpty(u)) return false;
            var lower = u.ToLowerInvariant();
            return lower.StartsWith("http") && (lower.Contains(".png") || lower.Contains(".jpg") || lower.Contains(".jpeg") || lower.Contains(".webp"));
        }
    }
}
