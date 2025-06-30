import json
import boto3
import hashlib

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('Users')

def lambda_handler(event, context):
    try:
        body = json.loads(event['body'])
        username = body['username']
        password = body['password']

        # ユーザー検索
        response = table.scan(
            FilterExpression='username = :u',
            ExpressionAttributeValues={':u': username}
        )
        items = response.get('Items', [])
        if not items:
            return response_json(401, {'message': 'ユーザーが見つかりません'})

        user = items[0]
        password_hash = hashlib.sha256(password.encode('utf-8')).hexdigest()
        if user['password_hash'] != password_hash:
            return response_json(401, {'message': 'パスワードが違います'})

        # 認証成功
        return response_json(200, {'message': 'ログイン成功', 'user_id': user['user_id']})

    except Exception as e:
        return response_json(500, {'message': 'サーバーエラー', 'error': str(e)})

def response_json(status_code, body):
    return {
        'statusCode': status_code,
        'headers': {'Content-Type': 'application/json'},
        'body': json.dumps(body)
    }