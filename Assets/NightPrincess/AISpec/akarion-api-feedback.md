# AkarionHub API 文件改善建議

## 背景

我在 Unity 專案中串接 AkarionHub API（聊天 + 圖片生成），使用 AI 輔助開發。過程中因為文件資訊不足，反覆除錯了多次才成功。以下是具體遇到的問題和建議。

---

## 問題 1：缺少可直接使用的 curl 範例

**現狀：** 文件只有欄位說明，沒有完整的 curl 範例。

**影響：** 開發者無法快速驗證請求格式是否正確，每次都要靠猜測 → 發請求 → 看錯誤 → 修正，效率很低。

**建議：** 每個端點都加上一個可以直接複製貼上到終端機執行的 curl 範例：

```bash
# 聊天
curl -X POST https://akarion.dev/api/v1/ai/chat \
  -H "Authorization: Bearer ak_your_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "project_id": "your-project-uuid",
    "user_id": "your-player-id",
    "model": "google/gemini-2.5-flash",
    "messages": [{"role": "user", "content": "Hello"}],
    "max_tokens": 200
  }'

# 圖片生成
curl -X POST https://akarion.dev/api/v1/ai/image \
  -H "Authorization: Bearer ak_your_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "project_id": "your-project-uuid",
    "user_id": "your-player-id",
    "model": "google/gemini-3.1-flash-image-preview",
    "prompt": "A pixel art sword"
  }'
```

---

## 問題 2：欄位命名規則沒有明確標示

**現狀：** 欄位名稱是 `project_id`（snake_case），但文件中沒有強調這一點。

**影響：** 許多開發者（尤其是前端/Unity 開發者）習慣用 camelCase（`projectId`），第一次串接幾乎一定會寫錯，得到 400 錯誤後才發現。AI 輔助工具也會預設使用 camelCase。

**建議：** 在文件開頭的「快速開始」區域加一個顯眼的提示：

> ⚠️ 所有 API 欄位使用 snake_case 命名（例如 `project_id`，不是 `projectId`）

---

## 問題 3：模型名稱必須包含 provider 前綴，但文件沒有強調

**現狀：** 模型列表有寫 `google/gemini-2.5-flash`，但沒有說明「前綴是必須的」。

**影響：** 開發者可能只寫 `gemini-2.5-flash`，得到錯誤後不知道原因。

**建議：** 在模型列表旁加上說明：

> 模型名稱必須包含 provider 前綴，例如 `google/gemini-2.5-flash`，而不是 `gemini-2.5-flash`。

---

## 問題 4：`/ai/image` 端點的回應格式因模型而異，但文件沒有區分

**現狀：** `/ai/image` 的文件只展示了一種回應格式。

**實際情況：**
- FLUX、SDXL 等模型回傳標準格式：`{"data": [{"url": "...", "b64_json": "..."}]}`
- Nano Banana 系列回傳 chat completion 格式：`{"choices": [{"message": {"content": "..."}}]}`

**影響：** 開發者用一種格式解析，換模型就壞掉，而且完全不知道為什麼。這是我們花最多時間除錯的問題。

**建議：** 在 `/ai/image` 端點文件中，按模型分類列出回應格式差異：

```
### 回應格式

回應格式因模型而異：

#### FLUX / SDXL 模型
{"data": [{"url": "https://...", "b64_json": "..."}]}

#### Nano Banana 系列（Gemini Image）
{"choices": [{"message": {"content": "..."}}]}
圖片資料位於 content 中，可能是 base64 字串或圖片 URL。
```

---

## 問題 5：`/ai/image` 端點的必填欄位驗證規則不明確

**現狀：** 文件沒有明確說明哪些欄位是必填的。

**實際情況：** 即使使用 `messages` 格式，頂層仍然必須有 `model` 和 `prompt` 欄位，否則回傳 `400: model and prompt are required`。

**影響：** 開發者如果用 `messages` 格式送圖片編輯請求，可能只放 `model` + `messages` 而省略 `prompt`，導致 400 錯誤。

**建議：** 明確標示：

> `/ai/image` 端點的 `model` 和 `prompt` 為必填欄位。即使同時使用 `messages` 進行圖片編輯，仍然必須提供頂層的 `prompt`。

---

## 問題 6：Nano Banana 系列的模型名稱對應不清楚

**現狀：** 文件中「Nano Banana」這個名稱和實際的 model ID 之間的對應關係不夠清楚。

**影響：** 開發者知道要用「Nano Banana」但不確定該填 `google/gemini-2.5-flash-image` 還是 `google/gemini-3.1-flash-image-preview` 還是其他。

**建議：** 加一個清晰的對照表：

| 名稱 | Model ID | 備註 |
|------|----------|------|
| Nano Banana | `google/gemini-2.5-flash-image` | 基礎版 |
| Nano Banana 2 | `google/gemini-3.1-flash-image-preview` | 推薦，品質較好 |
| Nano Banana Pro | `google/gemini-3-pro-image-preview` | 最高品質，較慢 |

---

## 問題 7：圖片編輯（帶參考圖）的請求格式缺乏完整範例

**現狀：** 文件提到支援圖片編輯，但沒有針對 `/ai/image` 端點的完整範例。

**影響：** 開發者不確定在 `/ai/image` 帶參考圖時，應該同時放 `prompt` + `messages` + `modalities`，還是只放其中一部分。

**建議：** 提供完整的圖片編輯請求範例：

```json
{
  "project_id": "your-project-uuid",
  "user_id": "your-player-id",
  "model": "google/gemini-3.1-flash-image-preview",
  "prompt": "Transform this sketch into a clean pixel art sword",
  "messages": [
    {
      "role": "user",
      "content": [
        {"type": "text", "text": "Transform this sketch into a clean pixel art sword"},
        {"type": "image_url", "image_url": {"url": "data:image/png;base64,iVBOR..."}}
      ]
    }
  ],
  "modalities": ["image", "text"]
}
```

---

## 總結

以上問題的共通點是：**文件假設開發者已經知道 API 的隱含規則**。實際上，大部分開發者（包括 AI 輔助工具）都是第一次串接，需要的是「複製貼上就能跑」的範例，而不是需要推理才能組出正確請求的說明。

建議優先改善的項目（按影響程度排序）：

1. **每個端點加 curl 範例** — 效益最大，開發者 30 秒內就能驗證
2. **回應格式按模型分類說明** — 避免最難除錯的問題
3. **必填欄位明確標示** — 減少 400 錯誤
4. **模型名稱對照表** — 減少混淆
