import json
import boto3
import time
from decimal import Decimal
from typing import Dict, Any

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def convert_decimals(obj):
    """Decimal型をint/floatに変換する"""
    if isinstance(obj, list):
        return [convert_decimals(i) for i in obj]
    elif isinstance(obj, dict):
        return {k: convert_decimals(v) for k, v in obj.items()}
    elif isinstance(obj, Decimal):
        if obj % 1 == 0:
            return int(obj)
        else:
            return float(obj)
    else:
        return obj

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    ベットアクション（Call/Raise/Drop）を処理するLambda関数
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        game_id = body.get('gameId')
        player_id = body.get('playerId')
        action_type = body.get('actionType')  # "call", "raise", "drop"
        bet_value = body.get('betValue', 1)
        
        # 必須パラメータの検証
        if not game_id or not player_id or not action_type:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameters: gameId, playerId, actionType'
                })
            }
        
        # アクションタイプの検証
        if action_type not in ['call', 'raise', 'drop']:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Invalid actionType. Must be call, raise, or drop'
                })
            }
        
        # ゲーム状態を取得
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
        
        # ゲームフェーズがbettingかチェック
        if game_state['gamePhase'] != 'betting':
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Game is not in betting phase'
                })
            }
        
        # プレイヤー情報を取得
        is_player1 = player_id == game_state['player1Id']
        opponent_id = game_state['player2Id'] if is_player1 else game_state['player1Id']
        
        print(f"Player {player_id} ({'player1' if is_player1 else 'player2'}) performing {action_type} with bet value {bet_value}")
        
        # ベットアクションを処理
        update_attrs = {}
        expression_attribute_names = {}
        expression_attribute_values = {}
        
        current_time = int(time.time())
        
        if action_type == 'drop':
            # ドロップの場合
            if is_player1:
                update_attrs['player1Dropped'] = True
                update_attrs['gamePhase'] = 'reveal'
                update_attrs['winner'] = game_state['player2Id']
            else:
                update_attrs['player2Dropped'] = True
                update_attrs['gamePhase'] = 'reveal'
                update_attrs['winner'] = game_state['player1Id']
            
            print(f"Player {player_id} dropped. Game phase changed to reveal. Winner: {update_attrs['winner']}")
            
        elif action_type in ['call', 'raise']:
            # コールまたはレイズの場合
            if is_player1:
                update_attrs['player1BetAmount'] = bet_value
                update_attrs['player1LastAction'] = action_type
                update_attrs['player1LastActionTime'] = current_time
            else:
                update_attrs['player2BetAmount'] = bet_value
                update_attrs['player2LastAction'] = action_type
                update_attrs['player2LastActionTime'] = current_time
            
            # 相手のアクションをチェック
            opponent_bet_amount = game_state.get('player1BetAmount' if not is_player1 else 'player2BetAmount', 0)
            opponent_last_action = game_state.get('player1LastAction' if not is_player1 else 'player2LastAction')
            
            # 両者がアクション済みの場合
            if opponent_last_action and opponent_last_action in ['call', 'raise']:
                # 両者がコールした場合、またはレイズの応答が完了した場合
                if (action_type == 'call' and opponent_last_action == 'call') or \
                   (action_type == 'call' and opponent_last_action == 'raise' and bet_value >= opponent_bet_amount):
                    update_attrs['gamePhase'] = 'reveal'
                    print(f"Both players have acted. Game phase changed to reveal.")
            
            print(f"Player {player_id} performed {action_type} with bet value {bet_value}")
        
        # 更新時間を設定
        update_attrs['updatedAt'] = current_time
        
        # DynamoDBを更新
        update_expression = 'SET ' + ', '.join([f'#{k} = :{k}' for k in update_attrs.keys()])
        expression_attribute_names = {f'#{k}': k for k in update_attrs.keys()}
        expression_attribute_values = {f':{k}': v for k, v in update_attrs.items()}
        
        table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=update_expression,
            ExpressionAttributeNames=expression_attribute_names,
            ExpressionAttributeValues=expression_attribute_values
        )
        
        # 最新のゲーム状態を取得
        response = table.get_item(Key={'gameId': game_id})
        updated_game_state = response['Item']
        
        # レスポンスを作成
        response_data = {
            'gameId': game_id,
            'gamePhase': updated_game_state['gamePhase'],
            'isMyTurn': False,  # アクション実行後は相手のターン
            'currentBet': bet_value,
            'message': f'Successfully performed {action_type}'
        }
        
        # 相手のアクション情報を追加（相手が既にアクション済みの場合）
        if not is_player1 and 'player1LastAction' in updated_game_state:
            response_data['opponentAction'] = {
                'actionType': updated_game_state['player1LastAction'],
                'betValue': updated_game_state.get('player1BetAmount', 0),
                'playerId': game_state['player1Id']
            }
        elif is_player1 and 'player2LastAction' in updated_game_state:
            response_data['opponentAction'] = {
                'actionType': updated_game_state['player2LastAction'],
                'betValue': updated_game_state.get('player2BetAmount', 0),
                'playerId': game_state['player2Id']
            }
        
        print(f"Bet action response: {json.dumps(response_data, indent=2)}")
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps(convert_decimals(response_data))
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