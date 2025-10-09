import json
import boto3
import time
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
game_table = dynamodb.Table('GameStates')

def lambda_handler(event, context):
    """
    スキル使用処理のLambda関数
    プレイヤーが使用したスキルをDynamoDBに記録
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        game_id = body.get('gameId')
        player_id = body.get('playerId')
        skill_type = body.get('skillType')  # "Scan", "Change", "Obstruct", "FakeOut", "Copy"
        
        # 必須パラメータの検証
        if not all([game_id, player_id, skill_type]):
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameters'
                })
            }
        
        # 有効なスキルタイプか確認
        valid_skills = ['Scan', 'Change', 'Obstruct', 'FakeOut', 'Copy']
        if skill_type not in valid_skills:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': f'Invalid skill type. Must be one of: {valid_skills}'
                })
            }
        
        # ゲーム状態を取得
        response = game_table.get_item(Key={'gameId': game_id})
        
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
        
        # プレイヤーの識別
        is_player1 = (player_id == game_state['player1Id'])
        
        if not is_player1 and player_id != game_state['player2Id']:
            return {
                'statusCode': 403,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Player not in this game'
                })
            }
        
        # 使用済スキルリストを取得
        skill_list_key = 'player1UsedSkills' if is_player1 else 'player2UsedSkills'
        used_skills = game_state.get(skill_list_key, [])
        
        # 既に使用済みか確認
        if skill_type in used_skills:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': f'Skill {skill_type} already used'
                })
            }
        
        # スキルを使用済リストに追加
        used_skills.append(skill_type)
        
        # DynamoDBを更新
        current_time = int(time.time())
        game_table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=f'SET {skill_list_key} = :skills, updatedAt = :time',
            ExpressionAttributeValues={
                ':skills': used_skills,
                ':time': current_time
            }
        )
        
        print(f"Skill {skill_type} used by player {player_id} in game {game_id}")
        
        # レスポンスを作成
        response_data = {
            'success': True,
            'gameId': game_id,
            'playerId': player_id,
            'skillType': skill_type,
            'usedSkills': used_skills
        }
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
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
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps({
                'error': 'Internal server error',
                'message': str(error)
            })
        }

