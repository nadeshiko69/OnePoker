import json
import boto3

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def lambda_handler(event, context):
    body = json.loads(event['body'])
    room_code = body.get('roomCode')
    
    try:
        # ルーム情報を取得
        response = table.get_item(Key={'roomcode': room_code})
        item = response.get('Item')
        
        if not item:
            return {
                'statusCode': 404,
                'body': json.dumps({'message': 'Room not found'})
            }
        
        # 親のBet完了状態を返す
        parent_bet_complete = item.get('parentBetComplete', False)
        parent_bet_action = item.get('parentBetAction', '')
        parent_bet_amount = item.get('parentBetAmount', 0)
        minimum_bet_amount = item.get('minimumBetAmount', 0)
        
        return {
            'statusCode': 200,
            'body': json.dumps({
                'parentBetComplete': parent_bet_complete,
                'parentBetAction': parent_bet_action,
                'parentBetAmount': parent_bet_amount,
                'minimumBetAmount': minimum_bet_amount,
                'message': 'Parent bet status retrieved successfully'
            })
        }
        
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps({'message': f'Error: {str(e)}'})
        }
