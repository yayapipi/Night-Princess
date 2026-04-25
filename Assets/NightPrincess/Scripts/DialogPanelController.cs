using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess
{
    public class DialogPanelController : MonoBehaviour
    {
        [Header("API")]
        public AkarionAPI api;

        [Header("Models")]
        public string chatModel = "x-ai/grok-4.1-fast";
        public string visionModel = "x-ai/grok-4.1-fast";

        [Header("UI Refs")]
        public GameObject panelRoot;
        public TMP_Text dialogText;
        public TMP_InputField playerInputField;
        public Button sendBtn;
        public Button buildItemBtn;
        public Button exitBtn;

        [Header("Linked Panels")]
        public CreateItemPanelController createItemPanel;

        [Header("Personality")]
        [TextArea(4, 12)]
        public string princessSystemPrompt =
            "你扮演一位高傲、嬌縱、又不失優雅的公主。語氣帶著貴族口吻，喜歡用「本宮」、「哼」、「真是的」等詞彙。對話必須非常公主、傲嬌、偶爾撒嬌，但底子裡是善良的。回應務必精煉、感情豐富，控制在三句話內，並且在語句中流露公主獨有的脾氣與威儀。";

        [Header("Effects")]
        public string waitingText = "（公主正在思考……）";
        public float typingSpeed = 0.04f;

        [Header("Greeting")]
        [TextArea(2, 4)]
        public string firstGreeting = "哼，膽子不小，竟敢站在本宮面前。說吧，找本宮何事？";

        private readonly List<AkarionAPI.ChatMessage> history = new List<AkarionAPI.ChatMessage>();
        private Coroutine typingRoutine;
        private bool isWaiting;
        private bool greeted;

        public bool IsOpen { get { return panelRoot != null && panelRoot.activeSelf; } }

        void OnEnable() { UILock.Push(); }
        void OnDisable() { UILock.Pop(); }

        void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (api == null) api = AkarionAPI.Instance;
        }

        void Start()
        {
            if (sendBtn != null) sendBtn.onClick.AddListener(OnSendClicked);
            if (exitBtn != null) exitBtn.onClick.AddListener(CloseDialog);
            if (buildItemBtn != null) buildItemBtn.onClick.AddListener(OnBuildItemClicked);
            if (playerInputField != null)
                playerInputField.onSubmit.AddListener(delegate(string _) { OnSendClicked(); });

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void OpenDialog()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            if (playerInputField != null)
            {
                playerInputField.text = "";
                playerInputField.ActivateInputField();
            }
            if (!greeted)
            {
                greeted = true;
                history.Clear();
                history.Add(AkarionAPI.ChatMessage.System(princessSystemPrompt));
                history.Add(AkarionAPI.ChatMessage.Assistant(firstGreeting));
                ShowAssistantText(firstGreeting);
            }
        }

        public void CloseDialog()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void OnBuildItemClicked()
        {
            if (createItemPanel == null) return;
            CloseDialog();
            createItemPanel.OpenPanel();
        }

        public void OnSendClicked()
        {
            if (isWaiting) return;
            if (playerInputField == null) return;
            string txt = playerInputField.text;
            if (string.IsNullOrWhiteSpace(txt)) return;
            playerInputField.text = "";
            SendUserMessage(txt);
        }

        public void SendUserMessage(string text)
        {
            history.Add(AkarionAPI.ChatMessage.User(text));
            BeginWaiting();
            if (api == null) api = AkarionAPI.Instance;
            api.SendChat(chatModel, history, OnChatResult);
        }

        public void SendItemToPrincess(Texture2D itemTexture, string itemName)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (string.IsNullOrEmpty(itemName)) itemName = "一件神秘的禮物";
            string userMsg = "本騎士獻上一件名為「" + itemName + "」的寶物，請公主過目並評價。";
            BeginWaiting();
            if (api == null) api = AkarionAPI.Instance;

            history.Add(AkarionAPI.ChatMessage.User(userMsg));

            List<AkarionAPI.ChatMessage> visionMsgs = new List<AkarionAPI.ChatMessage>(history);
            string dataUri = null;
            if (itemTexture != null)
            {
                byte[] png = itemTexture.EncodeToPNG();
                dataUri = "data:image/png;base64," + System.Convert.ToBase64String(png);
            }
            if (dataUri != null)
            {
                visionMsgs.RemoveAt(visionMsgs.Count - 1);
                visionMsgs.Add(AkarionAPI.ChatMessage.UserWithImage(userMsg, dataUri));
            }

            api.SendChat(visionModel, visionMsgs, OnChatResult);
        }

        void OnChatResult(bool ok, string text)
        {
            EndWaiting();
            if (!ok)
            {
                ShowAssistantText("（公主似乎有些不悅，先不理你了……" + text + "）");
                return;
            }
            history.Add(AkarionAPI.ChatMessage.Assistant(text));
            ShowAssistantText(text);
        }

        void BeginWaiting()
        {
            isWaiting = true;
            if (sendBtn != null) sendBtn.interactable = false;
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            if (dialogText != null) dialogText.text = waitingText;
        }

        void EndWaiting()
        {
            isWaiting = false;
            if (sendBtn != null) sendBtn.interactable = true;
        }

        void ShowAssistantText(string text)
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypeText(text));
        }

        IEnumerator TypeText(string text)
        {
            if (dialogText == null) yield break;
            dialogText.text = "";
            for (int i = 0; i < text.Length; i++)
            {
                dialogText.text += text[i];
                yield return new WaitForSeconds(typingSpeed);
            }
            typingRoutine = null;
        }
    }
}
