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
    update_expr = []
    expr_attr = {}
    if player_id == item.get('player1Id'):
        update_expr.append('SET player1Set = :true, player1CardValue = :cv')
        expr_attr[':true'] = True
        expr_attr[':cv'] = card_value
    elif player_id == item.get('player2Id'):
        update_expr.append('SET player2Set = :true, player2CardValue = :cv')
        expr_attr[':true'] = True
        expr_attr[':cv'] = card_value
    else:
        return {
            'statusCode': 400,
            'body': json.dumps({'message': 'Invalid playerId'})
        }

    # 3. 両者セット済みならgamePhaseをrevealに
    player1_set = item.get('player1Set', False) or (player_id == item.get('player1Id'))
    player2_set = item.get('player2Set', False) or (player_id == item.get('player2Id'))
    if player1_set and player2_set:
        update_expr.append('SET gamePhase = :reveal')
        expr_attr[':reveal'] = 'reveal'

    # 4. 更新実行
    update_expression = ', '.join(update_expr)
    table.update_item(
        Key={'gameId': game_id},
        UpdateExpression=update_expression,
        ExpressionAttributeValues=expr_attr
    )

    # 5. 最新状態を返却
    response = table.get_item(Key={'gameId': game_id})
    item = response.get('Item')
    return {
        'statusCode': 200,
        'body': json.dumps(convert_decimals(item))
    } 