import json
import boto3
import os
from typing import Dict, Any

dynamodb = boto3.resource('dynamodb')
# 環境変数から取得、なければFriendMatchRoomを使用
table_name = os.environ.get('TABLE_NAME', 'FriendMatchRoom')
table = dynamodb.Table(table_name)

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    マッチング状態をチェックするLambda関数
    ルームコードを指定してマッチング状態を取得
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        print('Table name:', table_name)
        
        # テーブルの存在確認
        try:
            table.load()
            print(f'Table {table_name} exists')
        except Exception as table_error:
            print(f'Table {table_name} does not exist or cannot be accessed: {str(table_error)}')
            return {
                'statusCode': 500,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'GET, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Table not found or inaccessible',
                    'table_name': table_name,
                    'message': str(table_error)
                })
            }
        
        # クエリパラメータからルームコードを取得
        query_params = event.get('queryStringParameters', {}) or {}
        room_code = query_params.get('roomCode')
        
        print('Room code:', room_code)
        
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
        print(f'Querying DynamoDB table {table_name} with key: {{"roomcode": "{room_code}"}}')
        response = table.get_item(Key={'roomcode': room_code})
        
        print('DynamoDB response:', json.dumps(response, indent=2))
        
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
                    'error': 'Room not found',
                    'room_code': room_code
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
        
        print('Response data:', json.dumps(response_data, indent=2))
        
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
        import traceback
        print('Traceback:', traceback.format_exc())
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
                'message': str(error),
                'table_name': table_name
            })
        } 