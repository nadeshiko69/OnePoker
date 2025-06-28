import json
import boto3
import random
import time
from datetime import datetime
from typing import Dict, List, Any

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('GameStates')

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    ゲーム開始処理のLambda関数
    デッキをシャッフルして各プレイヤーにカードを配布する
    """
    try:
        print('Event:', json.dumps(event, indent=2))
        
        # リクエストボディをパース
        body = json.loads(event['body'])
        room_code = body.get('roomCode')
        player1_id = body.get('player1Id')
        player2_id = body.get('player2Id')
        
        # 必須パラメータの検証
        if not all([room_code, player1_id, player2_id]):
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameters: roomCode, player1Id, player2Id'
                })
            }
        
        # ゲームIDを生成（タイムスタンプ + ランダム文字列）
        game_id = f"game_{int(time.time())}_{random.randint(100000, 999999)}"
        
        # デッキをシャッフル
        shuffled_deck = shuffle_deck()
        
        # 各プレイヤーに2枚ずつカードを配布
        player1_cards = shuffled_deck[:2]
        player2_cards = shuffled_deck[2:4]
        remaining_deck = shuffled_deck[4:]
        
        # ゲーム状態を作成
        game_state = {
            'gameId': game_id,
            'roomCode': room_code,
            'player1Id': player1_id,
            'player2Id': player2_id,
            'deck': remaining_deck,
            'player1Cards': player1_cards,
            'player2Cards': player2_cards,
            'player1Life': 10,
            'player2Life': 10,
            'currentBet': 0,
            'currentTurn': player1_id,  # 先手はplayer1
            'gamePhase': 'card_placement',  # card_placement, betting, reveal
            'player1CardPlaced': False,
            'player2CardPlaced': False,
            'player1BetAmount': 0,
            'player2BetAmount': 0,
            'player1PlacedCard': None,
            'player2PlacedCard': None,
            'createdAt': datetime.utcnow().isoformat(),
            'updatedAt': datetime.utcnow().isoformat()
        }
        
        # DynamoDBにゲーム状態を保存
        table.put_item(Item=game_state)
        
        # レスポンスを作成（各プレイヤーには自分のカードのみ返す）
        response = {
            'gameId': game_id,
            'roomCode': room_code,
            'player1Id': player1_id,
            'player2Id': player2_id,
            'currentTurn': player1_id,
            'gamePhase': 'card_placement',
            'player1Life': 10,
            'player2Life': 10
        }
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'POST, OPTIONS'
            },
            'body': json.dumps(response)
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

def shuffle_deck() -> List[int]:
    """
    デッキをシャッフルする関数
    52枚のカード（0-51のID）をランダムに並び替える
    """
    deck = list(range(52))  # 0-51のカードID
    random.shuffle(deck)    # シャッフル
    return deck 