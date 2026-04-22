using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialoguePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button offerTreasureButton;
    [SerializeField] private Button leaveButton;

    [Header("Panels")]
    [SerializeField] private TreasurePanel treasurePanel;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.03f;

    private Coroutine typewriterCoroutine;
    private bool isWaiting;

    public System.Action OnClose;

    private void Awake()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        offerTreasureButton.onClick.AddListener(OnOfferTreasureClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    public void Open()
    {
        gameObject.SetActive(true);

        if (AkarionAPI.Instance != null)
            AkarionAPI.Instance.ResetConversation();

        dialogueText.text = "";
        inputField.text = "";
        inputField.interactable = true;
        SetButtonsInteractable(true);

        ShowTypewriter("......歡迎你，冒險者。有什麼事嗎？");
    }

    public void Close()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        gameObject.SetActive(false);
        OnClose?.Invoke();
    }

    private void OnSendClicked()
    {
        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message) || isWaiting) return;

        inputField.text = "";
        SendMessage(message);
    }

    private void SendMessage(string message)
    {
        isWaiting = true;
        SetButtonsInteractable(false);
        inputField.interactable = false;

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        dialogueText.text = "等待中...";

        AkarionAPI.Instance.SendChat(message,
            reply =>
            {
                isWaiting = false;
                SetButtonsInteractable(true);
                inputField.interactable = true;
                ShowTypewriter(reply);
            },
            error =>
            {
                isWaiting = false;
                SetButtonsInteractable(true);
                inputField.interactable = true;
                ShowTypewriter("（公主似乎在思考什麼......請再試一次）");
                Debug.LogError($"Chat error: {error}");
            });
    }

    public void ShowTreasureReaction(string itemName)
    {
        gameObject.SetActive(true);
        isWaiting = true;
        SetButtonsInteractable(false);
        inputField.interactable = false;
        dialogueText.text = "等待中...";

        AkarionAPI.Instance.SendTreasureReaction(itemName,
            reply =>
            {
                isWaiting = false;
                SetButtonsInteractable(true);
                inputField.interactable = true;
                ShowTypewriter(reply);
            },
            error =>
            {
                isWaiting = false;
                SetButtonsInteractable(true);
                inputField.interactable = true;
                ShowTypewriter("（公主收下了你的寶物，露出了微妙的表情）");
                Debug.LogError($"Treasure reaction error: {error}");
            });
    }

    private void ShowTypewriter(string text)
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
    }

    private IEnumerator TypewriterCoroutine(string text)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        typewriterCoroutine = null;
    }

    private void OnOfferTreasureClicked()
    {
        if (isWaiting) return;
        gameObject.SetActive(false);
        treasurePanel.Open();
    }

    private void OnLeaveClicked()
    {
        Close();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        sendButton.interactable = interactable;
        offerTreasureButton.interactable = interactable;
    }
}
