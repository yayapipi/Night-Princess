using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NightPrincess.Akarion
{
    public static class AkarionAPI
    {
        public static string ComputePlayerSig(string playerId, string apiKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(playerId));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static void ApplyAuthHeaders(UnityWebRequest req, AkarionConfig cfg, bool playerMode = true)
        {
            req.SetRequestHeader("Authorization", "Bearer " + cfg.apiKey);
            req.SetRequestHeader("X-Api-Key", cfg.apiKey);
            if (playerMode && !string.IsNullOrEmpty(cfg.playerId))
            {
                req.SetRequestHeader("X-Player-Id", cfg.playerId);
                req.SetRequestHeader("X-Player-Sig", ComputePlayerSig(cfg.playerId, cfg.apiKey));
            }
        }

        public static IEnumerator PostJson(string url, string json, AkarionConfig cfg,
            Action<string> onSuccess, Action<string> onError = null, bool playerMode = false)
        {
            using var req = new UnityWebRequest(url, "POST");
            var body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyAuthHeaders(req, cfg, playerMode);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler.text);
            }
            else
            {
                string msg = $"{req.responseCode} {req.error} :: {req.downloadHandler?.text}";
                Debug.LogWarning($"[Akarion] POST {url} failed: {msg}");
                onError?.Invoke(msg);
            }
        }

        public static IEnumerator PostMultipart(string url, WWWForm form, AkarionConfig cfg,
            Action<string> onSuccess, Action<string> onError = null, bool playerMode = false)
        {
            using var req = UnityWebRequest.Post(url, form);
            ApplyAuthHeaders(req, cfg, playerMode);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler.text);
            }
            else
            {
                string msg = $"{req.responseCode} {req.error} :: {req.downloadHandler?.text}";
                Debug.LogWarning($"[Akarion] UPLOAD {url} failed: {msg}");
                onError?.Invoke(msg);
            }
        }

        public static IEnumerator DownloadTexture(string url, Action<Texture2D> onSuccess, Action<string> onError = null)
        {
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(DownloadHandlerTexture.GetContent(req));
            }
            else
            {
                Debug.LogWarning($"[Akarion] GET {url} failed: {req.error}");
                onError?.Invoke(req.error);
            }
        }

        public static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static string ExtractField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string key = "\"" + fieldName + "\"";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            while (idx >= 0)
            {
                int colon = json.IndexOf(':', idx + key.Length);
                if (colon < 0) return null;
                int i = colon + 1;
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length) return null;
                if (json[i] == '"')
                {
                    var sb = new StringBuilder();
                    i++;
                    while (i < json.Length)
                    {
                        char c = json[i];
                        if (c == '\\' && i + 1 < json.Length)
                        {
                            char n = json[i + 1];
                            switch (n)
                            {
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                case '"': sb.Append('"'); break;
                                case '\\': sb.Append('\\'); break;
                                case '/': sb.Append('/'); break;
                                default: sb.Append(n); break;
                            }
                            i += 2;
                            continue;
                        }
                        if (c == '"') return sb.ToString();
                        sb.Append(c);
                        i++;
                    }
                    return sb.ToString();
                }
                idx = json.IndexOf(key, idx + 1, StringComparison.Ordinal);
            }
            return null;
        }
    }
}
