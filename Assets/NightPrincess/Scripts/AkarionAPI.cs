using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NightPrincess
{
    public class AkarionAPI : MonoBehaviour
    {
        [Header("Akarion Credentials")]
        public string apiKey = "ak_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04";
        public string projectId = "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc";
        public string playerId = "test_mo9xxz0z";

        [Header("Endpoints")]
        public string chatEndpoint = "https://akarion.dev/api/v1/ai/chat";
        public string imageEndpoint = "https://akarion.dev/api/v1/ai/image";

        [Header("Defaults")]
        public int chatMaxTokens = 800;
        public float chatTemperature = 0.85f;
        public float requestTimeout = 60f;

        public delegate void ChatResultHandler(bool ok, string text);
        public delegate void ImageResultHandler(bool ok, Texture2D texture, string info);

        private static AkarionAPI _instance;
        public static AkarionAPI Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = UnityEngine.Object.FindFirstObjectByType<AkarionAPI>();
#else
                    _instance = UnityEngine.Object.FindObjectOfType<AkarionAPI>();
#endif
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        public class ChatMessage
        {
            public string role;
            public string text;
            public string imageDataUri;

            public static ChatMessage System(string text) { return new ChatMessage { role = "system", text = text }; }
            public static ChatMessage User(string text) { return new ChatMessage { role = "user", text = text }; }
            public static ChatMessage Assistant(string text) { return new ChatMessage { role = "assistant", text = text }; }
            public static ChatMessage UserWithImage(string text, string dataUri)
            {
                return new ChatMessage { role = "user", text = text, imageDataUri = dataUri };
            }
        }

        public void SendChat(string model, List<ChatMessage> messages, ChatResultHandler cb)
        {
            StartCoroutine(RunChat(model, messages, cb));
        }

        IEnumerator RunChat(string model, List<ChatMessage> messages, ChatResultHandler cb)
        {
            string body = BuildChatBody(model, messages, false);
            using (UnityWebRequest req = BuildPost(chatEndpoint, body))
            {
                req.timeout = (int)requestTimeout;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (cb != null) cb(false, req.error + "\n" + req.downloadHandler.text);
                    yield break;
                }
                string json = req.downloadHandler.text;
                string content = ExtractChatContent(json);
                if (cb != null) cb(true, content);
            }
        }

        public void GenerateOrEditImage(string model, string prompt, Texture2D sourceImage, ImageResultHandler cb)
        {
            StartCoroutine(RunImage(model, prompt, sourceImage, cb));
        }

        IEnumerator RunImage(string model, string prompt, Texture2D sourceImage, ImageResultHandler cb)
        {
            string dataUri = null;
            if (sourceImage != null)
            {
                byte[] png = sourceImage.EncodeToPNG();
                dataUri = "data:image/png;base64," + Convert.ToBase64String(png);
            }

            string body;
            if (dataUri == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"project_id\":\"").Append(JsonEscape(projectId)).Append("\",");
                sb.Append("\"user_id\":\"").Append(JsonEscape(playerId)).Append("\",");
                sb.Append("\"model\":\"").Append(JsonEscape(model)).Append("\",");
                sb.Append("\"prompt\":\"").Append(JsonEscape(prompt)).Append("\"");
                sb.Append("}");
                body = sb.ToString();
            }
            else
            {
                List<ChatMessage> msgs = new List<ChatMessage>();
                msgs.Add(ChatMessage.UserWithImage(prompt, dataUri));
                body = BuildChatBody(model, msgs, true);
            }

            string url = (dataUri == null) ? imageEndpoint : chatEndpoint;
            using (UnityWebRequest req = BuildPost(url, body))
            {
                req.timeout = (int)requestTimeout;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (cb != null) cb(false, null, req.error + "\n" + req.downloadHandler.text);
                    yield break;
                }
                string json = req.downloadHandler.text;
                string imageRef = ExtractImageReference(json);
                if (string.IsNullOrEmpty(imageRef))
                {
                    if (cb != null) cb(false, null, "No image in response: " + json);
                    yield break;
                }

                Texture2D tex = null;
                if (imageRef.StartsWith("data:"))
                {
                    int comma = imageRef.IndexOf(',');
                    if (comma > 0)
                    {
                        string b64 = imageRef.Substring(comma + 1);
                        byte[] bytes = Convert.FromBase64String(b64);
                        tex = new Texture2D(2, 2);
                        tex.LoadImage(bytes);
                    }
                }
                else
                {
                    using (UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(imageRef))
                    {
                        yield return texReq.SendWebRequest();
                        if (texReq.result == UnityWebRequest.Result.Success)
                            tex = DownloadHandlerTexture.GetContent(texReq);
                        else
                        {
                            if (cb != null) cb(false, null, "Texture download failed: " + texReq.error);
                            yield break;
                        }
                    }
                }

                if (cb != null) cb(true, tex, imageRef);
            }
        }

        UnityWebRequest BuildPost(string url, string body)
        {
            UnityWebRequest req = new UnityWebRequest(url, "POST");
            byte[] payload = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            return req;
        }

        string BuildChatBody(string model, List<ChatMessage> messages, bool wantImageOutput)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"project_id\":\"").Append(JsonEscape(projectId)).Append("\",");
            sb.Append("\"user_id\":\"").Append(JsonEscape(playerId)).Append("\",");
            sb.Append("\"model\":\"").Append(JsonEscape(model)).Append("\",");
            sb.Append("\"max_tokens\":").Append(chatMaxTokens).Append(",");
            sb.Append("\"temperature\":").Append(chatTemperature.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(",");
            if (wantImageOutput)
                sb.Append("\"modalities\":[\"image\",\"text\"],");
            sb.Append("\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(",");
                ChatMessage m = messages[i];
                sb.Append("{\"role\":\"").Append(JsonEscape(m.role)).Append("\",");
                if (!string.IsNullOrEmpty(m.imageDataUri))
                {
                    sb.Append("\"content\":[");
                    sb.Append("{\"type\":\"text\",\"text\":\"").Append(JsonEscape(m.text ?? "")).Append("\"},");
                    sb.Append("{\"type\":\"image_url\",\"image_url\":{\"url\":\"").Append(JsonEscape(m.imageDataUri)).Append("\"}}");
                    sb.Append("]");
                }
                else
                {
                    sb.Append("\"content\":\"").Append(JsonEscape(m.text ?? "")).Append("\"");
                }
                sb.Append("}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        string ExtractChatContent(string json)
        {
            int idx = json.IndexOf("\"content\"");
            if (idx < 0) return json;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return json;
            int p = colon + 1;
            while (p < json.Length && (json[p] == ' ' || json[p] == '\t' || json[p] == '\n' || json[p] == '\r')) p++;
            if (p >= json.Length) return json;
            if (json[p] == '"')
            {
                StringBuilder sb = new StringBuilder();
                p++;
                while (p < json.Length)
                {
                    char c = json[p];
                    if (c == '\\' && p + 1 < json.Length)
                    {
                        char n = json[p + 1];
                        if (n == 'n') sb.Append('\n');
                        else if (n == 'r') sb.Append('\r');
                        else if (n == 't') sb.Append('\t');
                        else if (n == '"') sb.Append('"');
                        else if (n == '\\') sb.Append('\\');
                        else if (n == '/') sb.Append('/');
                        else if (n == 'u' && p + 5 < json.Length)
                        {
                            string hex = json.Substring(p + 2, 4);
                            int code;
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out code))
                                sb.Append((char)code);
                            p += 4;
                        }
                        else sb.Append(n);
                        p += 2;
                        continue;
                    }
                    if (c == '"') break;
                    sb.Append(c);
                    p++;
                }
                return sb.ToString();
            }
            return json;
        }

        string ExtractImageReference(string json)
        {
            string pat = "\"url\"";
            int idx = 0;
            while (idx < json.Length)
            {
                int found = json.IndexOf(pat, idx);
                if (found < 0) break;
                int colon = json.IndexOf(':', found);
                if (colon < 0) break;
                int q = json.IndexOf('"', colon);
                if (q < 0) break;
                int end = q + 1;
                StringBuilder sb = new StringBuilder();
                while (end < json.Length)
                {
                    char c = json[end];
                    if (c == '\\' && end + 1 < json.Length) { sb.Append(json[end + 1]); end += 2; continue; }
                    if (c == '"') break;
                    sb.Append(c);
                    end++;
                }
                string val = sb.ToString();
                if (val.StartsWith("http") || val.StartsWith("data:image"))
                    return val;
                idx = end + 1;
            }
            return null;
        }

        public static string JsonEscape(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
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
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
