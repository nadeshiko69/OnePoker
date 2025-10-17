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
        player_id = body.get('playerId')  # プレイヤーIDを取得
        
        # 必須パラメータの検証
        if not game_id:
            return create_response(400, {'error': 'Missing required parameter: gameId'})
        
        if not player_id:
            return create_response(400, {'error': 'Missing required parameter: playerId'})
        
        print(f"Next round request from player: {player_id}")
        
        # ゲーム状態を取得
        response = game_table.get_item(Key={'gameId': game_id})
        
        if 'Item' not in response:
            return create_response(404, {'error': 'Game not found'})
        
        game_state = response['Item']
        
        # 現在のラウンド番号を取得
        current_round = int(game_state.get('currentRound', 1))
        next_round = current_round + 1
        
        # プレイヤーIDの検証
        player1_id = game_state.get('player1Id')
        player2_id = game_state.get('player2Id')
        
        if player_id not in [player1_id, player2_id]:
            return create_response(403, {'error': 'Player not in this game'})
        
        # プレイヤーがPlayer1かPlayer2かを判定
        is_player1 = (player_id == player1_id)
        print(f"Player {player_id} is {'Player1' if is_player1 else 'Player2'}")
        
        # 現在の手札枚数を取得
        player1_cards = game_state.get('player1Cards', [])
        player2_cards = game_state.get('player2Cards', [])
        
        # 重複実行チェック: 既に次のラウンドに進んでいる場合はエラー
        if game_state.get('gamePhase') == 'set_phase' and game_state.get('currentRound', 1) >= next_round:
            return create_response(400, {'error': 'Next round already started'})
        
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
        print(f"Initial hands - Player1: {player1_cards}, Player2: {player2_cards}")
    
        # 呼び出し元プレイヤーのデータのみを処理
        if is_player1:
            # Player1の処理
            player_set_card = game_state.get('player1CardValue')
            player_cards = player1_cards
            player_name = "Player1"
            card_value_key = 'player1CardValue'
            cards_key = 'player1Cards'
        else:
            # Player2の処理
            player_set_card = game_state.get('player2CardValue')
            player_cards = player2_cards
            player_name = "Player2"
            card_value_key = 'player2CardValue'
            cards_key = 'player2Cards'
        
        print(f"Processing {player_name} - Set card: {player_set_card}, Current hand: {player_cards}")
        
        # ステップ1: セットしたカードを手札から削除（1枚にする）
        if player_set_card is not None:
            set_card_value = int(player_set_card)
            if set_card_value in player_cards:
                player_cards.remove(set_card_value)
                print(f"Step 1: Removed set card {set_card_value} from {player_name}'s hand")
            else:
                print(f"Step 1: Warning - Set card {set_card_value} not found in {player_name}'s hand")
        
        print(f"Step 1 Complete - {player_name} hand after removing set card: {player_cards}")
        
        # ステップ2: デッキから1枚補充（2枚にする）
        if len(deck) < 1:
            return create_response(400, {'error': 'Not enough cards in deck'})
        
        # 手札枚数チェック（3枚以上になることを防ぐ）
        if len(player_cards) >= 2:
            print(f"Step 2: Warning - {player_name} already has 2+ cards: {len(player_cards)}")
            return create_response(400, {'error': f'{player_name} already has enough cards'})
        
        # 1枚配布
        player_cards.append(deck.pop(0))
        print(f"Step 2 Complete - {player_name} final hand: {player_cards}")
        
        # 更新された手札を適切な変数に戻す
        if is_player1:
            player1_cards = player_cards
        else:
            player2_cards = player_cards
        
        # ゲーム状態を更新
        current_time = int(time.time())
        
        # プレイヤー別の更新式を構築
        if is_player1:
            update_expression = f"""
                SET currentRound = :round,
                    currentDealer = :dealer,
                    awaitingPlayer = :dealer,
                    deck = :deck,
                    player1Cards = :p1cards,
                    player1CardValue = :null_val,
                    player1CardPlaced = :false_val,
                    player1Set = :false_val,
                    player1BetAmount = :zero,
                    player1UsedSkills = :empty_list,
                    gamePhase = :phase,
                    phaseTransitionTime = :null_val,
                    currentRequiredBet = :one,
                    updatedAt = :time
            """
        else:
            update_expression = f"""
                SET currentRound = :round,
                    currentDealer = :dealer,
                    awaitingPlayer = :dealer,
                    deck = :deck,
                    player2Cards = :p2cards,
                    player2CardValue = :null_val,
                    player2CardPlaced = :false_val,
                    player2Set = :false_val,
                    player2BetAmount = :zero,
                    player2UsedSkills = :empty_list,
                    gamePhase = :phase,
                    phaseTransitionTime = :null_val,
                    currentRequiredBet = :one,
                    updatedAt = :time
            """
        
        # 条件: 現在のラウンドが期待値と一致する場合のみ更新
        condition_expression = "currentRound = :current_round"
        
        try:
            game_table.update_item(
                Key={'gameId': game_id},
                UpdateExpression=update_expression,
                ConditionExpression=condition_expression,
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
                    ':time': current_time,
                    ':current_round': current_round
                }
            )
            print(f"Game state updated successfully for round {next_round}")
        except game_table.meta.client.exceptions.ConditionalCheckFailedException:
            print(f"Conditional check failed - round may have already been advanced")
            return create_response(400, {'error': 'Round already advanced by another request'})
        
        # レスポンスを作成
        response_data = {
            'success': True,
            'gameId': game_id,
            'playerId': player_id,
            'processedPlayer': 'Player1' if is_player1 else 'Player2',
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


