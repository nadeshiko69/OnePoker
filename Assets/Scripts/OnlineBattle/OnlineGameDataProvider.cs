using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// オンライン対戦のゲームデータを管理・提供するクラス
/// ゲームID、プレイヤーID、カード情報などを一元管理
/// </summary>
public class OnlineGameDataProvider : MonoBehaviour
{
    private OnlineGameDataWithCards gameData;
    
    // プロパティ
    public string GameId => gameData?.gameId ?? "";
    public string PlayerId => gameData?.playerId ?? "";
    public string OpponentId => gameData?.opponentId ?? "";
    public string RoomCode => gameData?.roomCode ?? "";
    public bool IsPlayer1 => gameData?.isPlayer1 ?? false;
    public int[] Player1Cards => gameData?.player1Cards;
    public int[] Player2Cards => gameData?.player2Cards;
    public int[] MyCards => IsPlayer1 ? Player1Cards : Player2Cards;
    public int[] OpponentCards => IsPlayer1 ? Player2Cards : Player1Cards;
    
    // ライフ値のプロパティ
    public int Player1Life => gameData?.player1Life ?? 20;
    public int Player2Life => gameData?.player2Life ?? 20;
    public int MyLife => IsPlayer1 ? Player1Life : Player2Life;
    public int OpponentLife => IsPlayer1 ? Player2Life : Player1Life;
    
    // セットしたカード値のプロパティ
    public int Player1CardValue => gameData?.player1CardValue ?? -1;
    public int Player2CardValue => gameData?.player2CardValue ?? -1;
    public int MySetCardValue => IsPlayer1 ? Player1CardValue : Player2CardValue;
    public int OpponentSetCardValue => IsPlayer1 ? Player2CardValue : Player1CardValue;
    
    // ゲームデータとプレイヤーIDのプロパティ
    public OnlineGameDataWithCards GameData => gameData;
    public string MyPlayerId => gameData?.playerId ?? "";
    
    void Start()
    {
        Debug.Log("[GameDataProvider] Start called");
        
        // テスト用: PlayerPrefsにデータがない場合のダミーデータ
        if (!LoadGameData())
        {
            Debug.LogWarning("[GameDataProvider] Failed to load game data, creating test data");
            CreateTestGameData();
        }
    }
    
    /// <summary>
    /// テスト用のゲームデータを作成
    /// </summary>
    private void CreateTestGameData()
    {
        gameData = new OnlineGameDataWithCards
        {
            gameId = "test_game_123",
            playerId = "test_player_456",
            opponentId = "test_opponent_789",
            isPlayer1 = true,
            roomCode = "TEST001",
            player1Cards = new int[] { 1, 2 },
            player2Cards = new int[] { 3, 4 },
            player1CardValue = -1,
            player2CardValue = -1,
            player1Life = 20,
            player2Life = 20,
            player1UsedSkills = new List<string>(),
            player2UsedSkills = new List<string>()
        };
        
        Debug.Log("[GameDataProvider] Test game data created:");
        Debug.Log($"[GameDataProvider] - GameId: '{gameData.gameId}'");
        Debug.Log($"[GameDataProvider] - PlayerId: '{gameData.playerId}'");
        Debug.Log($"[GameDataProvider] - OpponentId: '{gameData.opponentId}'");
    }
    
