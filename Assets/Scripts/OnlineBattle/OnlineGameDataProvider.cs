using UnityEngine;
using System;

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
    public bool IsPlayer1 => gameData?.isPlayer1 ?? false;
    public int[] Player1Cards => gameData?.player1Cards;
    public int[] Player2Cards => gameData?.player2Cards;
    public int[] MyCards => IsPlayer1 ? Player1Cards : Player2Cards;
    public int[] OpponentCards => IsPlayer1 ? Player2Cards : Player1Cards;
    
    /// <summary>
    /// PlayerPrefsからゲームデータを読み込み
    /// </summary>
    public bool LoadGameData()
    {
        Debug.Log("[GameDataProvider] Loading game data from PlayerPrefs");
        
        string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        
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
    }
}

