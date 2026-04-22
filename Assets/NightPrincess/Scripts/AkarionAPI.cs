using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AkarionAPI : MonoBehaviour
{
    public static AkarionAPI Instance { get; private set; }

    private const string BaseUrl = "https://akarion.dev/api/v1";
    private const string ApiKey = "ak_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04";
    private const string ProjectId = "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc";
    private const string PlayerId = "test_mo9xxz0z";

    private List<MessageEntry> conversationHistory = new List<MessageEntry>();

    private struct MessageEntry
    {
        public string role;
        public string content;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void ResetConversation()
    {
        conversationHistory.Clear();
        conversationHistory.Add(new MessageEntry
        {
            role = "system",
            content = "你是一位高傲但內心善良的公主，名叫艾琳。你住在城堡裡，偶爾會有冒險者來拜訪你。" +
                      "你說話帶有皇室風範，但也會展現可愛的一面。回答請簡短，控制在50字以內。" +
                      "如果冒險者送你禮物，根據禮物的品質和心意來決定你的反應。"
        });
    }

    public void SendChat(string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        if (conversationHistory.Count == 0)
            ResetConversation();

        conversationHistory.Add(new MessageEntry { role = "user", content = userMessage });
        StartCoroutine(SendChatCoroutine(onSuccess, onError));
    }

    public void SendTreasureReaction(string itemName, Action<string> onSuccess, Action<string> onError)
    {
        string message = $"冒險者送了你一個親手鍛造的寶物：「{itemName}」。請根據這個禮物的名稱表達你的感受，喜歡或不喜歡都可以。";
        SendChat(message, onSuccess, onError);
    }

    private IEnumerator SendChatCoroutine(Action<string> onSuccess, Action<string> onError)
    {
        // Build messages array manually
        var sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < conversationHistory.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("{\"role\":\"").Append(conversationHistory[i].role)
              .Append("\",\"content\":\"").Append(EscapeJson(conversationHistory[i].content))
              .Append("\"}");
        }
        sb.Append("]");

        string json = "{" +
            $"\"project_id\":\"{ProjectId}\"," +
            $"\"user_id\":\"{PlayerId}\"," +
            $"\"model\":\"google/gemini-2.5-flash\"," +
            $"\"messages\":{sb}," +
            $"\"max_tokens\":200," +
            $"\"temperature\":0.8" +
            "}";

        Debug.Log($"[AkarionAPI] Chat request: {json}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{BaseUrl}/ai/chat", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {ApiKey}");
            www.timeout = 30;

            yield return www.SendWebRequest();

            string responseText = www.downloadHandler?.text ?? "";
            Debug.Log($"[AkarionAPI] Chat response ({www.responseCode}): {responseText}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AkarionAPI] Chat HTTP error: {www.error} | Response: {responseText}");
                // Remove the failed user message from history
                if (conversationHistory.Count > 0)
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                onError?.Invoke($"HTTP {www.responseCode}: {www.error}");
                yield break;
            }

            // Parse response - extract content from choices[0].message.content
            string reply = ExtractChatReply(responseText);
            if (!string.IsNullOrEmpty(reply))
            {
                conversationHistory.Add(new MessageEntry { role = "assistant", content = reply });
                onSuccess?.Invoke(reply);
            }
            else
            {
                onError?.Invoke("無法解析 AI 回應");
            }
        }
    }

    private string ExtractChatReply(string json)
    {
        // Parse choices[0].message.content from response
        // Using simple string parsing since JsonUtility can't handle nested arrays well
        try
        {
            int choicesIdx = json.IndexOf("\"choices\"");
            if (choicesIdx < 0) return null;

            int contentIdx = json.IndexOf("\"content\"", choicesIdx);
            if (contentIdx < 0) return null;

            int colonIdx = json.IndexOf(":", contentIdx);
            if (colonIdx < 0) return null;

            // Find the opening quote of the value
            int startQuote = json.IndexOf("\"", colonIdx + 1);
            if (startQuote < 0) return null;

            // Find closing quote (handle escaped quotes)
            int endQuote = startQuote + 1;
            while (endQuote < json.Length)
            {
                if (json[endQuote] == '\\')
                {
                    endQuote += 2;
                    continue;
                }
                if (json[endQuote] == '"')
                    break;
                endQuote++;
            }

            string content = json.Substring(startQuote + 1, endQuote - startQuote - 1);
            // Unescape
            content = content.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            return content;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AkarionAPI] Parse error: {e.Message}");
            return null;
        }
    }

    public void GenerateImage(string prompt, string imageBase64, Action<Texture2D> onSuccess, Action<string> onError)
    {
        StartCoroutine(GenerateImageCoroutine(prompt, imageBase64, onSuccess, onError));
    }

    private IEnumerator GenerateImageCoroutine(string prompt, string imageBase64, Action<Texture2D> onSuccess, Action<string> onError)
    {
        string fullPrompt = $"Based on this rough sketch, create a clean pixel art game item: {prompt}. " +
                            "Pixel art style, transparent background, suitable for 2D RPG. " +
                            "Clean edges, vibrant colors, centered.";

        // Nano Banana 2 - /ai/image requires top-level model + prompt
        string json;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            // With sketch image reference
            json = "{" +
                $"\"project_id\":\"{ProjectId}\"," +
                $"\"user_id\":\"{PlayerId}\"," +
                $"\"model\":\"google/gemini-3.1-flash-image-preview\"," +
                $"\"prompt\":\"{EscapeJson(fullPrompt)}\"," +
                $"\"messages\":[{{\"role\":\"user\",\"content\":[" +
                    $"{{\"type\":\"text\",\"text\":\"{EscapeJson(fullPrompt)}\"}}," +
                    $"{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/png;base64,{imageBase64}\"}}}}" +
                $"]}}]," +
                $"\"modalities\":[\"image\",\"text\"]" +
                "}";
        }
        else
        {
            json = "{" +
                $"\"project_id\":\"{ProjectId}\"," +
                $"\"user_id\":\"{PlayerId}\"," +
                $"\"model\":\"google/gemini-3.1-flash-image-preview\"," +
                $"\"prompt\":\"{EscapeJson(fullPrompt)}\"" +
                "}";
        }

        Debug.Log($"[AkarionAPI] Image request: model=google/gemini-3.1-flash-image-preview, prompt={prompt}, hasSketch={!string.IsNullOrEmpty(imageBase64)}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{BaseUrl}/ai/image", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {ApiKey}");
            www.timeout = 120;

            yield return www.SendWebRequest();

            string responseText = www.downloadHandler?.text ?? "";
            // Log more of the response for debugging
            Debug.Log($"[AkarionAPI] Image response ({www.responseCode}): {(responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText)}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AkarionAPI] Image HTTP error: {www.error} | Response: {responseText}");
                onError?.Invoke($"HTTP {www.responseCode}: {www.error}");
                yield break;
            }

            // Response formats (try in order):
            // 1. images array: {"images":[{"url":"https://..."}]}
            // 2. data array:   {"data":[{"url":"...","b64_json":"..."}]}
            // 3. chat completion: {"choices":[{"message":{"content":"..."}}]}

            // 1. Try "images" array (Nano Banana documented format)
            string imageUrl = ExtractImageUrl(responseText);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                Debug.Log($"[AkarionAPI] Found image URL: {imageUrl}");
                yield return DownloadTexture(imageUrl, onSuccess, onError);
                yield break;
            }

            // 2. Try b64_json field
            string b64 = ExtractJsonStringField(responseText, "b64_json");
            if (!string.IsNullOrEmpty(b64))
            {
                byte[] imageBytes = Convert.FromBase64String(b64);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.LoadImage(imageBytes);
                onSuccess?.Invoke(tex);
                yield break;
            }

            // 3. Try chat completion content for embedded URL or base64
            string content = ExtractChatReply(responseText);
            if (!string.IsNullOrEmpty(content))
            {
                Debug.Log($"[AkarionAPI] Chat content: {(content.Length > 300 ? content.Substring(0, 300) + "..." : content)}");

                // data:image/... base64
                if (content.StartsWith("data:image"))
                {
                    int commaIdx = content.IndexOf(",");
                    if (commaIdx > 0)
                    {
                        byte[] imageBytes = Convert.FromBase64String(content.Substring(commaIdx + 1));
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tex.filterMode = FilterMode.Point;
                        tex.LoadImage(imageBytes);
                        onSuccess?.Invoke(tex);
                        yield break;
                    }
                }

                // URL in content
                int httpIdx = content.IndexOf("http");
                if (httpIdx >= 0)
                {
                    int urlEnd = httpIdx;
                    while (urlEnd < content.Length && content[urlEnd] != ' ' && content[urlEnd] != '"'
                           && content[urlEnd] != '\n' && content[urlEnd] != ')' && content[urlEnd] != ']')
                        urlEnd++;
                    string url = content.Substring(httpIdx, urlEnd - httpIdx);
                    Debug.Log($"[AkarionAPI] URL from content: {url}");
                    yield return DownloadTexture(url, onSuccess, onError);
                    yield break;
                }
            }

            // Nothing worked — log full response for debugging
            Debug.LogError($"[AkarionAPI] Could not extract image. Full response: {responseText}");
            onError?.Invoke("回應中沒有圖片資料");
        }
    }

    private string ExtractImageUrl(string json)
    {
        // Look for "images" array first, then "data" array, then any "url" field with image-like value
        string[] searchKeys = { "\"images\"", "\"data\"" };
        foreach (var key in searchKeys)
        {
            int keyIdx = json.IndexOf(key);
            if (keyIdx < 0) continue;

            // Find "url" within this array context
            int urlIdx = json.IndexOf("\"url\"", keyIdx);
            if (urlIdx < 0) continue;

            string url = ExtractJsonStringField(json.Substring(urlIdx - 1), "url");
            if (!string.IsNullOrEmpty(url) && url.StartsWith("http"))
                return url;
        }

        return null;
    }

    private IEnumerator DownloadTexture(string url, Action<Texture2D> onSuccess, Action<string> onError)
    {
        Debug.Log($"[AkarionAPI] Downloading texture from: {url}");

        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"下載圖片失敗: {www.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(www);
            tex.filterMode = FilterMode.Point;
            onSuccess?.Invoke(tex);
        }
    }

    private string ExtractJsonStringField(string json, string fieldName)
    {
        string searchKey = $"\"{fieldName}\"";
        int keyIdx = json.IndexOf(searchKey);
        if (keyIdx < 0) return null;

        int colonIdx = json.IndexOf(":", keyIdx + searchKey.Length);
        if (colonIdx < 0) return null;

        // Skip whitespace
        int i = colonIdx + 1;
        while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\n' || json[i] == '\r')) i++;

        if (i >= json.Length || json[i] != '"') return null;

        int startQuote = i;
        int endQuote = startQuote + 1;
        while (endQuote < json.Length)
        {
            if (json[endQuote] == '\\') { endQuote += 2; continue; }
            if (json[endQuote] == '"') break;
            endQuote++;
        }

        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    private string EscapeJson(string str)
    {
        if (str == null) return "";
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
