夜城公主

一款暗殺者的AI動作遊戲
一開始會有暗殺者會去斬殺士兵，透過滑鼠點擊可以衝刺到士兵背後進行斬殺。
靠近公主之後，點擊E，可以打開對話面板，然後透過AI跟公主聊天，有一個按鈕可以打開寶物繪製的面板，可以在上面畫畫，畫完之後AI會進行圖片編輯優化。



玩家功能:

畫面元件
1. 玩家物件 | SpriteRenderer | Samurai
Tag:Player
BoxCollider2D
有 Idle，Walk，Dash 3個動畫

機制邏輯
使用鍵盤WASD讓玩家在場景中移動，移動的時候要使用Walk的動畫，暫停的時候切換回Idle
滑鼠點擊的時候就直接會衝刺到點擊的地方，衝刺的時候要快速Lerp的感覺，可以在Inspector調整不同的速度，使用Dash的動畫。
玩家從碰到敵人(Enemy Tag)背後的時候，就把敵人殺掉，如果碰到的是前面，就會被敵人反殺。玩家的動畫直接使用名字比對進行播放。
如果碰到Collider，而不是敵人的話，就會在它面前停下來。

遊戲效果
衝刺的時候震動一下Camera效果
玩家衝刺的時候要有線條的效果 (可以使用Line Renderer實現，衝刺的效果要帥一點）


—

敵人功能:


畫面元件
1. 敵人物件 | SpriteRenderer | Gladiators 
Tag:Enemy
BoxCollider2D
有 Idle，Walk，Attack，Hurt 4個動畫
可能會有多個

機制邏輯
可以設定前後或是上下移動，前方會有一個弧度的Trigger偵測器，當玩家碰到這個地方的時候，就會衝過去刺殺玩家，玩家就死掉，遊戲重新開始。
如果玩家是從背後衝刺過去刺殺，敵人就死掉。
敵人攻擊玩家的時候，玩家也要有死亡的Prefab效果，然後敵人要播放Attack的動畫。



遊戲效果

敵人死掉的時候播放Hurt和死亡的Particle Prefab物件 (Resources/Explosion)
敵人攻擊玩家的時候，玩家也要有死亡的Prefab效果
前方弧度的Trigger偵測器，可以直接顯示在遊戲中和Inspector中，Order Layer 設定成最高

—
