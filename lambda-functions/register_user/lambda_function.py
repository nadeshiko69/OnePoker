import json
import boto3
import uuid
import hashlib

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('Users')  # 事前にDynamoDBテーブル「Users」を作成しておく

def lambda_handler(event, context):
    try:
        body = json.loads(event['body'])
        username = body['username']
        email = body['email']
        password = body['password']

        # 入力バリデーション
        if not username or not email or not password:
            return response(400, {'message': '全ての項目を入力してください。'})

        # パスワードをハッシュ化（SHA-256例）
        password_hash = hashlib.sha256(password.encode('utf-8')).hexdigest()

        # ユーザID生成
        user_id = str(uuid.uuid4())

        # DynamoDBに登録
        table.put_item(
            Item={
                'user_id': user_id,
                'username': username,
                'email': email,
                'password_hash': password_hash
            }
        )

        return response(200, {'message': 'ユーザ登録成功', 'user_id': user_id})

    except Exception as e:
        return response(500, {'message': 'サーバエラー', 'error': str(e)})

def response(status_code, body):
    return {
        'statusCode': status_code,
        'headers': {'Content-Type': 'application/json'},
        'body': json.dumps(body)
    }