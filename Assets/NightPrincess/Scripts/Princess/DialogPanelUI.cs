using System.Collections.Generic;
using NightPrincess.Akarion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess.Princess
{
    public class DialogPanelUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogText;
        [SerializeField] private TMP_InputField playerInput;
        [SerializeField] private Button sendBtn;
        [SerializeField] private Button buildItemBtn;
        [SerializeField] private Button exitBtn;

        [Header("Dependencies")]
        [SerializeField] private AkarionLLMClient llm;
        [SerializeField] private TypewriterEffect typewriter;
        [SerializeField] private CreateItemPanelUI createItemPanel;
        [SerializeField] private AkarionConfig config;

        [Header("Waiting UX")]
        [SerializeField] private string waitingText = "…公主正在思考…";
        [SerializeField] private string openingLine = "暗殺者？本公主等你很久了。";

        private readonly List<AkarionLLMClient.ChatMessage> history = new();
        private bool waiting;

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
            if (typewriter == null) typewriter = GetComponent<TypewriterEffect>();
            if (typewriter != null && dialogText != null) typewriter.SetTarget(dialogText);

            if (sendBtn != null) sendBtn.onClick.AddListener(OnSend);
            if (exitBtn != null) exitBtn.onClick.AddListener(Close);
            if (buildItemBtn != null) buildItemBtn.onClick.AddListener(OnOpenCreateItem);

            if (llm == null) llm = Object.FindFirstObjectByType<AkarionLLMClient>(FindObjectsInactive.Include);
            if (config == null) config = Resources.Load<AkarionConfig>("AkarionConfig");
            initialized = true;
        }

        public void Open()
        {
            EnsureInitialized();
            panelRoot.SetActive(true);
            AkarionEventLogger.Instance?.LogEvent(AkarionEvents.DialogStart);

            if (history.Count == 0)
            {
                string systemPrompt = config != null ? config.princessSystemPrompt : "You are a haughty princess NPC.";
                history.Add(new AkarionLLMClient.ChatMessage("system", systemPrompt));
                AppendAssistant(openingLine);
                if (typewriter != null) typewriter.Show(openingLine);
                else if (dialogText != null) dialogText.text = openingLine;
            }

            if (playerInput != null)
            {
                playerInput.text = string.Empty;
                playerInput.ActivateInputField();
            }
        }

        public void Close()
        {
            panelRoot.SetActive(false);
            if (createItemPanel != null && createItemPanel.IsOpen) createItemPanel.Close();
        }

        public void OnGiveBack(string itemName, string imageUrl)
        {
            panelRoot.SetActive(true);
            string userText = $"（暗殺者獻上了一件名為「{itemName}」的道具）";
            history.Add(new AkarionLLMClient.ChatMessage("user", userText));
            AkarionEventLogger.Instance?.LogText(AkarionEvents.GiveItem, "item_name", itemName + "|" + (imageUrl ?? ""));
            SendChat();
        }

        private void OnSend()
        {
            if (waiting) return;
            if (playerInput == null || string.IsNullOrWhiteSpace(playerInput.text)) return;
            string msg = playerInput.text.Trim();
            playerInput.text = string.Empty;

            history.Add(new AkarionLLMClient.ChatMessage("user", msg));
            AkarionEventLogger.Instance?.LogText(AkarionEvents.PlayerMessage, "text", msg);
            SendChat();
        }

        private void OnOpenCreateItem()
        {
            if (createItemPanel == null) return;
            panelRoot.SetActive(false);
            createItemPanel.Open(this);
        }

        private void SendChat()
        {
            if (llm == null)
            {
                string msg = "(場景未掛 AkarionLLMClient，請執行 Night Princess → Do Everything)";
                if (dialogText != null) dialogText.text = msg;
                Debug.LogWarning("[Princess] " + msg);
                return;
            }
            waiting = true;
            if (sendBtn != null) sendBtn.interactable = false;
            if (typewriter != null) typewriter.ShowInstant(waitingText);
            else if (dialogText != null) dialogText.text = waitingText;

            llm.Chat(history,
                reply =>
                {
                    waiting = false;
                    if (sendBtn != null) sendBtn.interactable = true;
                    AppendAssistant(reply);
                    if (typewriter != null) typewriter.Show(reply);
                    else if (dialogText != null) dialogText.text = reply;
                },
                err =>
                {
                    waiting = false;
                    if (sendBtn != null) sendBtn.interactable = true;
                    string shortErr = err == null ? "unknown" : (err.Length > 160 ? err.Substring(0, 160) + "…" : err);
                    if (dialogText != null) dialogText.text = "（連線失敗：" + shortErr + "）";
                    Debug.LogWarning("[Princess] Chat error: " + err);
                });
        }

        private void AppendAssistant(string text)
        {
            history.Add(new AkarionLLMClient.ChatMessage("assistant", text));
        }
    }
}
