import json
import boto3
import time
from datetime import datetime
from typing import Dict, Any
import decimal

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def convert_decimal(obj):
    if isinstance(obj, list):
        return [convert_decimal(i) for i in obj]
    elif isinstance(obj, dict):
        return {k: convert_decimal(v) for k, v in obj.items()}
    elif isinstance(obj, decimal.Decimal):
        if obj % 1 == 0:
            return int(obj)
        else:
            return float(obj)
    else:
        return obj

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    ゲーム状態を更新するLambda関数
    プレイヤーのアクション（カード配置、ベット、スキル使用）を処理する
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        game_id = body.get('gameId')
        player_id = body.get('playerId')
        action_type = body.get('actionType')  # 'place_card', 'bet', 'use_skill', 'call'
        action_data = body.get('actionData', {})
        
        # 必須パラメータの検証
        if not all([game_id, player_id, action_type]):
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
        
        # プレイヤーが自分のターンかチェック
        if game_state['currentTurn'] != player_id:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Not your turn'
                })
            }
        
        # アクションタイプに応じて処理
        is_player1 = player_id == game_state['player1Id']
        update_data = {}
        
        # set_phaseから自動的にcard_placementに移行する処理
        if game_state['gamePhase'] == 'set_phase':
            current_time = int(time.time())
            phase_transition_time = game_state.get('phaseTransitionTime', 0)
            
            if current_time >= phase_transition_time:
                update_data['gamePhase'] = 'card_placement'
                update_data['phaseTransitionTime'] = None  # 移行完了後はクリア
                print(f"Auto-transitioning from set_phase to card_placement for game {game_id}")
        
        if action_type == 'place_card':
            # カード配置処理
            card_id = action_data.get('cardId')
            if card_id is None:
                return create_error_response('Missing cardId in actionData')
            
            # カードが自分の手札にあるかチェック
            player_cards = game_state['player1Cards'] if is_player1 else game_state['player2Cards']
            if card_id not in player_cards:
                return create_error_response('Card not in hand')
            
            # 既にカードを配置済みかチェック
            if (is_player1 and game_state['player1CardPlaced']) or (not is_player1 and game_state['player2CardPlaced']):
                return create_error_response('Card already placed')
            
            # カード配置を記録
            if is_player1:
                update_data.update({
                    'player1CardPlaced': True,
                    'player1PlacedCard': card_id
                })
            else:
                update_data.update({
                    'player2CardPlaced': True,
                    'player2PlacedCard': card_id
                })
            
            # 両者カード配置済みの場合、ベットフェーズに移行
            if (is_player1 and game_state['player2CardPlaced']) or (not is_player1 and game_state['player1CardPlaced']):
                update_data['gamePhase'] = 'betting'
                # ターンを相手に移す
                update_data['currentTurn'] = game_state['player2Id'] if is_player1 else game_state['player1Id']
        
        elif action_type == 'bet':
            # ベット処理
            bet_amount = action_data.get('amount', 0)
            if bet_amount <= 0:
                return create_error_response('Invalid bet amount')
            
            # ライフが足りるかチェック
            player_life = game_state['player1Life'] if is_player1 else game_state['player2Life']
            if player_life < bet_amount:
                return create_error_response('Insufficient life')
            
            # ベットを記録
            if is_player1:
                update_data.update({
                    'player1BetAmount': game_state['player1BetAmount'] + bet_amount,
                    'player1Life': player_life - bet_amount,
                    'currentBet': game_state['currentBet'] + bet_amount
                })
            else:
                update_data.update({
                    'player2BetAmount': game_state['player2BetAmount'] + bet_amount,
                    'player2Life': player_life - bet_amount,
                    'currentBet': game_state['currentBet'] + bet_amount
                })
            
            # ターンを相手に移す
            update_data['currentTurn'] = game_state['player2Id'] if is_player1 else game_state['player1Id']
        
        elif action_type == 'call':
            # コール処理（カード公開フェーズに移行）
            update_data['gamePhase'] = 'reveal'
            # ターンを相手に移す
            update_data['currentTurn'] = game_state['player2Id'] if is_player1 else game_state['player1Id']
        
        elif action_type == 'use_skill':
            # スキル使用処理
            skill_type = action_data.get('skillType')
            if not skill_type:
                return create_error_response('Missing skillType in actionData')
            
            # スキル使用の記録（実装は後で詳細化）
            update_data['lastSkillUsed'] = {
                'playerId': player_id,
                'skillType': skill_type,
                'timestamp': datetime.utcnow().isoformat()
            }
            
            # ターンを相手に移す
            update_data['currentTurn'] = game_state['player2Id'] if is_player1 else game_state['player1Id']
        
        elif action_type == 'check_phase_transition':
            # フェーズ移行確認処理（set_phaseからcard_placementへの自動移行）
            current_time = int(time.time())
            phase_transition_time = game_state.get('phaseTransitionTime', 0)
            
            if game_state['gamePhase'] == 'set_phase' and current_time >= phase_transition_time:
                update_data['gamePhase'] = 'card_placement'
                update_data['phaseTransitionTime'] = None
                print(f"Phase transition confirmed: set_phase -> card_placement for game {game_id}")
            else:
                # 移行条件を満たしていない場合は何もしない
                print(f"Phase transition not ready yet for game {game_id}")
        
        else:
            return create_error_response(f'Unknown action type: {action_type}')
        
        # 更新時刻を設定
        update_data['updatedAt'] = datetime.utcnow().isoformat()
        
        # DynamoDBを更新
        update_expression = 'SET ' + ', '.join([f'#{k} = :{k}' for k in update_data.keys()])
        expression_attribute_names = {f'#{k}': k for k in update_data.keys()}
        expression_attribute_values = {f':{k}': v for k, v in update_data.items()}
        
        table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=update_expression,
            ExpressionAttributeNames=expression_attribute_names,
            ExpressionAttributeValues=expression_attribute_values
        )
        
        # 成功レスポンス
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps(convert_decimal(update_data))
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

def create_error_response(message: str) -> Dict[str, Any]:
    """エラーレスポンスを作成するヘルパー関数"""
    return {
        'statusCode': 400,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Headers': 'Content-Type',
            'Access-Control-Allow-Methods': 'POST, OPTIONS'
        },
        'body': json.dumps({
            'error': message
        })
    } 