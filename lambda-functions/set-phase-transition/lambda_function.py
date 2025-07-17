import json
import boto3
import time
from typing import Dict, Any

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    フェーズ移行時間を設定するLambda関数
    Unity側の監視開始時に呼び出される
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        game_id = body.get('gameId')
        player_id = body.get('playerId')
        transition_delay = body.get('transitionDelay', 3)  # デフォルト3秒
        
        # 必須パラメータの検証
        if not game_id or not player_id:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameters: gameId, playerId'
                })
            }
        
        # DynamoDBから現在のゲーム状態を取得
        response = table.get_item(Key={'gameId': game_id})
        
        if 'Item' not in response:
            return {
                'statusCode': 404,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Game not found'
                })
            }
        
        game_state = response['Item']
        
        # プレイヤーがこのゲームに参加しているかチェック
        if player_id not in [game_state['player1Id'], game_state['player2Id']]:
            return {
                'statusCode': 403,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Player not authorized to access this game'
                })
            }
        
        # 現在時刻を取得
        current_time = int(time.time())
        
        # フェーズ移行時間を設定
        phase_transition_time = current_time + transition_delay
        
        print(f"Game {game_id}: Setting phase transition time to {phase_transition_time} (current_time={current_time}, delay={transition_delay})")
        
        # DynamoDBを更新
        update_expression = 'SET phaseTransitionTime = :phaseTransitionTime, updatedAt = :updatedAt'
        expression_attribute_values = {
            ':phaseTransitionTime': phase_transition_time,
            ':updatedAt': current_time
        }
        
        table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=update_expression,
            ExpressionAttributeValues=expression_attribute_values
        )
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps({
                'success': True,
                'phaseTransitionTime': phase_transition_time,
                'message': f'Phase transition time set to {transition_delay} seconds from now'
            })
        }
        
    except Exception as error:
        print('Error:', str(error))
        return {
            'statusCode': 500,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps({
                'error': 'Internal server error',
                'message': str(error)
            })
        } 