using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public static class OnlineBattleStarter
{
    public static IEnumerator StartGameAndTransition(
        string startGameUrl,
        string roomCode,
        string playerId,
        string opponentId,
        bool isPlayer1)
    {
        Debug.Log($"[OnlineBattleStarter] 🚀 StartGameAndTransition開始");
        Debug.Log($"[OnlineBattleStarter] パラメータ - roomCode: {roomCode}, playerId: {playerId}, opponentId: {opponentId}, isPlayer1: {isPlayer1}");
        Debug.Log($"[OnlineBattleStarter] API URL: {startGameUrl}");
        
        string json = "{\"roomCode\":\"" + roomCode + "\"}";
        Debug.Log($"[OnlineBattleStarter] リクエストJSON: {json}");
        
        UnityWebRequest request = new UnityWebRequest(startGameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"[OnlineBattleStarter] start-game API呼び出し中...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[OnlineBattleStarter] ✅ start-game API成功");
            Debug.Log($"[OnlineBattleStarter] レスポンス: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<StartGameResponse>(request.downloadHandler.text);
            Debug.Log($"[OnlineBattleStarter] レスポンス解析完了 - gameId: {response.gameId}");

            // 引数で渡されたopponentIdをそのまま使用（既に正しく設定されている）
            string finalOpponentId = opponentId;
            Debug.Log($"[OnlineBattleStarter] 最終opponentId: {finalOpponentId}");

            // ゲームデータを保存（カード配列も含める）
            var onlineGameData = new OnlineGameDataWithCards
            {
                roomCode = roomCode,
                playerId = playerId,
                opponentId = finalOpponentId,
                isPlayer1 = isPlayer1,
                gameId = response.gameId,
                player1Cards = response.player1Cards,
                player2Cards = response.player2Cards,
            };
            
            Debug.Log($"[OnlineBattleStarter] ゲームデータ作成完了");
            Debug.Log($"[OnlineBattleStarter] - roomCode: {onlineGameData.roomCode}");
            Debug.Log($"[OnlineBattleStarter] - playerId: {onlineGameData.playerId}");
            Debug.Log($"[OnlineBattleStarter] - opponentId: {onlineGameData.opponentId}");
            Debug.Log($"[OnlineBattleStarter] - isPlayer1: {onlineGameData.isPlayer1}");
            Debug.Log($"[OnlineBattleStarter] - gameId: {onlineGameData.gameId}");
            Debug.Log($"[OnlineBattleStarter] - player1Cards: [{string.Join(", ", response.player1Cards)}]");
            Debug.Log($"[OnlineBattleStarter] - player2Cards: [{string.Join(", ", response.player2Cards)}]");
            
            string gameDataJson = JsonUtility.ToJson(onlineGameData);
            PlayerPrefs.SetString("OnlineGameData", gameDataJson);
            PlayerPrefs.Save();

            Debug.Log($"[OnlineBattleStarter] 💾 ゲームデータ保存完了 - PlayerPrefsに保存");
            Debug.Log($"[OnlineBattleStarter] 保存されたJSON: {gameDataJson}");

            // 1秒待ってシーン遷移
            Debug.Log($"[OnlineBattleStarter] ⏰ 1秒後にシーン遷移開始...");
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"[OnlineBattleStarter] 🎮 OnlineBattleSceneに遷移開始");
            SceneManager.LoadScene("OnlineBattleScene");
        }
        else
        {
            Debug.LogError($"[OnlineBattleStarter] ❌ start-game API失敗");
            Debug.LogError($"[OnlineBattleStarter] エラー詳細: {request.error}");
            Debug.LogError($"[OnlineBattleStarter] エラーレスポンス: {request.downloadHandler.text}");
            
            Debug.Log($"[OnlineBattleStarter] 🔙 TitleSceneに戻ります");
            SceneManager.LoadScene("TitleScene");
        }
    }

    [System.Serializable]
    private class StartGameResponse
    {
        public string gameId;
        public string roomCode;
        public string player1Id;
        public string player2Id;
        public int[] player1Cards;
        public int[] player2Cards;
        public string currentTurn;
        public string gamePhase;
        public int player1Life;
        public int player2Life;
    }

    [System.Serializable]
    private class OnlineGameDataWithCards
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
        public int[] player1Cards;
        public int[] player2Cards;
        public string player1Id;
        public string player2Id;
    }
} 