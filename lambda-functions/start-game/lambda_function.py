import json
import boto3
import random
import time
from datetime import datetime
from typing import Dict, Any
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
room_table = dynamodb.Table('FriendMatchRoom')
game_table = dynamodb.Table('GameStates')

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
        
        # 必須パラメータの検証
        if not room_code:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Missing required parameter: roomCode'
                })
            }
        
        # ルーム情報を取得
        room_response = room_table.get_item(Key={'roomcode': room_code})
        
        if 'Item' not in room_response:
            return {
                'statusCode': 404,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Room not found'
                })
            }
        
        room_data = room_response['Item']
        
        # ルームがマッチング済みかチェック
        if room_data['status'] != 'matched':
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'POST, OPTIONS'
                },
                'body': json.dumps({
                    'error': 'Room is not matched yet'
                })
            }
        
        # プレイヤー情報を取得
        player1_id = room_data['hostPlayerId']
        player2_id = room_data['guestPlayerId']
        
        # 既存のゲーム状態をチェック（roomCodeで検索）
        existing_games = game_table.scan(
            FilterExpression='roomCode = :roomCode',
            ExpressionAttributeValues={':roomCode': room_code}
        )
        
        if existing_games['Items']:
            print(f"Found existing game for roomCode {room_code}: {existing_games['Items']}")
            # 既存のゲーム状態が存在する場合は、最初のものを返す
            existing_game = existing_games['Items'][0]
            
            # Decimal型を変換
            existing_game = convert_decimals(existing_game)
            
            response = {
                'gameId': existing_game['gameId'],
                'roomCode': existing_game['roomCode'],
                'player1Id': existing_game['player1Id'],
                'player2Id': existing_game['player2Id'],
                'player1Cards': existing_game['player1Cards'],
                'player2Cards': existing_game['player2Cards'],
                'currentTurn': existing_game['currentTurn'],
                'gamePhase': existing_game['gamePhase'],
                'player1Life': existing_game['player1Life'],
                'player2Life': existing_game['player2Life']
            }
            
            print(f"Returning existing game state: {response}")
            
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
        
        # 既存のゲーム状態が存在しない場合のみ新規作成
        print(f"No existing game found for roomCode {room_code}, creating new game")
        
        # ゲームIDを生成（タイムスタンプ + ランダム文字列）
        game_id = f"game_{int(time.time())}_{random.randint(100000, 999999)}"
        
        # デッキをシャッフル
        shuffled_deck = shuffle_deck()
        
        # 各プレイヤーに2枚ずつカードを配布
        player1_cards = shuffled_deck[:2]
        player2_cards = shuffled_deck[2:4]
        remaining_deck = shuffled_deck[4:]
        
        # 現在時刻を取得
        current_time = int(time.time())
        
        print(f"Game {game_id}: Starting with set_phase, current_time={current_time}")
        
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
            'gamePhase': 'set_phase',  # 最初はset_phaseから開始
            'phaseTransitionTime': None,  # Unity側の監視開始時に設定
            'player1Set': False,  # プレイヤー1のカードセット状態
            'player2Set': False,  # プレイヤー2のカードセット状態
            'player1CardValue': None,  # プレイヤー1のセットしたカード値
            'player2CardValue': None,  # プレイヤー2のセットしたカード値
            'player1CardPlaced': False,  # プレイヤー1のカード配置状態
            'player2CardPlaced': False,  # プレイヤー2のカード配置状態
            'player1BetAmount': 0,
            'player2BetAmount': 0,
            'player1PlacedCard': None,
            'player2PlacedCard': None,
            'awaitingPlayer': 'P1',  # 親から開始
            'currentRequiredBet': 1,  # 初期ベット額
            'player1UsedSkills': [],  # プレイヤー1の使用済スキル
            'player2UsedSkills': [],  # プレイヤー2の使用済スキル
            'createdAt': current_time,
            'updatedAt': current_time
        }
        
        # DynamoDBにゲーム状態を保存
        game_table.put_item(Item=game_state)
        print(f"New game state created and saved: {game_id}")
        
        # レスポンスを作成
        response = {
            'gameId': game_id,
            'roomCode': room_code,
            'player1Id': player1_id,
            'player2Id': player2_id,
            'player1Cards': player1_cards,
            'player2Cards': player2_cards,
            'currentTurn': player1_id,
            'gamePhase': 'set_phase',
            'player1Life': 10,
            'player2Life': 10
        }
        
        print(f"New game response: {json.dumps(response)}")
        
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

def convert_decimals(obj):
    """
    Decimal型をint/floatに変換する関数
    """
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

def shuffle_deck():
    """
    デッキをシャッフルする関数
    52枚のカード（0-51のID）をランダムに並び替える
    """
    deck = list(range(52))  # 0-51のカードID
    random.shuffle(deck)    # シャッフル
    return deck 