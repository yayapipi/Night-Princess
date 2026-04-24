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
            "以玩家相同的簡筆畫風格，將這張草圖延伸優化為一件名為「{itemName}」的精緻奇幻道具插畫。"
            + "線條清晰，純白色（#FFFFFF）背景，道具主體置中。"
            + "嚴格禁止：任何文字、標題、標籤、註解、箭頭、簽名、浮水印、裝飾文字、語言文字（中英日韓等）、說明文字、頁碼、名稱標註。"
            + "畫面只允許出現道具本身的線條，其他區域全部保持純白，不要任何背景元素。";
    }
}
