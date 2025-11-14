import json
import boto3
import time
from typing import Dict, Any
import decimal

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def convert_decimal(obj):
    if isinstance(obj, list):
        return [convert_decimal(i) for i in obj]
    if isinstance(obj, dict):
        return {k: convert_decimal(v) for k, v in obj.items()}
    if isinstance(obj, decimal.Decimal):
        try:
            return int(obj) if obj % 1 == 0 else float(obj)
        except Exception:
            return float(obj)
    return obj

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
        
        # デバッグログを追加
        print(f"Game {game_id}: Retrieved game state - gamePhase: {game_state.get('gamePhase')}, phaseTransitionTime: {game_state.get('phaseTransitionTime')}")
        # set_phaseからcard_placementへの自動遷移処理を削除
        # 現在はset_phaseとcard_placementが統合されているため、自動遷移は不要
        
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
            'player1CardValue': game_state['player1CardValue'],
            'player2CardValue': game_state['player2CardValue'],
            'player1BetAmount': convert_decimal(game_state.get('player1BetAmount', 0)),
            'player2BetAmount': convert_decimal(game_state.get('player2BetAmount', 0)),
            'player1Set': game_state.get('player1Set', False),
            'player2Set': game_state.get('player2Set', False),
            'awaitingPlayer': game_state.get('awaitingPlayer', 'P1'),
            'currentRequiredBet': convert_decimal(game_state.get('currentRequiredBet', 1)),
            'player1UsedSkills': game_state.get('player1UsedSkills', []),
            'player2UsedSkills': game_state.get('player2UsedSkills', []),
            'player1Cards': game_state.get('player1Cards', []),
            'player2Cards': game_state.get('player2Cards', []),
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
            'body': json.dumps(convert_decimal(response_data))
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