import json
import boto3
from typing import Dict, Any

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    マッチング状態をチェックするLambda関数
    ルームコードを指定してマッチング状態を取得
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # クエリパラメータからルームコードを取得
        room_code = event['queryStringParameters'].get('roomCode')
        
        # 必須パラメータの検証
        if not room_code:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'GET, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameter: roomCode'
                })
            }
        
        # DynamoDBからルーム情報を取得
        response = table.get_item(Key={'roomcode': room_code})
        
        if 'Item' not in response:
            return {
                'statusCode': 404,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'GET, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Room not found'
                })
            }
        
        room_data = response['Item']
        
        # レスポンスデータを作成
        response_data = {
            'roomCode': room_data['roomcode'],
            'status': room_data['status'],
            'hostPlayerId': room_data['hostPlayerId']
        }
        
        # ゲストプレイヤーIDが存在する場合は追加
        if 'guestPlayerId' in room_data:
            response_data['guestPlayerId'] = room_data['guestPlayerId']
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET, OPTIONS'
            },
            'body': json.dumps(response_data)
        }
        
    except Exception as error:
        print('Error:', str(error))
        return {
            'statusCode': 500,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET, OPTIONS'
            },
            'body': json.dumps({
                'error': 'Internal server error',
                'message': str(error)
            })
        } 