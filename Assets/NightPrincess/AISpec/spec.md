# Night Princess — AI 互動系統規格書

## 專案環境

- Unity 6000.3.10f1
- UI 使用 TextMeshPro（TMP_Text、TMP_InputField），不要使用 Legacy UI（Text、InputField）
- 已安裝 DOTween（Assets/Plugins/Demigiant/DOTween）
- 2D 像素風格遊戲，FilterMode 統一用 Point

---

## AkarionHub API 規格

Base URL: `https://akarion.dev/api/v1`

### 認證

Header 擇一使用：
```
Authorization: Bearer <API_KEY>
```
或
```
X-Api-Key: <API_KEY>
```

### 憑證

```
API Key:    ak_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04
Project ID: 449f5cca-fd8e-40c6-a38e-ff3aa587d8dc
Player ID:  test_mo9xxz0z
```

### ⚠️ API 重要注意事項

1. 欄位名稱使用 **snake_case**：`project_id`、`user_id`（不是 camelCase）
2. 模型名稱必須包含 **provider 前綴**，例如 `google/gemini-2.5-flash`（不是 `gemini-2.5-flash`）
3. Unity 的 `JsonUtility.ToJson()` 無法正確序列化巢狀 `List<T>`，請手動拼接 JSON 字串
4. 回應格式可能因模型不同而異（見下方各端點說明）

---

### 端點 1：AI 聊天 — `POST /ai/chat`

用途：公主 NPC 的 LLM 對話

#### 請求格式

```json
{
  "project_id": "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc",
  "user_id": "test_mo9xxz0z",
  "model": "google/gemini-2.5-flash",
  "messages": [
    {"role": "system", "content": "你是公主艾琳..."},
    {"role": "user", "content": "你好"}
  ],
  "max_tokens": 200,
  "temperature": 0.8
}
```

#### 回應格式

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "哼，又是一個冒險者..."
      }
    }
  ]
}
```

取值路徑：`choices[0].message.content`

#### 可用聊天模型

- `google/gemini-2.5-flash`（推薦，便宜快速）
- `openai/gpt-5`
- `anthropic/claude-opus-4`
- `x-ai/grok-4.1-fast`

---

### 端點 2：AI 圖片生成 — `POST /ai/image`

用途：鍛造寶物的圖片生成

#### ⚠️ 關鍵注意事項

- **頂層必須同時有 `model` 和 `prompt` 欄位**，否則回傳 400 錯誤
- 不同模型的完整請求格式不同（見下方）
- 回應格式也因模型而異

#### Nano Banana 2（推薦）— `google/gemini-3.1-flash-image-preview`

此模型支援圖片編輯，需要額外帶 `messages` + `modalities` 欄位。

**純文字生成：**

```json
{
  "project_id": "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc",
  "user_id": "test_mo9xxz0z",
  "model": "google/gemini-3.1-flash-image-preview",
  "prompt": "A pixel art sword, transparent background"
}
```

**帶參考圖生成（圖片編輯）：**

```json
{
  "project_id": "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc",
  "user_id": "test_mo9xxz0z",
  "model": "google/gemini-3.1-flash-image-preview",
  "prompt": "A pixel art sword, transparent background",
  "messages": [
    {
      "role": "user",
      "content": [
        {"type": "text", "text": "Based on this sketch, create a pixel art item..."},
        {"type": "image_url", "image_url": {"url": "data:image/png;base64,<BASE64>"}}
      ]
    }
  ],
  "modalities": ["image", "text"]
}
```

#### 回應格式（Nano Banana 系列）

回應是 **chat completion 格式**，不是 image data 格式：

```json
{
  "choices": [
    {
      "message": {
        "content": "..."
      }
    }
  ]
}
```

圖片可能在以下位置，需要依序嘗試：
1. `choices[0].message.content` 內含 `data:image/png;base64,...` 字串
2. `choices[0].message.content` 內含圖片 URL（`https://...`）
3. 回應 JSON 中的 `inline_data` 欄位含 base64 資料

#### 其他圖片模型（回應格式不同）

FLUX、SDXL 等模型回傳標準 image 格式：

