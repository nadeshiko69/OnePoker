import json
import boto3
import time

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def lambda_handler(event, context):
    body = json.loads(event['body'])
    room_code = body.get('roomCode')
    player_id = body.get('playerId')
    bet_action = body.get('betAction')  # 'call', 'raise', 'drop'
    bet_amount = body.get('betAmount', 0)  # レイズ時の金額
    
    try:
        # ルーム情報を取得
        response = table.get_item(Key={'roomcode': room_code})
        item = response.get('Item')
        
        if not item:
            return {
                'statusCode': 404,
                'body': json.dumps({'message': 'Room not found'})
            }
        
        # 親プレイヤーのBet完了を記録
        current_time = int(time.time())
        update_expression = 'SET parentBetComplete = :pbc, parentBetAction = :pba, parentBetAmount = :pba_amount, lastUpdateTime = :lut'
        expression_values = {
            ':pbc': True,
            ':pba': bet_action,
            ':pba_amount': bet_amount,
            ':lut': current_time
        }
        
        # レイズの場合は最小ベット額も更新
        if bet_action == 'raise':
            update_expression += ', minimumBetAmount = :mba'
            expression_values[':mba'] = bet_amount
        
        table.update_item(
            Key={'roomcode': room_code},
            UpdateExpression=update_expression,
            ExpressionAttributeValues=expression_values
        )
        
        return {
            'statusCode': 200,
            'body': json.dumps({
                'message': 'Bet completion notified successfully',
                'betAction': bet_action,
                'betAmount': bet_amount,
                'timestamp': current_time
            })
        }
        
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps({'message': f'Error: {str(e)}'})
        }
