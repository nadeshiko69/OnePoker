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
        
        selected_card_index = body.get('selectedCardIndex', -1)
        try:
            selected_card_index = int(selected_card_index)
        except (TypeError, ValueError):
            selected_card_index = -1
        
        # 使用済スキルリストを取得
        skill_list_key = 'player1UsedSkills' if is_player1 else 'player2UsedSkills'
        used_skills = list(game_state.get(skill_list_key, []))
        
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
        
        player_cards_key = 'player1Cards' if is_player1 else 'player2Cards'
        player1_cards = [int(card) for card in game_state.get('player1Cards', [])]
        player2_cards = [int(card) for card in game_state.get('player2Cards', [])]
        deck = [int(card) for card in game_state.get('deck', [])]
        
        updated_used_skills = used_skills + [skill_type]
        current_time = int(time.time())
        drawn_card = -1
        discarded_card = -1
        
        if skill_type == 'Change':
            target_cards = player1_cards if is_player1 else player2_cards
            
            if selected_card_index < 0 or selected_card_index >= len(target_cards):
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*',
                        'Access-Control-Allow-Headers': 'Content-Type',
                        'Access-Control-Allow-Methods': 'POST, OPTIONS'
                    },
                    'body': json.dumps({
                        'error': 'Invalid card index for Change skill'
                    })
                }
            
            if len(deck) == 0:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*',
                        'Access-Control-Allow-Headers': 'Content-Type',
                        'Access-Control-Allow-Methods': 'POST, OPTIONS'
                    },
                    'body': json.dumps({
                        'error': 'Deck is empty. Cannot draw a new card.'
                    })
                }
            
            discarded_card = target_cards.pop(selected_card_index)
            drawn_card = deck.pop(0)
            target_cards.append(drawn_card)
            
            print(f"Change skill executed: discarded {discarded_card}, drew {drawn_card}")
            
            update_expression = f'SET {skill_list_key} = :skills, {player_cards_key} = :player_cards, deck = :deck, updatedAt = :time'
            expression_values = {
                ':skills': updated_used_skills,
                ':player_cards': target_cards,
                ':deck': deck,
                ':time': current_time
            }
        else:
            update_expression = f'SET {skill_list_key} = :skills, updatedAt = :time'
            expression_values = {
                ':skills': updated_used_skills,
                ':time': current_time
            }
        
        # DynamoDBを更新
        game_table.update_item(
            Key={'gameId': game_id},
            UpdateExpression=update_expression,
            ExpressionAttributeValues=expression_values
        )
        
        print(f"Skill {skill_type} used by player {player_id} in game {game_id}")
        
        # レスポンスを作成
        response_data = {
            'success': True,
            'gameId': game_id,
            'playerId': player_id,
            'skillType': skill_type,
            'usedSkills': updated_used_skills,
            'player1Cards': player1_cards,
            'player2Cards': player2_cards,
            'deck': deck,
            'drawnCard': drawn_card,
            'discardedCard': discarded_card
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

