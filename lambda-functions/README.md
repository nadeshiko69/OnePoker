# OnePoker Online Battle Lambda Functions

このディレクトリには、OnePokerのオンライン対戦機能を実現するためのAWS Lambda関数が含まれています。

## 概要

オンライン対戦では、以下の3つのLambda関数を使用します：

1. **start-game**: ゲーム開始時にデッキをシャッフルしてカードを配布
2. **get-game-state**: ゲーム状態を取得（プレイヤーIDに応じて適切な情報のみ返す）
3. **update-game-state**: プレイヤーのアクション（カード配置、ベット、スキル使用）を処理

## アーキテクチャ

```
Unity Client ←→ API Gateway ←→ Lambda Functions ←→ DynamoDB
```

## デプロイ手順

### 1. 前提条件

- AWS CLIがインストールされている
- AWS認証情報が設定されている
- Python 3.9以上がインストールされている

### 2. インフラストラクチャのデプロイ

```bash
# CloudFormationスタックを作成
aws cloudformation create-stack \
  --stack-name onepoker-online-battle \
  --template-body file://cloudformation-template.yaml \
  --parameters ParameterKey=Environment,ParameterValue=dev \
  --capabilities CAPABILITY_NAMED_IAM

# デプロイ完了を待機
aws cloudformation wait stack-create-complete --stack-name onepoker-online-battle
```

### 3. Lambda関数のデプロイ

```bash
# 各関数をデプロイ
./deploy.sh start-game
./deploy.sh get-game-state
./deploy.sh update-game-state
```

## API エンドポイント

### ゲーム開始
- **URL**: `POST /game/start`
- **リクエスト**:
```json
{
  "roomCode": "123456",
  "player1Id": "player1",
  "player2Id": "player2"
}
```
- **レスポンス**:
```json
{
  "gameId": "game_1234567890_abc123",
  "roomCode": "123456",
  "player1Id": "player1",
  "player2Id": "player2",
  "currentTurn": "player1",
  "gamePhase": "card_placement",
  "player1Life": 10,
  "player2Life": 10
}
```

### ゲーム状態取得
- **URL**: `GET /game/state?gameId={gameId}&playerId={playerId}`
- **レスポンス**:
```json
{
  "gameId": "game_1234567890_abc123",
  "currentTurn": "player1",
  "gamePhase": "card_placement",
  "myCards": [12, 25],
  "myLife": 10,
  "myBetAmount": 0,
  "myCardPlaced": false,
  "opponentCardPlaced": false,
  "player1Life": 10,
  "player2Life": 10,
  "currentBet": 0
}
```

### ゲーム状態更新
- **URL**: `POST /game/update`
- **リクエスト**:
```json
{
  "gameId": "game_1234567890_abc123",
  "playerId": "player1",
  "actionType": "place_card",
  "actionData": {
    "cardId": 12
  }
}
```

## アクションタイプ

### place_card
カードを配置する
```json
{
  "actionType": "place_card",
  "actionData": {
    "cardId": 12
  }
}
```

### bet
ベットする
```json
{
  "actionType": "bet",
  "actionData": {
    "amount": 2
  }
}
```

### call
コールする（カード公開フェーズに移行）
```json
{
  "actionType": "call",
  "actionData": {}
}
```

### use_skill
スキルを使用する
```json
{
  "actionType": "use_skill",
  "actionData": {
    "skillType": "scan"
  }
}
```

## ゲームフェーズ

1. **card_placement**: カード配置フェーズ
2. **betting**: ベットフェーズ
3. **reveal**: カード公開フェーズ

## DynamoDBテーブル構造

### GameStates テーブル

| フィールド | 型 | 説明 |
|-----------|----|----|
| gameId | String | ゲームID（パーティションキー） |
| roomCode | String | ルームコード |
| player1Id | String | プレイヤー1のID |
| player2Id | String | プレイヤー2のID |
| deck | List | 残りのデッキ |
| player1Cards | List | プレイヤー1の手札 |
| player2Cards | List | プレイヤー2の手札 |
| player1Life | Number | プレイヤー1のライフ |
| player2Life | Number | プレイヤー2のライフ |
| currentBet | Number | 現在のベット額 |
| currentTurn | String | 現在のターンプレイヤー |
| gamePhase | String | ゲームフェーズ |
| player1CardPlaced | Boolean | プレイヤー1のカード配置済み |
| player2CardPlaced | Boolean | プレイヤー2のカード配置済み |
| player1BetAmount | Number | プレイヤー1のベット額 |
| player2BetAmount | Number | プレイヤー2のベット額 |
| player1PlacedCard | Number | プレイヤー1が配置したカードID |
| player2PlacedCard | Number | プレイヤー2が配置したカードID |
| createdAt | String | 作成日時 |
| updatedAt | String | 更新日時 |

## セキュリティ

- API GatewayでCORSを有効化
- Lambda関数で入力値検証を実装
- DynamoDBでアクセス制御を設定
- プレイヤー認証は別途実装が必要

## トラブルシューティング

### よくあるエラー

1. **Missing required parameters**: 必須パラメータが不足
2. **Game not found**: ゲームIDが存在しない
3. **Player not authorized**: プレイヤーがゲームに参加していない
4. **Not your turn**: 自分のターンではない
5. **Card not in hand**: 手札にないカードを配置しようとした

### ログの確認

```bash
# CloudWatchログを確認
aws logs describe-log-groups --log-group-name-prefix "/aws/lambda/start-game"
aws logs tail /aws/lambda/start-game-dev --follow
```

## 開発者向け情報

### ローカルテスト

```bash
# テストイベントを作成
echo '{
  "body": "{\"roomCode\":\"123456\",\"player1Id\":\"player1\",\"player2Id\":\"player2\"}"
}' > test-event.json

# ローカルでテスト
python start-game/index.py
```

### 新しい関数の追加

1. 関数ディレクトリを作成
2. `index.py`を作成
3. `deploy.sh`に追加
4. CloudFormationテンプレートに追加
5. デプロイ 