    /// <summary>
    /// PlayerPrefsからゲームデータを読み込み
    /// </summary>
    public bool LoadGameData()
    {
        Debug.Log("[GameDataProvider] Loading game data from PlayerPrefs");
        
        string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        Debug.Log($"[GameDataProvider] Raw JSON from PlayerPrefs: '{gameDataJson}'");
        Debug.Log($"[GameDataProvider] JSON length: {gameDataJson.Length}");
        
        if (string.IsNullOrEmpty(gameDataJson))
        {
            Debug.LogError("[GameDataProvider] OnlineGameData is empty in PlayerPrefs");
            return false;
        }
        
        try
        {
            gameData = JsonUtility.FromJson<OnlineGameDataWithCards>(gameDataJson);
            
            if (gameData != null)
            {
                Debug.Log($"[GameDataProvider] GameData parsed successfully:");
                Debug.Log($"[GameDataProvider] - GameId: '{gameData.gameId}'");
                Debug.Log($"[GameDataProvider] - PlayerId: '{gameData.playerId}'");
                Debug.Log($"[GameDataProvider] - OpponentId: '{gameData.opponentId}'");
                Debug.Log($"[GameDataProvider] - IsPlayer1: {gameData.isPlayer1}");
                LogGameDataDetails();
                return true;
            }
            else
            {
                Debug.LogError("[GameDataProvider] Failed to parse game data");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataProvider] Exception while loading game data: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// ゲームデータを更新
    /// </summary>
    public void UpdateGameData(OnlineGameDataWithCards newData)
    {
        gameData = newData;
        Debug.Log("[GameDataProvider] Game data updated");
    }
    
    /// <summary>
    /// セットしたカード値を更新
    /// </summary>
    public void UpdateSetCardValues(int player1CardValue, int player2CardValue)
    {
        if (gameData == null)
        {
            Debug.LogError("[GameDataProvider] Game data is null, cannot update set card values");
            return;
        }
        
        gameData.player1CardValue = player1CardValue;
        gameData.player2CardValue = player2CardValue;
        
        Debug.Log($"[GameDataProvider] Set card values updated - Player1: {player1CardValue}, Player2: {player2CardValue}");
    }
    
    /// <summary>
    /// 使用済スキルを更新
    /// </summary>
    public void UpdateUsedSkills(List<string> player1UsedSkills, List<string> player2UsedSkills)
    {
        if (gameData == null)
        {
            Debug.LogError("[GameDataProvider] Game data is null, cannot update used skills");
            return;
        }
        
        gameData.player1UsedSkills = player1UsedSkills ?? new List<string>();
        gameData.player2UsedSkills = player2UsedSkills ?? new List<string>();
        
        Debug.Log($"[GameDataProvider] Used skills updated - Player1: {string.Join(", ", gameData.player1UsedSkills)}, Player2: {string.Join(", ", gameData.player2UsedSkills)}");
    }
    
    /// <summary>
    /// 特定プレイヤーのセットしたカード値を更新
    /// </summary>
    public void UpdatePlayerSetCardValue(string playerId, int cardValue)
    {
        if (gameData == null)
        {
            Debug.LogError("[GameDataProvider] Game data is null, cannot update player set card value");
            return;
        }
        
        if (playerId == gameData.playerId)
        {
            // 自分のカード値を更新
            if (gameData.isPlayer1)
            {
                gameData.player1CardValue = cardValue;
                Debug.Log($"[GameDataProvider] Player1 set card value updated: {cardValue}");
            }
            else
            {
                gameData.player2CardValue = cardValue;
                Debug.Log($"[GameDataProvider] Player2 set card value updated: {cardValue}");
            }
        }
        else
        {
            // 相手のカード値を更新
            if (gameData.isPlayer1)
            {
                gameData.player2CardValue = cardValue;
                Debug.Log($"[GameDataProvider] Opponent (Player2) set card value updated: {cardValue}");
            }
            else
            {
                gameData.player1CardValue = cardValue;
                Debug.Log($"[GameDataProvider] Opponent (Player1) set card value updated: {cardValue}");
            }
        }
    }
    
    /// <summary>
    /// ゲームデータの詳細をログ出力
    /// </summary>
    private void LogGameDataDetails()
    {
        Debug.Log("=== Game Data Details ===");
        Debug.Log($"[GameDataProvider] GameId: {gameData.gameId}");
        Debug.Log($"[GameDataProvider] RoomCode: {gameData.roomCode}");
        Debug.Log($"[GameDataProvider] PlayerId: {gameData.playerId}");
        Debug.Log($"[GameDataProvider] OpponentId: {gameData.opponentId}");
        Debug.Log($"[GameDataProvider] IsPlayer1: {gameData.isPlayer1}");
        Debug.Log($"[GameDataProvider] Player1Cards: {(gameData.player1Cards != null ? string.Join(",", gameData.player1Cards) : "null")}");
        Debug.Log($"[GameDataProvider] Player2Cards: {(gameData.player2Cards != null ? string.Join(",", gameData.player2Cards) : "null")}");
        Debug.Log($"[GameDataProvider] Player1CardValue: {gameData.player1CardValue}");
        Debug.Log($"[GameDataProvider] Player2CardValue: {gameData.player2CardValue}");
        Debug.Log($"[GameDataProvider] Player1Life: {gameData.player1Life}");
        Debug.Log($"[GameDataProvider] Player2Life: {gameData.player2Life}");
    }
    
    /// <summary>
    /// プレイヤーのカード値を取得
    /// </summary>
    public int GetPlayerCardValue(int index)
    {
        var cards = MyCards;
        if (cards != null && index >= 0 && index < cards.Length)
        {
            return cards[index];
        }
        Debug.LogWarning($"[GameDataProvider] Invalid player card index: {index}");
        return -1;
    }
    
    /// <summary>
    /// 相手のカード値を取得
    /// </summary>
    public int GetOpponentCardValue(int index)
    {
        var cards = OpponentCards;
        if (cards != null && index >= 0 && index < cards.Length)
        {
            return cards[index];
        }
        Debug.LogWarning($"[GameDataProvider] Invalid opponent card index: {index}");
        return -1;
    }
    
    /// <summary>
    /// ゲームデータが有効かチェック
    /// </summary>
    public bool IsValid()
    {
        return gameData != null && 
               !string.IsNullOrEmpty(gameData.gameId) && 
               !string.IsNullOrEmpty(gameData.playerId);
    }
    
    [Serializable]
    public class OnlineGameDataWithCards
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
        public int[] player1Cards;
        public int[] player2Cards;
        public int? player1CardValue;  // セットしたカード値
        public int? player2CardValue;  // セットしたカード値
        public int player1Life;  // プレイヤー1のライフ
        public int player2Life;  // プレイヤー2のライフ
        public List<string> player1UsedSkills;  // プレイヤー1の使用済スキル
        public List<string> player2UsedSkills;  // プレイヤー2の使用済スキル
    }
}

