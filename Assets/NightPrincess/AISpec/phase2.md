Read https://akarion.dev/api/media/skill and follow the instructions to integrate AkarionHub API

下載Akarion的Skills和PostmanJson範例來實作遊戲的Ai後端數據庫功能

API Key
API Key: ak_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04
Project ID: 449f5cca-fd8e-40c6-a38e-ff3aa587d8dc
Player ID: test_mo9xxz0z

---

公主邏輯

1. 玩家靠近公主(Sprite Renderer)的時候，可以點擊E進行對話
2. 點擊之後會打開一個對話面板


對話面板


畫面元件 | DialogPanel | 一開始會是關閉狀態
公主的文字對話框 | TextMeshPro Text UI | DialogText
玩家的輸入框 | TMP Input Field | PlayerInputField
發送按鈕 | Button | SendBtn
獻上寶物的按鈕 | Button | BuildItemBtn
離開按鈕 |Button | ExitBtn

機制邏輯
使用 Akarion AI 的LLM對話功能進行自然語言對話，公主的性格要很公主。
點擊離開按鈕的時候退出對話視窗
點擊獻上寶物的按鈕的時候打開獻上寶物的視窗
輸入框Submit的時候就自動發送對話給AI
在Inspector 可以選擇要使用哪個 LLM 模型

遊戲效果
正在等待AI回答的時候，文字要顯示等待中，文字顯示的時候要有打字的效果。



獻上寶物面板


畫面元件 | CreateItemPanel | 一開始會是關閉狀態
繪畫面板 | UI Raw Image | DrawTargetImg
道具的名字輸入框 | TMP Input Field | ItemNameInputField
鍛造按鈕 | Button | BuildBtn
送給公主按鈕 | Button | GiveBtn
關閉按鈕 | Button | CloseBtn

機制邏輯

在繪畫面板上面可以直接使用滑鼠進行畫圖，畫好之後可以在道具名字輸入框輸入道具的名字，點擊鍛造按鈕的時候，會對繪畫面板上的圖案進行截圖，然後連同道具名字發送給AI進行優化，生成一張新的圖片，圖片必須要白色背景，然後再放回到繪畫面板上。要延伸玩家的繪畫，簡筆畫風，不要重新畫圖，把繪畫的提示詞放在Inspector給開發者調整。使用google/gemini-3.1-flash-image-preview來生成道具，使用 Akarion AI 的後端功能來進行AI圖片編輯的功能。

送給公主按鈕會在第一次鍛造成功之後變成可以點擊，點擊之後，會關閉寶物面板，回到對話面板，公主會繼續跟你對話，表示喜不喜歡你送的道具，公主的圖片辨識LLM可以統一使用 x-ai/grok-4.1-fast


遊戲效果
點擊鍛造按鈕的時候，會出現LoadingObj，來進行等待，完成後關閉。
