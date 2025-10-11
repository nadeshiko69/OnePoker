# 複数ラウンドシステム実装ガイド

## 📋 概要

複数ラウンドに対応し、ゲームを連続してプレイできるようにします。

---

## 🎯 実装機能

### **1. ラウンド進行**
- Revealフェーズ終了後、3秒待ってから次のSetPhaseに自動遷移
- ラウンド番号を+1
- ゲーム状態をリセット

### **2. カード管理**
- SetZoneのカードを削除
- 使用済みカードをデッキに戻してシャッフル
- 各プレイヤーに1枚ずつ配布（2枚に戻す）

### **3. 親子交代**
- **1-3ラウンド**: Player1が親
- **4-6ラウンド**: Player2が親
- **7-9ラウンド**: Player1が親
- **10-12ラウンド**: Player2が親
- ...以降繰り返し

### **4. スキルリセット**
- 使用済スキルをクリア
- 次のラウンドで再度使用可能に

---

## 🛠️ 実装ファイル

### **サーバー側 (AWS Lambda)**

#### **1. start-game/lambda_function.py**
- **追加フィールド**: `currentRound`, `currentDealer`
- **初期値**: `currentRound=1`, `currentDealer='P1'`

#### **2. next-round/lambda_function.py** (新規)
- **機能**: 次のラウンドに進む処理
- **処理内容**:
  1. ラウンド番号を+1
  2. 親を決定（3ラウンドごとに交代）
  3. 使用済みカードをデッキに戻す
  4. デッキをシャッフル
  5. 各プレイヤーに1枚配布
  6. ゲーム状態をリセット

---

### **クライアント側 (Unity)**

#### **1. HttpManager.cs**
- **追加メソッド**: `NextRound()`
- **追加クラス**: `NextRoundRequest`, `NextRoundResponse`

#### **2. OnlineGameManager.cs**
- **追加メソッド**:
  - `WaitAndProceedToNextRound()`: 3秒待機
  - `StartNextRound()`: サーバーにリクエスト
  - `OnNextRoundSuccess()`: 成功時の処理
  - `OnNextRoundError()`: エラー処理
  - `ClearSetZone()`: SetZoneのカードを削除

---

## 📊 処理フロー

```
【Reveal Phase終了】
  ↓
勝敗表示（ResultView）
  ↓
3秒待機
  ↓
OnlineGameManager.StartNextRound()
  ↓
HttpManager.NextRound() → Lambda関数呼び出し
  ↓
【Lambda: next-round】
  ├─ ラウンド+1
  ├─ 親決定
  ├─ 使用済カードをデッキに戻す
  ├─ デッキシャッフル
  ├─ カード配布
  └─ DynamoDB更新
  ↓
OnlineGameManager.OnNextRoundSuccess()
  ├─ ゲームデータ更新
  ├─ 手札再設定
  ├─ SetZoneクリア
  ├─ スキルリセット
  └─ フェーズ監視再開
  ↓
【Set Phase開始】
```

---

## 🔧 親決定ロジック

```python
# Lambda関数内
if ((next_round - 1) // 3) % 2 == 0:
    new_dealer = 'P1'
else:
    new_dealer = 'P2'
```

**計算例:**
| ラウンド | `(round-1)//3` | `%2` | 親 |
|---------|---------------|------|-----|
| 1 | 0 | 0 | P1 |
| 2 | 0 | 0 | P1 |
| 3 | 0 | 0 | P1 |
| 4 | 1 | 1 | P2 |
| 5 | 1 | 1 | P2 |
| 6 | 1 | 1 | P2 |
| 7 | 2 | 0 | P1 |
| 8 | 2 | 0 | P1 |
| 9 | 2 | 0 | P1 |

---

## 🚀 デプロイ手順

### **Step 1: start-game Lambda更新**

既存のLambda関数を更新（`currentRound`, `currentDealer`追加）

```powershell
cd lambda-functions/start-game
aws lambda update-function-code --function-name start-game --zip-file fileb://function.zip
```

### **Step 2: next-round Lambda作成**

```powershell
cd lambda-functions/next-round
.\deploy.ps1
```

### **Step 3: API Gateway設定**

1. **POST /next-round エンドポイント作成**
2. **Lambda統合**: `next-round`関数を選択
3. **CORS有効化**
4. **APIデプロイ**

---

## 🧪 テスト手順

### **1. 1ラウンド目プレイ**
- SetPhase: カード配置
- BettingPhase: ベット
- RevealPhase: 勝敗表示

### **2. 自動遷移確認**
- Reveal表示後3秒待機
- 自動的にSetPhaseに戻る

### **3. カード状態確認**
- SetZoneが空になっている
- 手札が2枚になっている
- スキルボタンが使用可能になっている

### **4. 親交代確認**
- 4ラウンド目で親が切り替わる

---

## 📝 ログ確認ポイント

### **Unity Console:**
```
[OnlineGameManager] Waiting 3 seconds before next round...
[OnlineGameManager] Proceeding to next round
[OnlineGameManager] Next round success: Round 2, Dealer: P1
[OnlineGameManager] My SetZone cleared
[OnlineGameManager] Opponent SetZone cleared
[OnlineGameManager] Next round setup complete - Round 2
```

### **Lambda CloudWatch:**
```
Game game_xxx: Advancing from round 1 to 2
Round 2: Dealer is P1
Used cards this round: [5, 12]
Deck shuffled. Deck size: 50
Cards dealt. Player1: [3, 8], Player2: [15, 22]
Game state updated for round 2
```

---

## ⚠️ 注意事項

1. **SetZoneのGameObject名**
   - `MySetZone`
   - `OpponentSetZone`
   - これらの名前が正しく設定されているか確認

2. **デッキ枚数**
   - 52枚デッキを想定
   - 長期戦になるとカード不足の可能性
   - 必要に応じてデッキ再生成処理を追加

3. **ライフポイント**
   - 現在は減らない仕様
   - 勝敗判定でライフを減らす処理は別途実装

---

## 🎮 今後の拡張

- [ ] ライフポイント減少処理
- [ ] ゲーム終了判定（ライフ0）
- [ ] ラウンド数制限
- [ ] リザルト画面（最終結果）
- [ ] リプレイ機能

---

**作成日:** 2025-10-10  
**ステータス:** 実装完了（テスト待ち）


