import json
import boto3
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
TABLE_NAME = 'GameStates'

def convert_decimals(obj):
    if isinstance(obj, list):
        return [convert_decimals(i) for i in obj]
    elif isinstance(obj, dict):
        return {k: convert_decimals(v) for k, v in obj.items()}
    elif isinstance(obj, Decimal):
        if obj % 1 == 0:
            return int(obj)
        else:
            return float(obj)
    else:
        return obj

def lambda_handler(event, context):
    print("Received event:", event)
    table = dynamodb.Table(TABLE_NAME)
    body = event.get('body')
    if body and isinstance(body, str):
        body = json.loads(body)
    elif not body:
        body = event

    game_id = body['gameId']
    player_id = body['playerId']
    card_value = body['cardValue']

    # 1. 現在のゲーム状態を取得
    response = table.get_item(Key={'gameId': game_id})
    item = response.get('Item')
    if not item:
        return {
            'statusCode': 404,
            'body': json.dumps({'message': 'Game not found'})
        }

    # 2. プレイヤーごとにセット済みフラグとカード値を更新
    update_attrs = {}
    expr_attr = {}
    
    if player_id == item.get('player1Id'):
        update_attrs['player1Set'] = True
        update_attrs['player1CardValue'] = card_value
        update_attrs['player1CardPlaced'] = True
        print(f"Player1 card set: cardValue={card_value}, player1Set=True, player1CardPlaced=True")
    elif player_id == item.get('player2Id'):
        update_attrs['player2Set'] = True
        update_attrs['player2CardValue'] = card_value
        update_attrs['player2CardPlaced'] = True
        print(f"Player2 card set: cardValue={card_value}, player2Set=True, player2CardPlaced=True")
    else:
        return {
            'statusCode': 400,
            'body': json.dumps({'message': 'Invalid playerId'})
        }

    # 3. 両者セット済みならgamePhaseをbettingに
    # 現在のプレイヤーがセットした後の状態を計算
    if player_id == item.get('player1Id'):
        player1_set = True  # 今セットしたのでTrue
        player2_set = item.get('player2Set', False)  # 相手の状態
    else:  # player2Idの場合
        player1_set = item.get('player1Set', False)  # 相手の状態
        player2_set = True  # 今セットしたのでTrue
    
    print(f"Player1Set: {player1_set}, Player2Set: {player2_set}")
    
    if player1_set and player2_set:
        print("Both players have set their cards, transitioning to betting phase")
        update_attrs['gamePhase'] = 'betting'  # revealではなくbettingに変更
    else:
        print(f"Waiting for opponent. Player1Set: {player1_set}, Player2Set: {player2_set}")

    # 4. 更新実行
    update_expression = 'SET ' + ', '.join([f'#{k} = :{k}' for k in update_attrs.keys()])
    expression_attribute_names = {f'#{k}': k for k in update_attrs.keys()}
    expression_attribute_values = {f':{k}': v for k, v in update_attrs.items()}
    table.update_item(
        Key={'gameId': game_id},
        UpdateExpression=update_expression,
        ExpressionAttributeNames=expression_attribute_names,
        ExpressionAttributeValues=expression_attribute_values
    )

    # 5. 最新状態を返却
    response = table.get_item(Key={'gameId': game_id})
    item = response.get('Item')
    
    # player2Setフィールドが存在しない場合はFalseを設定
    if 'player2Set' not in item:
        item['player2Set'] = False
        print(f"Added missing player2Set field: {item['player2Set']}")
    
    # Decimal型をint/floatに変換
    item = convert_decimals(item)
    
    print(f"Final game state - gamePhase: {item.get('gamePhase')}, player1Set: {item.get('player1Set')}, player2Set: {item.get('player2Set')}")
    print(f"Final game state - player1CardPlaced: {item.get('player1CardPlaced')}, player2CardPlaced: {item.get('player2CardPlaced')}")
    print(f"Final game state - player1CardValue: {item.get('player1CardValue')}, player2CardValue: {item.get('player2CardValue')}")
    
    return {
        'statusCode': 200,
        'body': json.dumps(item)
    } 