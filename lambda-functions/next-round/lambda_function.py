import json
import boto3
import random
import time
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
game_table = dynamodb.Table('GameStates')

def lambda_handler(event, context):
    """
    次のラウンドに進む処理
    - ラウンド番号を+1
    - 3ラウンドごとに親子を入れ替え
    - 使用済みカードをデッキに戻してシャッフル
    - 各プレイヤーに1枚ずつカードを配布
    - ゲーム状態をリセット（set_phaseに戻す）
    """
    try:
        print('Event:', json.dumps(event, indent=2, default=str))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        game_id = body.get('gameId')
        
        # 必須パラメータの検証
        if not game_id:
            return create_response(400, {'error': 'Missing required parameter: gameId'})
        
        # ゲーム状態を取得
        response = game_table.get_item(Key={'gameId': game_id})
        
        if 'Item' not in response:
            return create_response(404, {'error': 'Game not found'})
        
        game_state = response['Item']
        
        # 現在のラウンド番号を取得
        current_round = int(game_state.get('currentRound', 1))
        next_round = current_round + 1
        
        print(f"Game {game_id}: Advancing from round {current_round} to {next_round}")
        
        # 親を決定（3ラウンドごとに交代）
        # 1-3: P1, 4-6: P2, 7-9: P1, 10-12: P2, ...
        if ((next_round - 1) // 3) % 2 == 0:
            new_dealer = 'P1'
        else:
            new_dealer = 'P2'
        
        print(f"Round {next_round}: Dealer is {new_dealer}")
        
        # デッキを取得
        deck = game_state.get('deck', [])
        player1_cards = game_state.get('player1Cards', [])
        player2_cards = game_state.get('player2Cards', [])
        
        # 使用済みカード（セットしたカード）を取得
        used_cards = []
        if game_state.get('player1CardValue') is not None:
            used_cards.append(int(game_state['player1CardValue']))
        if game_state.get('player2CardValue') is not None:
            used_cards.append(int(game_state['player2CardValue']))
        
        print(f"Used cards this round: {used_cards}")
        
        # デッキに使用済みカードを戻す
        deck.extend(used_cards)
        
        # デッキをシャッフル
        deck = [int(card) for card in deck]
        random.shuffle(deck)
        
        print(f"Deck shuffled. Deck size: {len(deck)}")
        
        # 各プレイヤーに1枚ずつ配布（現在1枚持っているので、2枚になる）
        if len(deck) < 2:
            return create_response(400, {'error': 'Not enough cards in deck'})
        
        player1_cards.append(deck.pop(0))
        player2_cards.append(deck.pop(0))
        
        print(f"Cards dealt. Player1: {player1_cards}, Player2: {player2_cards}")
        
        # ゲーム状態を更新
        current_time = int(time.time())
        
        update_expression = """
            SET currentRound = :round,
                currentDealer = :dealer,
                awaitingPlayer = :dealer,
                deck = :deck,
                player1Cards = :p1cards,
                player2Cards = :p2cards,
                gamePhase = :phase,
                phaseTransitionTime = :null_val,
                player1Set = :false_val,
                player2Set = :false_val,
                player1CardValue = :null_val,
                player2CardValue = :null_val,
                player1CardPlaced = :false_val,
                player2CardPlaced = :false_val,
                player1BetAmount = :zero,
                player2BetAmount = :zero,
                currentRequiredBet = :one,
                player1UsedSkills = :empty_list,
                player2UsedSkills = :empty_list,
                updatedAt = :time
        """
        
        game_table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=update_expression,
            ExpressionAttributeValues={
                ':round': next_round,
                ':dealer': new_dealer,
                ':deck': deck,
                ':p1cards': player1_cards,
                ':p2cards': player2_cards,
                ':phase': 'set_phase',
                ':null_val': None,
                ':false_val': False,
                ':zero': 0,
                ':one': 1,
                ':empty_list': [],
                ':time': current_time
            }
        )
        
        print(f"Game state updated for round {next_round}")
        
        # レスポンスを作成
        response_data = {
            'success': True,
            'gameId': game_id,
            'currentRound': next_round,
            'currentDealer': new_dealer,
            'gamePhase': 'set_phase',
            'player1Cards': player1_cards,
            'player2Cards': player2_cards
        }
        
        return create_response(200, response_data)
        
    except Exception as error:
        print('Error:', str(error))
        import traceback
        traceback.print_exc()
        return create_response(500, {
            'error': 'Internal server error',
            'message': str(error)
        })

def create_response(status_code, body):
    """
    API Gateway用のレスポンスを作成
    """
    return {
        'statusCode': status_code,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Headers': 'Content-Type',
            'Access-Control-Allow-Methods': 'POST, OPTIONS'
        },
        'body': json.dumps(body, default=str)
    }


