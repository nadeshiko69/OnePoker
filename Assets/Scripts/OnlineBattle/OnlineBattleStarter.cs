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
        string json = "{\"roomCode\":\"" + roomCode + "\"}";
        UnityWebRequest request = new UnityWebRequest(startGameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<StartGameResponse>(request.downloadHandler.text);

            // isPlayer1フラグでplayer1Id/player2Idを切り替え
            string getOpponentId;
            if (isPlayer1) {
                getOpponentId = response.player2Id;
            } else {
                getOpponentId = response.player1Id;
            }

            // ゲームデータを保存（カード配列も含める）
            var onlineGameData = new OnlineGameDataWithCards
            {
                roomCode = roomCode,
                playerId = playerId,
                opponentId = getOpponentId,
                isPlayer1 = isPlayer1,
                gameId = response.gameId,
                player1Cards = response.player1Cards,
                player2Cards = response.player2Cards,
            };
            string gameDataJson = JsonUtility.ToJson(onlineGameData);
            PlayerPrefs.SetString("OnlineGameData", gameDataJson);
            PlayerPrefs.Save();

            // 3秒待ってシーン遷移
            yield return new WaitForSeconds(3f);
            SceneManager.LoadScene("OnlineBattleScene");
        }
        else
        {
            Debug.LogError($"Failed to start game: {request.error}");
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