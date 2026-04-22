using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreasurePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private DrawingCanvas drawingCanvas;
    [SerializeField] private TMP_InputField itemNameInput;
    [SerializeField] private Button forgeButton;
    [SerializeField] private Button giveButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private GameObject loadingObj;

    [Header("Panels")]
    [SerializeField] private DialoguePanel dialoguePanel;

    private bool isForging;
    private string currentItemName;

    private void Awake()
    {
        forgeButton.onClick.AddListener(OnForgeClicked);
        giveButton.onClick.AddListener(OnGiveClicked);
        clearButton.onClick.AddListener(OnClearClicked);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        drawingCanvas.ClearCanvas();
        itemNameInput.text = "";
        itemNameInput.interactable = true;
        forgeButton.gameObject.SetActive(true);
        giveButton.gameObject.SetActive(false);
        loadingObj.SetActive(false);
        isForging = false;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnForgeClicked()
    {
        string itemName = itemNameInput.text.Trim();
        if (string.IsNullOrEmpty(itemName) || isForging) return;

        currentItemName = itemName;
        isForging = true;

        loadingObj.SetActive(true);
        forgeButton.interactable = false;
        itemNameInput.interactable = false;

        string imageBase64 = drawingCanvas.GetBase64();

        AkarionAPI.Instance.GenerateImage(itemName, imageBase64,
            texture =>
            {
                isForging = false;
                loadingObj.SetActive(false);

                drawingCanvas.SetGeneratedImage(texture);

                // Hide forge button, show give button
                forgeButton.gameObject.SetActive(false);
                giveButton.gameObject.SetActive(true);
            },
            error =>
            {
                isForging = false;
                loadingObj.SetActive(false);
                forgeButton.interactable = true;
                itemNameInput.interactable = true;
                Debug.LogError($"Image generation error: {error}");
            });
    }

    private void OnGiveClicked()
    {
        Close();
        dialoguePanel.ShowTreasureReaction(currentItemName);
    }

    private void OnClearClicked()
    {
        drawingCanvas.ClearCanvas();
    }
}
