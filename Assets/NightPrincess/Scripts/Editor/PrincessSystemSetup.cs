using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PrincessSystemSetup
{
    [MenuItem("Night Princess/Setup Princess System")]
    public static void Setup()
    {
        // ─── 1. AkarionAPI (singleton) ───
        var apiObj = new GameObject("AkarionAPI");
        apiObj.AddComponent<AkarionAPI>();
        Undo.RegisterCreatedObjectUndo(apiObj, "Create AkarionAPI");

        // ─── 2. Find Princess in scene ───
        GameObject princessObj = null;
        // Try to find existing princess object
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr.gameObject.name.ToLower().Contains("princess") || sr.gameObject.name.ToLower().Contains("eilin"))
            {
                princessObj = sr.gameObject;
                break;
            }
        }

        if (princessObj == null)
        {
            // Create a placeholder princess
            princessObj = new GameObject("Princess");
            princessObj.transform.position = new Vector3(3f, 0f, 0f);
            var sr2 = princessObj.AddComponent<SpriteRenderer>();

            // Try to load princess sprite
            var idleSprite = Resources.Load<Sprite>("Princess/eilin_idle_0");
            if (idleSprite != null) sr2.sprite = idleSprite;

            // Add animator
            var anim = princessObj.AddComponent<Animator>();
            var controller = Resources.Load<RuntimeAnimatorController>("Animation/eilin_idle_0");
            if (controller != null) anim.runtimeAnimatorController = controller;

            // Add collider for reference
            var col = princessObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1.5f);

            Undo.RegisterCreatedObjectUndo(princessObj, "Create Princess");
        }

        // ─── 3. Create Canvas ───
        var canvasObj = new GameObject("PrincessUI_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

        // Ensure EventSystem exists
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

        // ─── 4. Interact Hint (Press E) ───
        var hintObj = CreateUIText(canvasObj.transform, "InteractHint", "按 E 對話",
            new Vector2(0.5f, 0.3f), new Vector2(200, 40), 20);
        hintObj.SetActive(false);

        // ─── 5. Dialogue Panel ───
        var dialoguePanel = CreatePanel(canvasObj.transform, "DialoguePanel",
            new Vector2(0.5f, 0.5f), new Vector2(600, 400), new Color(0.1f, 0.1f, 0.15f, 0.95f));

        // Title
        CreateUIText(dialoguePanel.transform, "Title", "── 公主艾琳 ──",
            new Vector2(0.5f, 1f), new Vector2(300, 40), 22,
            new Vector2(0, -20), TextAnchor.MiddleCenter);

        // Dialogue text area
        var dialogueTextObj = CreateUIText(dialoguePanel.transform, "DialogueText", "",
            new Vector2(0.5f, 0.5f), new Vector2(540, 200), 18,
            new Vector2(0, 30), TextAnchor.UpperLeft);
        var dialogueText = dialogueTextObj.GetComponent<Text>();
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;

        // Input field
        var inputFieldObj = CreateInputField(dialoguePanel.transform, "InputField", "輸入訊息...",
            new Vector2(0.5f, 0f), new Vector2(400, 40), new Vector2(-50, 70));

        // Send button
        var sendBtn = CreateButton(dialoguePanel.transform, "SendButton", "發送",
            new Vector2(1f, 0f), new Vector2(100, 40), new Vector2(-70, 70),
            new Color(0.2f, 0.6f, 0.3f));

        // Offer treasure button
        var offerBtn = CreateButton(dialoguePanel.transform, "OfferTreasureButton", "獻上寶物",
            new Vector2(0f, 0f), new Vector2(150, 40), new Vector2(100, 20),
            new Color(0.7f, 0.5f, 0.1f));

        // Leave button
        var leaveBtn = CreateButton(dialoguePanel.transform, "LeaveButton", "離開",
            new Vector2(1f, 0f), new Vector2(100, 40), new Vector2(-70, 20),
            new Color(0.5f, 0.2f, 0.2f));

        // ─── 6. Treasure Panel ───
        var treasurePanel = CreatePanel(canvasObj.transform, "TreasurePanel",
            new Vector2(0.5f, 0.5f), new Vector2(650, 500), new Color(0.12f, 0.08f, 0.15f, 0.95f));

        // Title
        CreateUIText(treasurePanel.transform, "TreasureTitle", "── 鍛造寶物 ──",
            new Vector2(0.5f, 1f), new Vector2(300, 40), 22,
            new Vector2(0, -20), TextAnchor.MiddleCenter);

        // Drawing canvas (RawImage)
        var canvasArea = new GameObject("DrawingCanvas", typeof(RectTransform));
        canvasArea.transform.SetParent(treasurePanel.transform, false);
        var canvasRT = canvasArea.GetComponent<RectTransform>();
        canvasRT.anchorMin = canvasRT.anchorMax = new Vector2(0.5f, 0.55f);
        canvasRT.sizeDelta = new Vector2(256, 256);
        canvasRT.anchoredPosition = new Vector2(0, 20);
        var rawImg = canvasArea.AddComponent<RawImage>();
        rawImg.color = Color.white;
        var drawingCanvas = canvasArea.AddComponent<DrawingCanvas>();

        // Outline around canvas
        var outline = canvasArea.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.6f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);

        // Item name input
        var itemNameInput = CreateInputField(treasurePanel.transform, "ItemNameInput", "輸入道具名稱...",
            new Vector2(0.5f, 0f), new Vector2(400, 40), new Vector2(0, 110));

        // Forge button
        var forgeBtn = CreateButton(treasurePanel.transform, "ForgeButton", "鍛造",
            new Vector2(0.5f, 0f), new Vector2(120, 40), new Vector2(-80, 60),
            new Color(0.8f, 0.4f, 0.1f));

        // Clear button
        var clearBtn = CreateButton(treasurePanel.transform, "ClearButton", "清除",
            new Vector2(0.5f, 0f), new Vector2(120, 40), new Vector2(80, 60),
            new Color(0.4f, 0.4f, 0.4f));

        // Give to princess button (hidden by default)
        var giveBtn = CreateButton(treasurePanel.transform, "GiveButton", "送給公主",
            new Vector2(0.5f, 0f), new Vector2(180, 45), new Vector2(0, 15),
            new Color(0.8f, 0.2f, 0.5f));

        // Loading object
        var loadingObj = new GameObject("LoadingObj", typeof(RectTransform));
        loadingObj.transform.SetParent(treasurePanel.transform, false);
        var loadRT = loadingObj.GetComponent<RectTransform>();
        loadRT.anchorMin = loadRT.anchorMax = new Vector2(0.5f, 0.55f);
        loadRT.sizeDelta = new Vector2(256, 256);
        loadRT.anchoredPosition = new Vector2(0, 20);
        var loadBg = loadingObj.AddComponent<Image>();
        loadBg.color = new Color(0, 0, 0, 0.7f);

        var loadTextObj = CreateUIText(loadingObj.transform, "LoadingText", "鍛造中...",
            new Vector2(0.5f, 0.5f), new Vector2(200, 40), 24,
            Vector2.zero, TextAnchor.MiddleCenter);
        loadingObj.SetActive(false);

        // Try to add a loading icon prefab if available
        var loadIconPrefab = Resources.Load<GameObject>("L01");
        if (loadIconPrefab == null)
        {
            // Try loading from Loading Icons folder
            var allPrefabs = Resources.LoadAll<GameObject>("");
            foreach (var p in allPrefabs)
            {
                if (p.name.StartsWith("L0"))
                {
                    loadIconPrefab = p;
                    break;
                }
            }
        }

        // ─── 7. Wire up components ───

        // DialoguePanel script
        var dialoguePanelScript = dialoguePanel.AddComponent<DialoguePanel>();
        SetPrivateField(dialoguePanelScript, "dialogueText", dialogueText);
        SetPrivateField(dialoguePanelScript, "inputField", inputFieldObj.GetComponent<InputField>());
        SetPrivateField(dialoguePanelScript, "sendButton", sendBtn.GetComponent<Button>());
        SetPrivateField(dialoguePanelScript, "offerTreasureButton", offerBtn.GetComponent<Button>());
        SetPrivateField(dialoguePanelScript, "leaveButton", leaveBtn.GetComponent<Button>());

        // TreasurePanel script
        var treasurePanelScript = treasurePanel.AddComponent<TreasurePanel>();
        SetPrivateField(treasurePanelScript, "drawingCanvas", drawingCanvas);
        SetPrivateField(treasurePanelScript, "itemNameInput", itemNameInput.GetComponent<InputField>());
        SetPrivateField(treasurePanelScript, "forgeButton", forgeBtn.GetComponent<Button>());
        SetPrivateField(treasurePanelScript, "giveButton", giveBtn.GetComponent<Button>());
        SetPrivateField(treasurePanelScript, "clearButton", clearBtn.GetComponent<Button>());
        SetPrivateField(treasurePanelScript, "loadingObj", loadingObj);
        SetPrivateField(treasurePanelScript, "dialoguePanel", dialoguePanelScript);

        // Cross-reference: DialoguePanel -> TreasurePanel
        SetPrivateField(dialoguePanelScript, "treasurePanel", treasurePanelScript);

        // PrincessNPC script
        var princessNPC = princessObj.GetComponent<PrincessNPC>();
        if (princessNPC == null)
            princessNPC = princessObj.AddComponent<PrincessNPC>();
        SetPrivateField(princessNPC, "dialoguePanel", dialoguePanelScript);
        SetPrivateField(princessNPC, "interactHint", hintObj);

        // Start with panels hidden
        dialoguePanel.SetActive(false);
        treasurePanel.SetActive(false);

        // ─── 8. Select the canvas for review ───
        Selection.activeGameObject = canvasObj;
        EditorUtility.SetDirty(canvasObj);

        Debug.Log("<color=green>[Night Princess] 公主系統設定完成！</color>\n" +
                  "- AkarionAPI singleton 已建立\n" +
                  "- 公主 NPC 已設定\n" +
                  "- 對話面板 UI 已建立\n" +
                  "- 寶物鍛造面板 UI 已建立\n" +
                  "- 所有 Inspector 參考已自動連結");
    }

    // ─── Helper Methods ───

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color bgColor)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    private static GameObject CreateUIText(Transform parent, string name, string text,
        Vector2 anchor, Vector2 size, int fontSize,
        Vector2 offset = default, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;

        var txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = Color.white;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Vector2 offset, Color color)
    {
        var btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;

        var img = btnObj.AddComponent<Image>();
        img.color = color;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;

        // Button label
        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(btnObj.transform, false);

        var labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.fontSize = 16;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btnObj;
    }

    private static GameObject CreateInputField(Transform parent, string name, string placeholder,
        Vector2 anchor, Vector2 size, Vector2 offset)
    {
        var inputObj = new GameObject(name, typeof(RectTransform));
        inputObj.transform.SetParent(parent, false);

        var rt = inputObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;

        var bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Text child
        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inputObj.transform, false);
        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 2);
        textRT.offsetMax = new Vector2(-10, -2);

        var inputText = textObj.AddComponent<Text>();
        inputText.fontSize = 16;
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.supportRichText = false;

        // Placeholder child
        var phObj = new GameObject("Placeholder", typeof(RectTransform));
        phObj.transform.SetParent(inputObj.transform, false);
        var phRT = phObj.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(10, 2);
        phRT.offsetMax = new Vector2(-10, -2);

        var phText = phObj.AddComponent<Text>();
        phText.text = placeholder;
        phText.fontSize = 16;
        phText.color = new Color(0.6f, 0.6f, 0.6f);
        phText.alignment = TextAnchor.MiddleLeft;
        phText.fontStyle = FontStyle.Italic;
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // InputField component
        var input = inputObj.AddComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = phText;

        return inputObj;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
            EditorUtility.SetDirty(target as Object);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found on {type.Name}");
        }
    }
}
