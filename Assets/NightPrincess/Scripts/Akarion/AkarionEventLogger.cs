using UnityEngine;

namespace NightPrincess.Akarion
{
    public static class AkarionEvents
    {
        public const string GameStart = "game_start";
        public const string EnemyKilled = "enemy_killed";
        public const string PlayerDied = "player_died";
        public const string DialogStart = "dialog_start";
        public const string PlayerMessage = "player_message";
        public const string PlayerDrawing = "player_drawing";
        public const string ItemForged = "item_forged";
        public const string GiveItem = "give_item";
    }

    public class AkarionEventLogger : MonoBehaviour
    {
        public static AkarionEventLogger Instance { get; private set; }

        [SerializeField] private AkarionConfig config;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LogEvent(string eventType, string eventDataJson = "{}")
        {
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            if (config == null) { Debug.LogWarning("[Akarion] Config missing (Resources/AkarionConfig.asset)"); return; }
            StartCoroutine(LogRoutine(eventType, eventDataJson));
        }

        public void LogText(string eventType, string key, string value)
        {
            string payload = "{\"" + AkarionAPI.EscapeJson(key) + "\":\"" + AkarionAPI.EscapeJson(value) + "\"}";
            LogEvent(eventType, payload);
        }

        private System.Collections.IEnumerator LogRoutine(string eventType, string eventDataJson)
        {
            string url = config.baseUrl + "/events";
            string body = "{"
                + "\"project_id\":\"" + config.projectId + "\","
                + "\"player_id\":\"" + config.playerId + "\","
                + "\"event_type\":\"" + AkarionAPI.EscapeJson(eventType) + "\","
                + "\"event_data\":" + (string.IsNullOrEmpty(eventDataJson) ? "{}" : eventDataJson) + ","
                + "\"source\":\"Unity NightPrincess\""
                + "}";

            yield return AkarionAPI.PostJson(url, body, config,
                _ => Debug.Log($"[Akarion] Event logged: {eventType}"),
                err => Debug.LogWarning($"[Akarion] Event failed: {eventType} -> {err}"));
        }
    }
}
