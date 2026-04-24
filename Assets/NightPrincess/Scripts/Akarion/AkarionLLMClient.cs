using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NightPrincess.Akarion
{
    public class AkarionLLMClient : MonoBehaviour
    {
        [SerializeField] private AkarionConfig config;
        [SerializeField] private bool verboseLogging = true;

        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
            public ChatMessage(string r, string c) { role = r; content = c; }
        }

        public AkarionConfig Config => config;
        public void SetConfig(AkarionConfig cfg) => config = cfg;

        public void Chat(List<ChatMessage> history, Action<string> onReply, Action<string> onError = null)
        {
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            if (config == null) { onError?.Invoke("AkarionConfig missing (Resources/AkarionConfig.asset)"); return; }
            StartCoroutine(ChatRoutine(history, onReply, onError));
        }

        private IEnumerator ChatRoutine(List<ChatMessage> history, Action<string> onReply, Action<string> onError)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"project_id\":\"").Append(config.projectId).Append("\",");
            sb.Append("\"user_id\":\"").Append(config.playerId).Append("\",");
            sb.Append("\"model\":\"").Append(config.chatModel).Append("\",");
            sb.Append("\"temperature\":0.8,");
            sb.Append("\"max_tokens\":2000,");
            sb.Append("\"messages\":[");
            for (int i = 0; i < history.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"role\":\"").Append(history[i].role).Append("\",");
                sb.Append("\"content\":\"").Append(AkarionAPI.EscapeJson(history[i].content)).Append("\"}");
            }
            sb.Append("]}");

            string url = config.baseUrl + "/ai/chat";
            string body = sb.ToString();
            if (verboseLogging) Debug.Log("[Akarion LLM] POST " + url + " body=" + body);

            yield return AkarionAPI.PostJson(url, body, config,
                resp =>
                {
                    if (verboseLogging) Debug.Log("[Akarion LLM] RESP: " + resp);

                    // Error shape first: {"error":{"message":"..."}}
                    string errMsg = AkarionAPI.ExtractField(resp, "message");
                    string content = ExtractAssistantContent(resp);

                    if (!string.IsNullOrEmpty(content))
                    {
                        onReply?.Invoke(content);
                        return;
                    }

                    string reason = !string.IsNullOrEmpty(errMsg) ? errMsg : "(no content in response)";
                    Debug.LogWarning("[Akarion LLM] Parse failed: " + reason + " | raw=" + resp);
                    onError?.Invoke(reason);
                },
                err =>
                {
                    Debug.LogWarning("[Akarion LLM] HTTP error: " + err);
                    onError?.Invoke(err);
                });
        }

        private static string ExtractAssistantContent(string resp)
        {
            if (string.IsNullOrEmpty(resp)) return null;
            // Locate the message object then its content, so we don't mix up error.message with assistant.content
            int msgIdx = resp.IndexOf("\"message\"", StringComparison.Ordinal);
            if (msgIdx < 0)
            {
                // Some providers return: {"choices":[{"text":"..."}]}
                return AkarionAPI.ExtractField(resp, "text") ?? AkarionAPI.ExtractField(resp, "content");
            }
            string sub = resp.Substring(msgIdx);
            string content = AkarionAPI.ExtractField(sub, "content");
            if (!string.IsNullOrEmpty(content)) return content;
            return AkarionAPI.ExtractField(resp, "content");
        }
    }
}
