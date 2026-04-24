using UnityEngine;

namespace NightPrincess.Akarion
{
    [CreateAssetMenu(menuName = "NightPrincess/Akarion Config", fileName = "AkarionConfig")]
    public class AkarionConfig : ScriptableObject
    {
        [Header("Credentials")]
        public string apiKey = "ak_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04";
        public string projectId = "449f5cca-fd8e-40c6-a38e-ff3aa587d8dc";
        public string playerId = "test_mo9xxz0z";
        public string displayName = "Night Princess Player";

        [Header("Endpoints")]
        public string baseUrl = "https://akarion.dev/api/v1";

        [Header("AI Models")]
        // Avoid pure reasoning models (openai/gpt-5, deepseek-r1) — they consume max_tokens on reasoning and return empty content.
        public string chatModel = "x-ai/grok-4.2";
        public string imageModel = "google/gemini-3.1-flash-image-preview";

        [Header("Princess Persona")]
        [TextArea(3, 10)]
        public string princessSystemPrompt =
            "你扮演一位高貴、傲嬌、有點任性的夜城公主，說話方式充滿公主氣質，常用「本公主」自稱。"
            + "你正在城堡中等待暗殺者救出你。請用繁體中文回覆，每次回覆控制在 60 字內，語氣可以驕傲、撒嬌或命令。";

        [Header("Item Generation Prompt")]
        [TextArea(3, 10)]
        public string itemGenerationPrompt =
            "這是玩家手繪的「{itemName}」草稿。請直接在這張原圖上進行延伸補強，絕對不要重新繪製或重新構圖。"
            + "必須保留玩家原本所有的線條、位置、比例、角度與輪廓不變，只在原有線條上加粗、補齊斷線、添加少量細節與筆觸，讓它看起來更完整。"
            + "風格維持玩家的簡筆畫手繪感，不要改成寫實風、卡通風或 3D 風。純白色（#FFFFFF）背景完全保留。"
            + "嚴格禁止：任何文字、標題、標籤、註解、箭頭、簽名、浮水印、裝飾文字、語言文字（中英日韓等）、名稱標註、背景元素、陰影背景。"
            + "最終輸出只能是原草圖的延伸精緻版。";
    }
}
