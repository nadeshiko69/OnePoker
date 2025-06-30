import boto3
import json

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('FriendMatchRoom')

def lambda_handler(event, context):
    body = json.loads(event['body'])
    room_code = body.get('roomcode')

    try:
        response = table.delete_item(
            Key={
                'roomcode': room_code
            }
        )
        return {
            'statusCode': 200,
            'body': json.dumps({ 'message': 'Room canceled' })
        }
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps({ 'error': str(e) })
        }