```json
{
  "data": [
    {"url": "https://...", "b64_json": "..."}
  ]
}
```

#### 所有可用圖片模型

| Model ID | 名稱 |
|----------|------|
| `google/gemini-3.1-flash-image-preview` | Nano Banana 2（推薦） |
| `google/gemini-2.5-flash-image` | Nano Banana |
| `google/gemini-3-pro-image-preview` | Nano Banana Pro |
| `openai/gpt-5-image` | GPT-5 Image |
| `openai/gpt-5-image-mini` | GPT-5 Image Mini |
| `black-forest-labs/flux-2` | FLUX 2 |

---

## 功能規格

### 一、公主 NPC 互動

**PrincessNPC.cs** — 掛在公主角色上

邏輯：
1. 每幀偵測玩家距離（`Vector2.Distance`）
2. 玩家進入範圍（預設 2f）時，顯示「按 E 對話」提示 UI
3. 按 E 鍵 → 呼叫 `PlayerController.SetInteracting(true)` 鎖定移動 → 開啟對話面板
4. 對話結束 → 呼叫 `PlayerController.SetInteracting(false)` 解除鎖定

需要修改 **PlayerController.cs**：
- 新增 `bool isInteracting` 欄位
- 新增 `public void SetInteracting(bool)` 方法
- `Update()` 開頭加入 `if (isDead || isInteracting) return;`
- `SetInteracting(true)` 時停止移動（`rb.linearVelocity = Vector2.zero`）

---

### 二、對話面板

**DialoguePanel.cs** — 掛在對話面板 UI 根物件上

UI 元件（全部用 TextMeshPro）：
- `TMP_Text dialogueText` — 顯示公主的對話文字
- `TMP_InputField inputField` — 玩家輸入訊息
- `Button sendButton` — 發送訊息
- `Button offerTreasureButton` — 切換到獻上寶物面板
- `Button leaveButton` — 關閉對話

功能：
- 開啟時呼叫 `AkarionAPI.ResetConversation()` 初始化對話歷史
- 發送訊息時：禁用按鈕 → 顯示「等待中...」→ 呼叫 API → 收到回覆後用打字效果逐字顯示
- **打字效果**：用 Coroutine 逐字加到 `dialogueText.text`，每字間隔 0.03 秒（`WaitForSecondsRealtime`）
- 錯誤時顯示 fallback 文字，不要讓 UI 卡死

---

### 三、獻上寶物面板

**TreasurePanel.cs** — 掛在寶物面板 UI 根物件上

UI 元件：
- `DrawingCanvas drawingCanvas` — 繪圖區域（RawImage + DrawingCanvas 腳本）
- `TMP_InputField itemNameInput` — 道具名稱輸入
- `Button forgeButton` — 鍛造按鈕
- `Button giveButton` — 送給公主按鈕（預設隱藏）
- `Button clearButton` — 清除畫布
- `GameObject loadingObj` — 鍛造中的 Loading 遮罩

流程：
1. 玩家在畫布上用滑鼠畫草圖 + 輸入道具名稱
2. 點擊「鍛造」→ 顯示 loadingObj → 呼叫 `AkarionAPI.GenerateImage()` 帶上草圖 base64 和名稱
3. 生成完成 → 隱藏 loadingObj → 用 `DrawingCanvas.SetGeneratedImage()` 顯示 AI 生成的圖片 → 隱藏鍛造按鈕 → 顯示「送給公主」按鈕
4. 點擊「送給公主」→ 關閉寶物面板 → 回到對話面板 → 呼叫 `DialoguePanel.ShowTreasureReaction(itemName)` 讓公主回應

---

### 四、繪圖畫布

**DrawingCanvas.cs** — 掛在有 RawImage 的物件上

前置條件（缺一不可，否則繪圖無反應）：
- 物件上有 `RawImage` 元件，且 `raycastTarget = true`
- 父 Canvas 上有 `GraphicRaycaster` 元件
- 場景中有 `EventSystem` 物件

