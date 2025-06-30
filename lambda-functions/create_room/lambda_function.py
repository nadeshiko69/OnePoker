import json
import boto3
import random
import time

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def generate_unique_room_code(max_retries=5):
    for _ in range(max_retries):
        code = str(random.randint(100000, 999999))
        response = table.get_item(Key={'roomcode': code})
        if 'Item' not in response:
            return code
    return None

def lambda_handler(event, context):
    body = json.loads(event['body'])
    host_player_id = body.get('playerId')
    
    if not host_player_id:
        return {
            'statusCode': 400,
            'body': json.dumps({'error': 'playerId is required'})
        }

    room_code = generate_unique_room_code()
    if not room_code:
        return {
            'statusCode': 500,
            'body': json.dumps({'error': 'Failed to generate unique room code'})
        }

    timestamp = int(time.time())
    ttl_timestamp = timestamp + 15 * 60  # 15分後に自動削除

    item = {
        'roomcode': room_code,
        'hostPlayerId': host_player_id,
        'status': 'waiting',
        'createdAt': timestamp,
        'ttl': ttl_timestamp
    }

    table.put_item(Item=item)

    return {
        'statusCode': 200,
        'body': json.dumps({'code': room_code})
    }
