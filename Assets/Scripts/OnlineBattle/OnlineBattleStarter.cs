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

            // ゲームデータを保存
            var onlineGameData = new OnlineGameData
            {
                roomCode = roomCode,
                playerId = playerId,
                opponentId = opponentId,
                isPlayer1 = isPlayer1,
                gameId = response.gameId
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
        public string currentTurn;
        public string gamePhase;
        public int player1Life;
        public int player2Life;
    }

    [System.Serializable]
    private class OnlineGameData
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
    }
} 