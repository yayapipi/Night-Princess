using UnityEngine;

namespace NightPrincess.Akarion
{
    public class AkarionPlayerManager : MonoBehaviour
    {
        public static AkarionPlayerManager Instance { get; private set; }

        [SerializeField] private AkarionConfig config;
        [SerializeField] private bool registerOnStart = true;
        [SerializeField] private bool logGameStartEvent = true;

        public AkarionConfig Config => config;
        public bool IsRegistered { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (registerOnStart) RegisterPlayer();
        }

        public void RegisterPlayer()
        {
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            if (config == null) { Debug.LogWarning("[Akarion] Config missing (Resources/AkarionConfig.asset)"); return; }
            StartCoroutine(RegisterRoutine());
        }

        private System.Collections.IEnumerator RegisterRoutine()
        {
            string url = config.baseUrl + "/players";
            string body = "{"
                + "\"project_id\":\"" + config.projectId + "\","
                + "\"player_id\":\"" + config.playerId + "\","
                + "\"display_name\":\"" + AkarionAPI.EscapeJson(config.displayName) + "\","
                + "\"platform\":\"unity\","
                + "\"os\":\"" + AkarionAPI.EscapeJson(SystemInfo.operatingSystem) + "\","
                + "\"region\":\"TW\""
                + "}";

            yield return AkarionAPI.PostJson(url, body, config,
                ok =>
                {
                    IsRegistered = true;
                    Debug.Log("[Akarion] Player registered");
                    if (logGameStartEvent && AkarionEventLogger.Instance != null)
                        AkarionEventLogger.Instance.LogEvent(AkarionEvents.GameStart);
                },
                err =>
                {
                    // Player already exists is common — still emit game start event
                    IsRegistered = true;
                    Debug.Log("[Akarion] Player register returned: " + err);
                    if (logGameStartEvent && AkarionEventLogger.Instance != null)
                        AkarionEventLogger.Instance.LogEvent(AkarionEvents.GameStart);
                },
                playerMode: false);
        }
    }
}
