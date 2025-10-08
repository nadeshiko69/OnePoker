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
    if isinstance(obj, dict):
        return {k: convert_decimals(v) for k, v in obj.items()}
    if isinstance(obj, Decimal):
        try:
            # 小数点を含まない場合はint、含む場合はfloatへ
            return int(obj) if obj % 1 == 0 else float(obj)
        except Exception:
            return float(obj)
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
        # 受け取ったベット値は数値に正規化
        raw_bet_value = body.get('betValue', 1)
        try:
            bet_value = int(raw_bet_value)
        except Exception:
            try:
                bet_value = int(float(raw_bet_value))
            except Exception:
                bet_value = 1
        
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

        # 追加の整合チェック：更新後の状態で両者がアクション済みかつ金額が一致/整合していればrevealへ
        try:
            p1_action = updated_game_state.get('player1LastAction')
            p2_action = updated_game_state.get('player2LastAction')
            p1_bet = updated_game_state.get('player1BetAmount', 0)
            p2_bet = updated_game_state.get('player2BetAmount', 0)
            # Decimalを数値へ
            p1_bet_n = int(p1_bet) if isinstance(p1_bet, Decimal) or isinstance(p1_bet, (int, float)) else int(float(p1_bet))
            p2_bet_n = int(p2_bet) if isinstance(p2_bet, Decimal) or isinstance(p2_bet, (int, float)) else int(float(p2_bet))

            both_acted = (p1_action in ['call', 'raise']) and (p2_action in ['call', 'raise'])
            bets_matched = (p1_bet_n == p2_bet_n)

            # いずれかがdropならこの処理はスキップ（上で処理済み）
            if updated_game_state.get('gamePhase') == 'betting' and both_acted and bets_matched:
                table.update_item(
                    Key={'gameId': game_id},
                    UpdateExpression='SET #gp = :gp, #ua = :ua',
                    ExpressionAttributeNames={'#gp': 'gamePhase', '#ua': 'updatedAt'},
                    ExpressionAttributeValues={':gp': 'reveal', ':ua': int(time.time())}
                )
                # 再取得
                response = table.get_item(Key={'gameId': game_id})
                updated_game_state = response['Item']
                print('Post-check set gamePhase to reveal due to matched bets and both acted.')
        except Exception as e:
            print(f'Post-update reveal check skipped due to error: {e}')
        
        # レスポンスを作成
        response_data = {
            'gameId': game_id,
            'gamePhase': updated_game_state['gamePhase'],
            'isMyTurn': False,  # アクション実行後は相手のターン
            'currentBet': bet_value,
            'message': f'Successfully performed {action_type}'
        }
        
        # 双方の最新ベット額を明示的に返す（クライアントUI更新用）
        response_data['player1BetAmount'] = convert_decimals(updated_game_state.get('player1BetAmount', 0))
        response_data['player2BetAmount'] = convert_decimals(updated_game_state.get('player2BetAmount', 0))

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
        
        # デバッグ出力でもDecimalを変換してからJSON化する
        print(f"Bet action response: {json.dumps(convert_decimals(response_data), indent=2)}")
        
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