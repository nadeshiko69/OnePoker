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
        
        # プレイヤー別の重複実行チェック
        if is_player1:
            player_cards_key = 'player1Cards'
            player_set_key = 'player1CardValue'
        else:
            player_cards_key = 'player2Cards'
            player_set_key = 'player2CardValue'
        
        # 既に手札が2枚で、セットしたカードがnullの場合は既に処理済み
        current_player_cards = game_state.get(player_cards_key, [])
        current_player_set = game_state.get(player_set_key)
        
        if len(current_player_cards) == 2 and current_player_set is None:
            print(f"{player_name} already processed for next round")
            return create_response(200, {
                'success': True,
                'gameId': game_id,
                'playerId': player_id,
                'processedPlayer': player_name,
                'message': 'Already processed'
            })
        
        print(f"Game {game_id}: Advancing from round {current_round} to {next_round}")
        
        # 現在の親を取得
        current_dealer = game_state.get('currentDealer', 'P1')
        
        # 親を決定（3ラウンドごとに交代）
        # 1-3: P1, 4-6: P2, 7-9: P1, 10-12: P2, ...
        if ((next_round - 1) // 3) % 2 == 0:
            new_dealer = 'P1'
        else:
            new_dealer = 'P2'
        
        print(f"Round {next_round}: Current dealer is {current_dealer}, New dealer is {new_dealer}")
        
        # デッキを取得
        deck = game_state.get('deck', [])
        
        # 使用済みカード（セットしたカード）を取得
        used_cards = []
        if game_state.get('player1CardValue') is not None:
            used_cards.append(int(game_state['player1CardValue']))
        if game_state.get('player2CardValue') is not None:
            used_cards.append(int(game_state['player2CardValue']))
        
        print(f"Used cards this round: {used_cards}")
        
        # 勝敗判定とライフ更新
        player1_life = int(game_state.get('player1Life', 20))
        player2_life = int(game_state.get('player2Life', 20))
        
        # 各プレイヤーのベット額を取得
        player1_bet = int(game_state.get('player1BetAmount', 0))
        player2_bet = int(game_state.get('player2BetAmount', 0))
        
        # 勝敗判定（カード値の比較）
        player1_card_value = game_state.get('player1CardValue')
        player2_card_value = game_state.get('player2CardValue')
        
        if player1_card_value is not None and player2_card_value is not None:
            p1_value = int(player1_card_value)
            p2_value = int(player2_card_value)
            
            print(f"Round {current_round} result: Player1={p1_value}, Player2={p2_value}")
            print(f"Bet amounts: Player1={player1_bet}, Player2={player2_bet}")
            
            # 勝敗判定
            p1_wins = determine_winner(p1_value, p2_value)
            
            if p1_wins == 1:
                # Player1の勝利 - Player1がPlayer2の掛け金を獲得、Player2が自分の掛け金を失う
                player1_life += player2_bet
                player2_life -= player2_bet
                print(f"Player1 wins! Life updated: P1={player1_life} (+{player2_bet}), P2={player2_life} (-{player2_bet})")
            elif p1_wins == -1:
                # Player2の勝利 - Player2がPlayer1の掛け金を獲得、Player1が自分の掛け金を失う
                player1_life -= player1_bet
                player2_life += player1_bet
                print(f"Player2 wins! Life updated: P1={player1_life} (-{player1_bet}), P2={player2_life} (+{player1_bet})")
            else:
                # 引き分け（ライフ変更なし）
                print(f"Draw! No life changes: P1={player1_life}, P2={player2_life}")
        else:
            print(f"No card values available for life calculation: P1={player1_card_value}, P2={player2_card_value}")
        
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
        
        # 親のプレイヤーかどうかを判定
        is_dealer = (player_id == current_dealer)
        print(f"Player {player_id} is {'dealer' if is_dealer else 'non-dealer'}")
        
        # プレイヤー別の更新式を構築
        if is_player1:
            if is_dealer:
                # 親（Player1）: 共通フィールド + Player1の手札を更新
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
                        player1Life = :p1life,
                        player2Life = :p2life,
                        gamePhase = :phase,
                        phaseTransitionTime = :null_val,
                        currentRequiredBet = :one,
                        updatedAt = :time
                """
            else:
                # 子（Player1）: Player1の手札のみを更新＋盤面をSetPhaseへ
                update_expression = f"""
                    SET player1Cards = :p1cards,
                        player1CardValue = :null_val,
                        player1CardPlaced = :false_val,
                        player1Set = :false_val,
                        player1BetAmount = :zero,
                        player1Life = :p1life,
                        player2Life = :p2life,
                        gamePhase = :phase,
                        awaitingPlayer = :dealer,
                        phaseTransitionTime = :null_val,
                        currentRequiredBet = :one,
                        updatedAt = :time
                """
        else:
            if is_dealer:
                # 親（Player2）: 共通フィールド + Player2の手札を更新
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
                        player1Life = :p1life,
                        player2Life = :p2life,
                        gamePhase = :phase,
                        phaseTransitionTime = :null_val,
                        currentRequiredBet = :one,
                        updatedAt = :time
                """
            else:
                # 子（Player2）: Player2の手札のみを更新＋盤面をSetPhaseへ
                update_expression = f"""
                    SET player2Cards = :p2cards,
                        player2CardValue = :null_val,
                        player2CardPlaced = :false_val,
                        player2Set = :false_val,
                        player2BetAmount = :zero,
                        player1Life = :p1life,
                        player2Life = :p2life,
                        gamePhase = :phase,
                        awaitingPlayer = :dealer,
                        phaseTransitionTime = :null_val,
                        currentRequiredBet = :one,
                        updatedAt = :time
                """
        
        # プレイヤー別のExpressionAttributeValuesを構築
        if is_player1:
            if is_dealer:
                # 親（Player1）: 共通フィールド + Player1の手札
                expression_values = {
                    ':round': next_round,
                    ':dealer': new_dealer,
                    ':deck': deck,
                    ':p1cards': player1_cards,
                    ':p1life': player1_life,
                    ':p2life': player2_life,
                    ':phase': 'set_phase',
                    ':null_val': None,
                    ':false_val': False,
                    ':zero': 0,
                    ':one': 1,
                    ':time': current_time
                }
            else:
                # 子（Player1）: Player1の手札のみ＋盤面遷移用の値
                expression_values = {
                    ':p1cards': player1_cards,
                    ':p1life': player1_life,
                    ':p2life': player2_life,
                    ':null_val': None,
                    ':false_val': False,
                    ':zero': 0,
                    ':one': 1,
                    ':phase': 'set_phase',
                    ':dealer': new_dealer,
                    ':time': current_time
                }
        else:
            if is_dealer:
                # 親（Player2）: 共通フィールド + Player2の手札
                expression_values = {
                    ':round': next_round,
                    ':dealer': new_dealer,
                    ':deck': deck,
                    ':p2cards': player2_cards,
                    ':p1life': player1_life,
                    ':p2life': player2_life,
                    ':phase': 'set_phase',
                    ':null_val': None,
                    ':false_val': False,
                    ':zero': 0,
                    ':one': 1,
                    ':time': current_time
                }
            else:
                # 子（Player2）: Player2の手札のみ＋盤面遷移用の値
                expression_values = {
                    ':p2cards': player2_cards,
                    ':p1life': player1_life,
                    ':p2life': player2_life,
                    ':null_val': None,
                    ':false_val': False,
                    ':zero': 0,
                    ':one': 1,
                    ':phase': 'set_phase',
                    ':dealer': new_dealer,
                    ':time': current_time
                }
        
        try:
            game_table.update_item(
                Key={'gameId': game_id},
                UpdateExpression=update_expression,
                ExpressionAttributeValues=expression_values
            )
            role = "dealer" if is_dealer else "non-dealer"
            print(f"Game state updated successfully for {player_name} ({role}) in round {next_round}")
        except Exception as e:
            print(f"Error updating game state: {str(e)}")
            return create_response(500, {'error': f'Failed to update game state: {str(e)}'})
        
        # DynamoDBから最新のゲーム状態を取得して、両プレイヤーの手札を含める
        try:
            updated_response = game_table.get_item(Key={'gameId': game_id})
            if 'Item' in updated_response:
                updated_game_state = updated_response['Item']
                # 最新の手札を取得
                player1_cards = updated_game_state.get('player1Cards', [])
                player2_cards = updated_game_state.get('player2Cards', [])
                print(f"Retrieved latest cards - Player1: {player1_cards}, Player2: {player2_cards}")
            else:
                print(f"Warning: Could not retrieve updated game state for game {game_id}")
        except Exception as e:
            print(f"Warning: Could not retrieve updated game state: {str(e)}")
            # エラーが発生しても、更新した手札を使用して続行
        
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
            'player2Cards': player2_cards,
            'player1Life': player1_life,
            'player2Life': player2_life
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

def determine_winner(card1_value, card2_value):
    """
    カード値の勝敗を判定する関数
    Aが2に負ける特殊ルールを含む
        1: Player1の勝利
        -1: Player2の勝利
        0: 引き分け
    """
    # カード値を数値に変換（0-51のIDから実際のカード値を取得）
    def get_card_number(card_id):
        # カードID 0-51を数値1-13に変換（A=1, 2=2, ..., K=13）
        return (card_id % 13) + 1
    
    p1_number = get_card_number(card1_value)
    p2_number = get_card_number(card2_value)
    
    print(f"Card numbers: Player1={p1_number}, Player2={p2_number}")
    
    # Aが2に負ける特殊ルール
    if p1_number == 1 and p2_number == 2:  # Player1がA、Player2が2
        print("Special rule: A loses to 2 - Player2 wins")
        return -1
    elif p1_number == 2 and p2_number == 1:  # Player1が2、Player2がA
        print("Special rule: A loses to 2 - Player1 wins")
        return 1
    
    # 通常の数値比較
    if p1_number > p2_number:
        return 1
    elif p2_number > p1_number:
        return -1
    else:
        return 0

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


