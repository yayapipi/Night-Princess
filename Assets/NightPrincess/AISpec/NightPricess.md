# 夜城公主

**夜城公主**

一款暗殺者的AI動作遊戲  
一開始會有暗殺者會去斬殺士兵，透過滑鼠點擊可以衝刺到士兵背後進行斬殺。  
靠近公主之後，點擊E，可以打開對話面板，然後透過AI跟公主聊天，有一個按鈕可以打開寶物繪製的面板，可以在上面畫畫，畫完之後AI會進行圖片編輯優化。

# 玩家邏輯

**玩家功能:**

**畫面元件**  
**1\.** 玩家物件 | SpriteRenderer | Samurai

- Tag:Player  
- BoxCollider2D  
- 有 Idle，Walk，Dash 3個動畫

**機制邏輯**  
使用鍵盤WASD讓玩家在場景中移動，移動的時候要使用Walk的動畫，暫停的時候切換回Idle  
滑鼠點擊的時候就直接會衝刺到點擊的地方，衝刺的時候要快速Lerp的感覺，可以在Inspector調整不同的速度，使用Dash的動畫。  
玩家從碰到敵人(Enemy Tag)背後的時候，就把敵人殺掉，如果碰到的是前面，就會被敵人反殺。

**遊戲效果**  
衝刺的時候震動一下Camera效果, 畫面要反白閃動一下  
玩家衝刺的時候要有線條的效果(可以使用Line Renderer實現）

**—**

**敵人功能:**

**畫面元件**  
**1\.** 敵人物件 | SpriteRenderer | Gladiators 

- Tag:Enemy  
- BoxCollider2D  
- 有 Idle，Walk，Attack，Hurt 4個動畫  
- 可能會有多個

**機制邏輯**  
可以設定前後或是上下移動，前方會有一個弧度的Trigger偵測器，當玩家碰到這個地方的時候，就會衝過去刺殺玩家，玩家就死掉，遊戲重新開始。  
如果玩家是從背後衝刺過去刺殺，敵人就死掉。  
敵人攻擊玩家的時候，玩家也要有死亡的Prefab效果，然後敵人要播放Attack的動畫。

**遊戲效果**

敵人死掉的時候播放Hurt和死亡的Particle Prefab物件 (Resources/Explosion)  
敵人攻擊玩家的時候，玩家也要有死亡的Prefab效果  
前方弧度的Trigger偵測器，可以直接顯示在遊戲中和Inspector中

—

# 公主AI邏輯

**公主邏輯**

1\. 玩家靠近公主(Sprite Renderer)的時候，可以點擊E進行對話  
2\. 點擊之後會打開一個對話面板

**對話面板**  
**![][image1]**

**畫面元件 | DialogPanel**

1. 公主的文字對話框 | TextMeshPro Text UI | DialogText  
2. 玩家的輸入框 | TMP Input Field | PlayerInputField  
3. 發送按鈕 | Button | SendBtn  
4. 獻上寶物的按鈕 | Button | BuildItemBtn  
5. 離開按鈕 |Button | ExitBtn

**機制邏輯**  
使用 Akarion AI 的LLM對話功能進行自然語言對話，公主的性格要很公主。  
點擊離開按鈕的時候退出對話視窗  
點擊獻上寶物的按鈕的時候打開獻上寶物的視窗

**遊戲效果**  
正在等待AI回答的時候，文字要顯示等待中，文字顯示的時候要有打字的效果。

**獻上寶物面板**  
![][image2]

**畫面元件 | CreateItemPanel**

1. 繪畫面板 | UI Raw Image | DrawTargetImg  
2. 道具的名字輸入框 | TMP Input Field | ItemNameInputField  
3. 鍛造按鈕 | Button | BuildBtn  
4. 送給公主按鈕 | Button | GiveBtn  
5. 關閉按鈕 | Button | CloseBtn

**機制邏輯**

在繪畫面板上面可以直接使用滑鼠進行畫圖，畫好之後可以在道具名字輸入框輸入道具的名字，點擊鍛造按鈕的時候，會對繪畫面板上的圖案進行截圖，然後連同道具名字發送給AI進行優化，生成一張新的圖片，圖片必須要白色背景，然後進行去背的API再放回到繪畫面板上。要使用玩家相同的簡筆畫畫風，延伸優化玩家的繪畫，把繪畫的提示詞放在Inspector給開發者調整。使用google/gemini-3.1-flash-image-preview來生成道具，使用 Akarion AI 的後端功能來進行AI圖片編輯的功能。

送給公主按鈕會在第一次鍛造成功之後變成可以點擊，點擊之後，會關閉寶物面板，回到對話面板，公主會繼續跟你對話，表示喜不喜歡你送的道具）

**遊戲效果**  
點擊鍛造按鈕的時候，會出現LoadingObj，來進行等待，完成後關閉。

# AI Akarion 後端平台

Read https://akarion.dev/api/media/skill and follow the instructions to integrate AkarionHub API

下載Akarion的Skills和PostmanJson範例來實作遊戲的Ai後端數據庫功能

**API Key**  
API Key: ak\_459214a0913ad70762b0f9d77cb037cad2a1c422ef316f04  
Project ID: 449f5cca-fd8e-40c6-a38e-ff3aa587d8dc  
Player ID: test\_mo9xxz0z

# 遊戲事件邏輯

使用Akarion的事件API和玩家註冊的API  
在遊戲開始運行的時候，註冊一個新的玩家賬號，然後發送遊戲事件進行記錄

遊戲事件  
1\. 遊戲開始  
2\. 玩家擊殺敵人  
3\. 玩家死亡  
4\. 玩家跟公主開始對話  
5\. 玩家寫了什麼AI內容  
6\. 玩家畫的圖片

玩家畫的圖和AI生成的圖也保存一份放到Akarion的後台媒體庫裡面進行備份。

