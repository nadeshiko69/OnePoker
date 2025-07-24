import json
import boto3
import os
from typing import Dict, Any
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
# 環境変数から取得、なければFriendMatchRoomを使用
table_name = os.environ.get('TABLE_NAME', 'FriendMatchRoom')
table = dynamodb.Table(table_name)

def convert_decimals(obj):
    """
    DynamoDBのDecimal型をJSONシリアライズ可能な型に変換
    """
    if isinstance(obj, Decimal):
        return int(obj) if obj % 1 == 0 else float(obj)
    elif isinstance(obj, dict):
        return {key: convert_decimals(value) for key, value in obj.items()}
    elif isinstance(obj, list):
        return [convert_decimals(item) for item in obj]
    else:
        return obj

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    マッチング状態をチェックするLambda関数
    ルームコードを指定してマッチング状態を取得
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        print('Table name:', table_name)
        
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
        
        print('DynamoDB response:', json.dumps(convert_decimals(response), indent=2))
        
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
        
        # Decimal型を変換
        response_data = convert_decimals(response_data)
        
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