實作方式：
- 實作 `IPointerDownHandler`、`IDragHandler`、`IPointerUpHandler`
- 在 `Awake()` 建立 256x256 的 `Texture2D`（RGBA32, FilterMode.Point）指定給 RawImage
- 滑鼠事件 → `RectTransformUtility.ScreenPointToLocalPointInRectangle()` 轉換座標 → 在 Texture2D 上用圓形筆刷繪製 → `Apply()`
- `GetBase64()` — `EncodeToPNG()` → `Convert.ToBase64String()`
- `SetGeneratedImage(Texture2D)` — 用 `RenderTexture` 縮放到畫布尺寸後顯示
- `ClearCanvas()` — 填充白色像素

---

### 五、API 封裝

**AkarionAPI.cs** — 掛在場景中的空物件上，Singleton 模式

⚠️ 實作重點：
- **手動拼接 JSON**，不用 `JsonUtility.ToJson()`，因為巢狀 List 序列化有問題
- **對話歷史**：維護 `List<MessageEntry>` 包含 role + content
- **圖片回應解析**：需要處理多種格式（見上方 API 說明），依序嘗試 `b64_json` → `url` → `choices[0].message.content` 中的 base64/URL
- **錯誤處理**：HTTP 錯誤時 log 完整的 response body，方便除錯
- **timeout**：聊天 30 秒，圖片生成 120 秒

公開方法：
```csharp
void ResetConversation()
void SendChat(string userMessage, Action<string> onSuccess, Action<string> onError)
void SendTreasureReaction(string itemName, Action<string> onSuccess, Action<string> onError)
void GenerateImage(string prompt, string imageBase64, Action<Texture2D> onSuccess, Action<string> onError)
```

---

## 場景設定 Hierarchy

```
Scene
├── Main Camera（掛 CameraShake）
├── Player（掛 PlayerController，Tag: Player）
├── Princess（掛 PrincessNPC + Animator + BoxCollider2D(trigger)）
├── AkarionAPI（空物件，掛 AkarionAPI）
├── EventSystem（掛 EventSystem + StandaloneInputModule）
└── Canvas（掛 Canvas + CanvasScaler + GraphicRaycaster）
    ├── InteractHint（TMP_Text「按 E 對話」，預設隱藏）
    ├── DialoguePanel（掛 DialoguePanel，預設隱藏）
    │   ├── DialogueText（TMP_Text）
    │   ├── InputField（TMP_InputField）
    │   ├── SendButton（Button）
    │   ├── OfferTreasureButton（Button）
    │   └── LeaveButton（Button）
    └── TreasurePanel（掛 TreasurePanel，預設隱藏）
        ├── DrawingCanvas（RawImage + DrawingCanvas 腳本）
        ├── ItemNameInput（TMP_InputField）
        ├── ForgeButton（Button）
        ├── GiveButton（Button，預設隱藏）
        ├── ClearButton（Button）
        └── LoadingObj（Image 遮罩 + TMP_Text「鍛造中...」，預設隱藏）
```

---

## Inspector 連結清單

| 腳本 | 欄位 | 拖入物件 |
|------|------|----------|
| PrincessNPC | dialoguePanel | DialoguePanel 物件 |
| PrincessNPC | interactHint | InteractHint 物件 |
| DialoguePanel | dialogueText | DialogueText 的 TMP_Text |
| DialoguePanel | inputField | InputField 的 TMP_InputField |
| DialoguePanel | sendButton | SendButton 的 Button |
| DialoguePanel | offerTreasureButton | OfferTreasureButton 的 Button |
| DialoguePanel | leaveButton | LeaveButton 的 Button |
| DialoguePanel | treasurePanel | TreasurePanel 物件 |
| TreasurePanel | drawingCanvas | DrawingCanvas 物件 |
| TreasurePanel | itemNameInput | ItemNameInput 的 TMP_InputField |
| TreasurePanel | forgeButton | ForgeButton 的 Button |
| TreasurePanel | giveButton | GiveButton 的 Button |
| TreasurePanel | clearButton | ClearButton 的 Button |
| TreasurePanel | loadingObj | LoadingObj 物件 |
| TreasurePanel | dialoguePanel | DialoguePanel 物件 |
