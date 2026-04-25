using UnityEngine;

namespace NightPrincess
{
    public class GameSession : MonoBehaviour
    {
        [Header("References")]
        public AkarionAPI api;

        [Header("Random Player")]
        public bool generateRandomPlayer = true;
        public string playerIdPrefix = "knight_";
        public string[] adjectivePool = new string[]
        {
            "Brave", "Cunning", "Silent", "Crimson", "Shadow",
            "Iron", "Mystic", "Lunar", "Swift", "Stormborn"
        };
        public string[] nounPool = new string[]
        {
            "Knight", "Wanderer", "Samurai", "Hero", "Phantom",
            "Blade", "Ronin", "Sentinel", "Warden", "Nightingale"
        };

        [Header("Stats Table")]
        public string killStatsTable = "kill_stats";
        public bool ensureTableOnStart = true;

        public string CurrentPlayerId { get { return api != null ? api.playerId : null; } }
        public string CurrentDisplayName { get { return api != null ? api.playerDisplayName : null; } }
        public int LocalKillCount { get; private set; }

        private string statsRowId;
        private bool statsRowResolved;

        private static GameSession _instance;
        public static GameSession Instance { get { return _instance; } }

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;

            if (api == null) api = AkarionAPI.Instance;
        }

        void Start()
        {
            if (api == null) { Debug.LogWarning("[GameSession] Akarion API missing"); return; }

            if (generateRandomPlayer)
            {
                api.playerId = GenerateRandomId();
                api.playerDisplayName = GenerateRandomName();
            }

            Debug.Log("[GameSession] Player " + api.playerDisplayName + " (" + api.playerId + ")");

            api.RegisterPlayer(api.playerId, api.playerDisplayName, OnRegisterDone);

            if (ensureTableOnStart) EnsureKillStatsTable();
        }

        void OnRegisterDone(bool ok, string body)
        {
            api.SendEvent("game_start",
                "{\"display_name\":\"" + AkarionAPI.JsonEscape(api.playerDisplayName) + "\"," +
                "\"scene\":\"" + AkarionAPI.JsonEscape(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) + "\"}");

            ResolveStatsRow();
        }

        void EnsureKillStatsTable()
        {
            string schema =
                "[" +
                "{\"name\":\"player_id\",\"type\":\"text\",\"required\":true,\"unique\":true}," +
                "{\"name\":\"display_name\",\"type\":\"text\"}," +
                "{\"name\":\"kill_count\",\"type\":\"number\",\"required\":true}" +
                "]";
            api.DataCreateTable(killStatsTable, schema, null);
        }

        void ResolveStatsRow()
        {
            api.DataQueryByField(killStatsTable, "player_id", api.playerId, OnQueryStats);
        }

        void OnQueryStats(bool ok, string body)
        {
            if (!ok) { statsRowResolved = true; return; }
            string id = AkarionAPI.ExtractFirstStringField(body, "id");
            long? existing = AkarionAPI.ExtractFirstLongField(body, "kill_count");
            if (!string.IsNullOrEmpty(id))
            {
                statsRowId = id;
                LocalKillCount = (int)(existing ?? 0);
                statsRowResolved = true;
                return;
            }

            string row = "{" +
                "\"player_id\":\"" + AkarionAPI.JsonEscape(api.playerId) + "\"," +
                "\"display_name\":\"" + AkarionAPI.JsonEscape(api.playerDisplayName) + "\"," +
                "\"kill_count\":0" +
                "}";
            api.DataInsert(killStatsTable, row, OnInsertStats);
        }

        void OnInsertStats(bool ok, string body)
        {
            if (ok)
            {
                statsRowId = AkarionAPI.ExtractFirstStringField(body, "id");
                LocalKillCount = 0;
            }
            statsRowResolved = true;
        }

        public void RecordEnemyKill(string enemyName)
        {
            LocalKillCount++;
            api.SendEvent("enemy_killed",
                "{\"enemy\":\"" + AkarionAPI.JsonEscape(enemyName ?? "") + "\"," +
                "\"total_kills\":" + LocalKillCount + "}");

            if (statsRowResolved && !string.IsNullOrEmpty(statsRowId))
            {
                string patch = "{\"kill_count\":" + LocalKillCount + "}";
                api.DataPatch(killStatsTable, statsRowId, patch, null);
            }
        }

        public void RecordPlayerDeath()
        {
            api.SendEvent("player_death",
                "{\"total_kills\":" + LocalKillCount + "}");
        }

        public void RecordDialogStart()
        {
            api.SendEvent("dialog_start", "{}");
        }

        public void RecordPlayerChat(string text)
        {
            string data = "{\"text\":\"" + AkarionAPI.JsonEscape(text ?? "") + "\"}";
            api.SendEvent("player_chat", data);
        }

        public void RecordDrawingSubmitted(string itemName, string drawingMediaUrl, string aiMediaUrl)
        {
            string data = "{" +
                "\"item_name\":\"" + AkarionAPI.JsonEscape(itemName ?? "") + "\"," +
                "\"drawing_url\":\"" + AkarionAPI.JsonEscape(drawingMediaUrl ?? "") + "\"," +
                "\"ai_url\":\"" + AkarionAPI.JsonEscape(aiMediaUrl ?? "") + "\"" +
                "}";
            api.SendEvent("player_drawing", data);
        }

        string GenerateRandomId()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(playerIdPrefix);
            for (int i = 0; i < 8; i++)
                sb.Append(chars[Random.Range(0, chars.Length)]);
            return sb.ToString();
        }

        string GenerateRandomName()
        {
            string adj = adjectivePool[Random.Range(0, adjectivePool.Length)];
            string noun = nounPool[Random.Range(0, nounPool.Length)];
            int suffix = Random.Range(100, 9999);
            return adj + noun + "#" + suffix;
        }
    }
}
