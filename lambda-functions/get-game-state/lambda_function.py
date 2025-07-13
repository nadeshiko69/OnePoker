import json
import boto3
import time
from typing import Dict, Any

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    ゲーム状態を取得するLambda関数
    プレイヤーIDに応じて適切な情報のみを返す
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # クエリパラメータからゲームIDとプレイヤーIDを取得
        game_id = event['queryStringParameters'].get('gameId')
        player_id = event['queryStringParameters'].get('playerId')
        
        # 必須パラメータの検証
        if not game_id or not player_id:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'GET, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameters: gameId, playerId'
                })
            }
        
        # DynamoDBからゲーム状態を取得
        response = table.get_item(Key={'gameId': game_id})
        
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
                    'Access-Control-Allow-Methods': 'GET, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Player not authorized to access this game'
                })
            }
        
        # set_phaseから自動的にcard_placementに移行する処理
        if game_state['gamePhase'] == 'set_phase':
            current_time = int(time.time())
            phase_transition_time = game_state.get('phaseTransitionTime', 0)
            
            if current_time >= phase_transition_time:
                # 自動移行を実行
                update_expression = 'SET gamePhase = :gamePhase, phaseTransitionTime = :phaseTransitionTime, updatedAt = :updatedAt'
                expression_attribute_values = {
                    ':gamePhase': 'card_placement',
                    ':phaseTransitionTime': None,
                    ':updatedAt': current_time
                }
                
                table.update_item(
                    Key={'gameId': game_id},
                    UpdateExpression=update_expression,
                    ExpressionAttributeValues=expression_attribute_values
                )
                
                # ゲーム状態を更新
                game_state['gamePhase'] = 'card_placement'
                game_state['phaseTransitionTime'] = None
                game_state['updatedAt'] = current_time
                
                print(f"Auto-transitioning from set_phase to card_placement for game {game_id}")
        
        # プレイヤーIDに応じて適切な情報を返す
        is_player1 = player_id == game_state['player1Id']
        
        response_data = {
            'gameId': game_state['gameId'],
            'roomCode': game_state['roomCode'],
            'player1Id': game_state['player1Id'],
            'player2Id': game_state['player2Id'],
            'currentTurn': game_state['currentTurn'],
            'gamePhase': game_state['gamePhase'],
            'player1Life': game_state['player1Life'],
            'player2Life': game_state['player2Life'],
            'currentBet': game_state['currentBet'],
            'player1CardPlaced': game_state['player1CardPlaced'],
            'player2CardPlaced': game_state['player2CardPlaced'],
            'player1BetAmount': game_state['player1BetAmount'],
            'player2BetAmount': game_state['player2BetAmount'],
            'updatedAt': game_state['updatedAt']
        }
        
        # phaseTransitionTimeが存在する場合は追加
        if 'phaseTransitionTime' in game_state and game_state['phaseTransitionTime'] is not None:
            response_data['phaseTransitionTime'] = game_state['phaseTransitionTime']
        
        # 自分のカードのみを返す
        if is_player1:
            response_data['myCards'] = game_state['player1Cards']
            response_data['myLife'] = game_state['player1Life']
            response_data['myBetAmount'] = game_state['player1BetAmount']
            response_data['myCardPlaced'] = game_state['player1CardPlaced']
        else:
            response_data['myCards'] = game_state['player2Cards']
            response_data['myLife'] = game_state['player2Life']
            response_data['myBetAmount'] = game_state['player2BetAmount']
            response_data['myCardPlaced'] = game_state['player2CardPlaced']
        
        # 相手の配置済みカード情報（カードIDのみ）
        if is_player1:
            response_data['opponentCardPlaced'] = game_state['player2CardPlaced']
            if game_state['player2PlacedCard']:
                response_data['opponentPlacedCardId'] = game_state['player2PlacedCard']
        else:
            response_data['opponentCardPlaced'] = game_state['player1CardPlaced']
            if game_state['player1PlacedCard']:
                response_data['opponentPlacedCardId'] = game_state['player1PlacedCard']
        
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
                'message': str(error)
            })
        } 