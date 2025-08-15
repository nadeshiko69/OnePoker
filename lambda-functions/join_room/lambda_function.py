import json
import boto3

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def lambda_handler(event, context):
    body = json.loads(event['body'])
    room_code = body.get('code')
    guest_player_id = body.get('playerId')

    # ルーム存在チェック
    response = table.get_item(Key={'roomcode': room_code})
    item = response.get('Item')

    if not item:
        return {
            'statusCode': 404,
            'body': json.dumps({ 'message': 'Room not found' })
        }

    if item['status'] != 'waiting':
        return {
            'statusCode': 400,
            'body': json.dumps({ 'message': 'Room already matched' })
        }

    # ルームを更新（ゲスト情報とステータス）
    table.update_item(
        Key={'roomcode': room_code},
        UpdateExpression='SET guestPlayerId = :g, #s = :s',
        ExpressionAttributeNames={ '#s': 'status' },
        ExpressionAttributeValues={
            ':g': guest_player_id,
            ':s': 'matched'
        }
    )

    # マッチング成功時にプレイヤーIDも返す
    return {
        'statusCode': 200,
        'body': json.dumps({ 
            'message': 'Matched successfully',
            'player1Id': item['hostPlayerId'],  # ホストプレイヤーID
            'player2Id': guest_player_id        # ゲストプレイヤーID
        })
    }